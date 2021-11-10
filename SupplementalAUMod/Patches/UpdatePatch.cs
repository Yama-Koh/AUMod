using HarmonyLib;
using System;
using System.IO;
using System.Net.Http;
using UnityEngine;
using static AUMod.Roles;
using System.Collections.Generic;
using System.Linq;

namespace AUMod.Patches
{
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    class HudManagerUpdatePatch {
        static void setPlayerNameColor(PlayerControl p, Color color)
        {
            p.nameText.color = color;
            if (MeetingHud.Instance != null)
                foreach (PlayerVoteArea player in MeetingHud.Instance.playerStates)
                    if (player.NameText != null && p.PlayerId == player.TargetPlayerId)
                        player.NameText.color = color;
        }

        static void setNameColors()
        {
            /*
             * TODO
            if (Sheriff.sheriff != null && Sheriff.sheriff == PlayerControl.LocalPlayer)
                setPlayerNameColor(Sheriff.sheriff, Sheriff.color);
             */
            if (Madmate.madmate != null && Madmate.madmate == PlayerControl.LocalPlayer)
                setPlayerNameColor(Madmate.madmate, Madmate.color);
        }

        static void Postfix(HudManager __instance)
        {
            if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started)
                return;

            /* CustomButton.HudUpdate(); */
            setNameColors();
        }
    }
}
