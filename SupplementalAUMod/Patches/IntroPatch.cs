using HarmonyLib;
using System;
using static AUMod.Roles;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace AUMod.Patches
{
    [HarmonyPatch]
    class IntroPatch {
        public static void setupIntroTeam(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
        }

        public static void setupIntroRole(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            List<RoleInfo> infos = RoleInfo.getRoleInfoForPlayer(PlayerControl.LocalPlayer);
            RoleInfo roleInfo = infos.FirstOrDefault();

            // TODO
            // add sheriff
            if (roleInfo != null && (roleInfo.roleId == RoleId.Madmate)) {
                __instance.TeamTitle.text = roleInfo.name;
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = roleInfo.introDescription;
                __instance.TeamTitle.color = roleInfo.color;
                __instance.BackgroundBar.material.color = roleInfo.color;
            }
        }

        private static void setUpIntroRoleText(IntroCutscene __instance)
        {
            List<RoleInfo> infos = RoleInfo.getRoleInfoForPlayer(PlayerControl.LocalPlayer);
            RoleInfo roleInfo = infos.FirstOrDefault();
            // TODO
            // add sheriff
            if (roleInfo != null && (roleInfo.roleId == RoleId.Madmate)) {
                __instance.YouAreText.color = roleInfo.color;
                __instance.RoleText.text = roleInfo.name;
                __instance.RoleText.color = roleInfo.color;
                __instance.RoleBlurbText.text = roleInfo.introDescription;
                __instance.RoleBlurbText.color = roleInfo.color;
            }
        }

        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.SetUpRoleText))]
        class SetUpRoleTextPatch {
            public static void Postfix(IntroCutscene __instance)
            {
                setUpIntroRoleText(__instance);
            }
        }

        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
        class BeginCrewmatePatch {
            public static void Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
            {
                setupIntroTeam(__instance, ref yourTeam);
            }

            public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
            {
                setupIntroRole(__instance, ref yourTeam);
            }
        }

        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
        class BeginImpostorPatch {
            public static void Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
            {
                setupIntroTeam(__instance, ref yourTeam);
            }

            public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
            {
                setupIntroRole(__instance, ref yourTeam);
            }
        }
    }
}
