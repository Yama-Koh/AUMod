using HarmonyLib;
using System;
using Hazel;
using UnityEngine;
using System.Linq;
using static AUMod.Roles;
using static AUMod.GameHistory;
using static AUMod.MapOptions;
using System.Collections.Generic;

namespace AUMod.Patches
{

    [HarmonyPatch(typeof(Vent), "CanUse")]
    public static class VentCanUsePatch {
        public static bool Prefix(Vent __instance,
            ref float __result,
            [HarmonyArgument(0)] GameData.PlayerInfo pc,
            [HarmonyArgument(1)] out bool canUse,
            [HarmonyArgument(2)] out bool couldUse)
        {
            float num = float.MaxValue;
            PlayerControl @object = pc.Object;

            bool roleCouldUse = false;
            if (Madmate.canEnterVents && Madmate.madmate != null && Madmate.madmate == @object)
                roleCouldUse = true;

            var usableDistance = __instance.UsableDistance;

            couldUse = (@object.inVent || roleCouldUse) && !pc.IsDead && (@object.CanMove || @object.inVent);
            canUse = couldUse;
            if (canUse) {
                Vector2 truePosition = @object.GetTruePosition();
                Vector3 position = __instance.transform.position;
                num = Vector2.Distance(truePosition, position);

                canUse &= (num <= usableDistance && !PhysicsHelpers.AnythingBetween(truePosition, position, Constants.ShipOnlyMask, false));
            }
            __result = num;
            return false;
        }
    }

    [HarmonyPatch(typeof(Vent), "Use")]
    public static class VentUsePatch {
        public static bool Prefix(Vent __instance)
        {
            bool canUse;
            bool couldUse;
            __instance.CanUse(PlayerControl.LocalPlayer.Data, out canUse, out couldUse);
            if (!canUse)
                return false; // No need to execute the native method as using is disallowed anyways

            bool canMoveInVents = true;
            if (Madmate.madmate == PlayerControl.LocalPlayer) {
                canMoveInVents = false;
            }

            bool isEnter = !PlayerControl.LocalPlayer.inVent;
            if (isEnter) {
                PlayerControl.LocalPlayer.MyPhysics.RpcEnterVent(__instance.Id);
            } else {
                PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(__instance.Id);
            }
            __instance.SetButtons(isEnter && canMoveInVents);
            return false;
        }
    }

    [HarmonyPatch(typeof(Console), nameof(Console.CanUse))]
    public static class ConsoleCanUsePatch {
        public static bool Prefix(ref float __result,
            Console __instance,
            [HarmonyArgument(0)] GameData.PlayerInfo pc,
            [HarmonyArgument(1)] out bool canUse,
            [HarmonyArgument(2)] out bool couldUse)
        {
            canUse = couldUse = false;
            if (__instance.AllowImpostor) {
                if (Madmate.madmate != null && Madmate.madmate == PlayerControl.LocalPlayer)
                    return !__instance.TaskTypes.Any(x => x == TaskTypes.FixLights || (!Madmate.canFixComm && x == TaskTypes.FixComms));
                return true;
            }
            if (!Helpers.hasFakeTasks(pc.Object))
                return true;
            __result = float.MaxValue;
            return false;
        }
    }

    [HarmonyPatch(typeof(TuneRadioMinigame), nameof(TuneRadioMinigame.Begin))]
    class CommsMinigameBeginPatch {
        static void Postfix(TuneRadioMinigame __instance)
        {
            if (!Madmate.canFixComm && Madmate.madmate != null && Madmate.madmate == PlayerControl.LocalPlayer) {
                __instance.Close();
            }
        }
    }

    [HarmonyPatch(typeof(SwitchMinigame), nameof(SwitchMinigame.Begin))]
    class LightsMinigameBeginPatch {
        static void Postfix(SwitchMinigame __instance)
        {
            if (Madmate.madmate != null && Madmate.madmate == PlayerControl.LocalPlayer) {
                __instance.Close();
            }
        }
    }

    [HarmonyPatch]
    class AdminPanelPatch {
        [HarmonyPatch(typeof(MapConsole), nameof(MapConsole.CanUse))]
        class MapConsoleCanUsePatch {
            public static bool Prefix(MapConsole __instance,
                ref float __result,
                [HarmonyArgument(0)] GameData.PlayerInfo pc,
                [HarmonyArgument(1)] out bool canUse,
                [HarmonyArgument(2)] out bool couldUse)
            {
                canUse = couldUse = false;
                __result = float.MaxValue;

                if (PlayerControl.LocalPlayer.Data.IsDead)
                    return true;
                /* if (CustomOptionHolder.enabledAdminTimer.getBool()) */
                /*     return MapOptions.AdminTimer > 0; */
                return true;
            }
        }

        [HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.Update))]
        class MapCountOverlayUpdatePatch {
            static bool Prefix(MapCountOverlay __instance)
            {
                __instance.timer += Time.deltaTime;
                if (__instance.timer < 0.1f) {
                    return false;
                }

                // Consume time to see admin map if the player is alive
                /*
                 * TODO
                if (!PlayerControl.LocalPlayer.Data.IsDead) {
                    // Show the grey map if all time to see admin is consumed and the player is alive
                    if (MapOptions.AdminTimer <= 0) {
                        __instance.isSab = true;
                        __instance.BackgroundColor.SetColor(Palette.DisabledGrey);
                        return false;
                    }

                    // Consume the time via RPC
                    MessageWriter writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId,
                        (byte)CustomRPC.ConsumeAdminTime,
                        Hazel.SendOption.Reliable);
                    writer.Write(__instance.timer);
                    writer.EndMessage();
                    // Reflect the consumed time to local (workaround)
                    MapOptions.AdminTimer -= __instance.timer;
                }
                 */

                __instance.timer = 0f;
                return false;
            }
        }
    }
}
