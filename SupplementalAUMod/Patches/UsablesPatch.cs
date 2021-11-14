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
            if (pc.Role.Role == RoleTypes.Engineer) {
                canUse = true;
                couldUse = true;
                return true;
            }

            float num = float.MaxValue;
            PlayerControl @object = pc.Object;

            bool roleCouldUse = false;
            if (Madmate.canEnterVents && Madmate.madmate != null && Madmate.madmate == @object)
                roleCouldUse = true;
            else if (pc.Role.IsImpostor)
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
        static Dictionary<SystemTypes, List<Color>> players = new Dictionary<SystemTypes, List<Color>>();

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
                if (CustomOptionHolder.enabledAdminTimer.getBool())
                    return MapOptions.AdminTimer > 0;
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
                if (!PlayerControl.LocalPlayer.Data.IsDead) {
                    // Show the grey map if all time to see admin is consumed and the player is alive
                    if (MapOptions.AdminTimer <= 0) {
                        __instance.isSab = true;
                        __instance.BackgroundColor.SetColor(Palette.DisabledGrey);
                        return false;
                    }

                    // Consume the time via RPC
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                        PlayerControl.LocalPlayer.NetId,
                        (byte)CustomRPC.ConsumeAdminTime,
                        Hazel.SendOption.Reliable,
                        -1);
                    writer.Write(__instance.timer);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.consumeAdminTime(__instance.timer);
                }

                // reset timer
                __instance.timer = 0f;

                // save players (for future engineer)
                players = new Dictionary<SystemTypes, List<Color>>();
                bool commsActive = false;

                // whether under comms sabotage
                foreach (PlayerTask task in PlayerControl.LocalPlayer.myTasks) {
                    if (task.TaskType == TaskTypes.FixComms) {
                        commsActive = true;
                        break;
                    }
                }

                // under comms sabotage
                if (!__instance.isSab && commsActive) {
                    __instance.isSab = true;
                    __instance.BackgroundColor.SetColor(Palette.DisabledGrey);
                    __instance.SabotageText.gameObject.SetActive(true);
                    return false;
                }

                // already fixed comms sabotage
                if (__instance.isSab && !commsActive) {
                    __instance.isSab = false;
                    __instance.BackgroundColor.SetColor(Color.green);
                    __instance.SabotageText.gameObject.SetActive(false);
                }

                for (int i = 0; i < __instance.CountAreas.Length; i++) {
                    CounterArea counterArea = __instance.CountAreas[i];
                    List<Color> roomColors = new List<Color>();
                    players.Add(counterArea.RoomType, roomColors);

                    if (commsActive) {
                        counterArea.UpdateCount(0);
                    } else {
                        PlainShipRoom plainShipRoom = ShipStatus.Instance.FastRooms[counterArea.RoomType];

                        if (plainShipRoom == null || !plainShipRoom.roomArea) {
                            // maybe error
                            return false;
                        }

                        int numPlayers = plainShipRoom.roomArea.OverlapCollider(__instance.filter, __instance.buffer);
                        int numAlivePlayers = numPlayers;
                        for (int j = 0; j < numPlayers; j++) {
                            Collider2D collider2D = __instance.buffer[j];
                            if (!(collider2D.tag == "DeadBody")) {
                                PlayerControl component = collider2D.GetComponent<PlayerControl>();
                                if (!component || component.Data == null || component.Data.Disconnected || component.Data.IsDead)
                                    numAlivePlayers--;
                                else if (component?.myRend?.material != null) {
                                    Color color = component.myRend.material.GetColor("_BodyColor");
                                    roomColors.Add(color);
                                }
                            } else {
                                DeadBody component = collider2D.GetComponent<DeadBody>();
                                if (!component)
                                    continue;

                                GameData.PlayerInfo playerInfo = GameData.Instance.GetPlayerById(component.ParentId);
                                if (playerInfo == null)
                                    continue;

                                Color color = Color.green;
                                roomColors.Add(color);
                            }
                        }
                        counterArea.UpdateCount(numAlivePlayers);
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(CounterArea), nameof(CounterArea.UpdateCount))]
        class CounterAreaUpdateCountPatch {
            private static Material defaultMat;
            static void Postfix(CounterArea __instance)
            {
                if (players.ContainsKey(__instance.RoomType)) {
                    List<Color> colors = players[__instance.RoomType];

                    for (int i = 0; i < __instance.myIcons.Count; i++) {
                        PoolableBehavior icon = __instance.myIcons[i];
                        SpriteRenderer renderer = icon.GetComponent<SpriteRenderer>();

                        if (renderer != null) {
                            if (defaultMat == null)
                                defaultMat = renderer.material;
                            renderer.material = defaultMat;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap))]
        class ShowSabotageMapPatch {
            static void Postfix(MapBehaviour __instance)
            {
                __instance.taskOverlay.Hide();
            }
        }
    }
}
