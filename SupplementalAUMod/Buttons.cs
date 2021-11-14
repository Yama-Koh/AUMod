using HarmonyLib;
using Hazel;
using System;
using UnityEngine;
using static AUMod.Roles;
using AUMod.Patches;

namespace AUMod {
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
static class HudManagerStartPatch {
    private static CustomButton sheriffKillButton;
    private static CustomButton madmateVentButton;

    public static void setCustomButtonCooldowns()
    {
        sheriffKillButton.MaxTimer = Sheriff.cooldown;
    }

    public static void Postfix(HudManager __instance)
    {
        // Sheriff Kill
        sheriffKillButton = new CustomButton(
            () => {
                byte targetId = 0;
                if (Sheriff.currentTarget.Data.Role.IsImpostor ||
                    (Sheriff.madmateCanDieToSheriff && Madmate.madmate == Sheriff.currentTarget))
                    targetId = Sheriff.currentTarget.PlayerId;
                else
                    targetId = PlayerControl.LocalPlayer.PlayerId;
                MessageWriter killWriter = AmongUsClient.Instance.StartRpcImmediately(
                    PlayerControl.LocalPlayer.NetId,
                    (byte)CustomRPC.SheriffKill,
                    Hazel.SendOption.Reliable,
                    -1);
                killWriter.Write(targetId);
                AmongUsClient.Instance.FinishRpcImmediately(killWriter);
                RPCProcedure.sheriffKill(targetId);

                sheriffKillButton.Timer = sheriffKillButton.MaxTimer;
                Sheriff.remainingShots--;
                Sheriff.currentTarget = null;
            },
            () => {
                return Sheriff.remainingShots > 0 &&
                  Sheriff.sheriff != null &&
                  Sheriff.sheriff == PlayerControl.LocalPlayer &&
                  !PlayerControl.LocalPlayer.Data.IsDead;
            },
            () => {
                return Sheriff.currentTarget && PlayerControl.LocalPlayer.CanMove;
            },
            () => {
                sheriffKillButton.Timer = sheriffKillButton.MaxTimer;
            },
            __instance.KillButton,
            __instance,
            KeyCode.Q
        );

        madmateVentButton = new CustomButton(
            () => {
                if (Madmate.currentTarget == null)
                    return;
                VentUsePatch.Prefix(Madmate.currentTarget);
            },
            () => {
                return Madmate.madmate != null &&
                  Madmate.canEnterVents &&
                  Madmate.madmate == PlayerControl.LocalPlayer &&
                  !PlayerControl.LocalPlayer.Data.IsDead;
            },
            () => {
                return Madmate.currentTarget &&
                  (PlayerControl.LocalPlayer.CanMove || PlayerControl.LocalPlayer.inVent);
            },
            () => {
            },
            __instance.ImpostorVentButton,
            __instance,
            KeyCode.V
        );

        // Set the default (or settings from the previous game) timers/durations when spawning the buttons
        setCustomButtonCooldowns();
    }
}
}
