using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnhollowerBaseLib;
using UnityEngine;
using System;
using static AUMod.Roles;

namespace AUMod.Patches
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
    class ShipStatusBeginPatch {

        public static void Prefix()
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId,
                (byte)CustomRPC.ResetVaribles,
                Hazel.SendOption.Reliable,
                -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.resetVariables();

            if (!DestroyableSingleton<TutorialManager>.InstanceExists) // Don't assign Roles in Tutorial
                assignRoles();
        }

        private static void assignRoles()
        {
            var data = getRoleAssignmentData();
            // Assign roles that should always be in the game next
            // Always perform this so far
            assignEnsuredRoles(data);
        }

        private static RoleAssignmentData getRoleAssignmentData()
        {
            // Get the players that we want to assign the roles to.
            // Madmate and Sheriff are assigned to natural crewmates.
            List<PlayerControl> crewmates = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
            crewmates.RemoveAll(x => x.Data.Role.IsImpostor);

            // Fill in the lists with the roles that should be assigned to players.
            // Note that the special roles (like Mafia or Lovers) are NOT included in these lists
            Dictionary<byte, int> crewSettings = new Dictionary<byte, int>();

            // TODO
            /* crewSettings.Add((byte)RoleId.Sheriff, CustomOptionHolder.sheriffSpawnRate.getSelection()); */
            crewSettings.Add((byte)RoleId.Madmate, CustomOptionHolder.madmateSpawnRate.getSelection());

            return new RoleAssignmentData {
                crewmates = crewmates,
                crewSettings = crewSettings
            };
        }

        private static void assignEnsuredRoles(RoleAssignmentData data)
        {
            // Get all roles where the chance to occur is set to 100%
            List<byte> ensuredCrewmateRoles = data.crewSettings.Select(x => x.Key).ToList();
            int crewmateRolesCount = ensuredCrewmateRoles.Count;

            // Assign roles until we run out of either players we can assign roles to or run out of roles we can assign to players
            while (data.crewmates.Count > 0 && crewmateRolesCount > 0) {
                Dictionary<RoleType, List<byte>> rolesToAssign = new Dictionary<RoleType, List<byte>>();
                rolesToAssign.Add(RoleType.Crewmate, ensuredCrewmateRoles);

                // Randomly select a pool of roles to assign a role from next (Crewmate role, Neutral role or Impostor role)
                // then select one of the roles from the selected pool to a player
                // and remove the role (and any potentially blocked role pairings) from the pool(s)
                var roleType = RoleType.Crewmate;
                var players = data.crewmates;
                var index = rnd.Next(0, rolesToAssign[roleType].Count);
                var roleId = rolesToAssign[roleType][index];
                setRoleToRandomPlayer(rolesToAssign[roleType][index], players);
                rolesToAssign[roleType].RemoveAt(index);

                // Adjust the role limit
                switch (roleType) {
                case RoleType.Crewmate:
                    crewmateRolesCount--;
                    break;
                case RoleType.Neutral:
                    /* data.maxNeutralRoles--; */
                    break;
                case RoleType.Impostor:
                    /* data.maxImpostorRoles--; */
                    break;
                }
            }
        }

        private static byte setRoleToRandomPlayer(byte roleId, List<PlayerControl> playerList, byte flag = 0, bool removePlayer = true)
        {
            var index = rnd.Next(0, playerList.Count);
            byte playerId = playerList[index].PlayerId;
            if (removePlayer)
                playerList.RemoveAt(index);

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId,
                (byte)CustomRPC.SetRole,
                Hazel.SendOption.Reliable,
                -1);
            writer.Write(roleId);
            writer.Write(playerId);
            writer.Write(flag);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.setRole(roleId, playerId, flag);
            return playerId;
        }

        private class RoleAssignmentData {
            public List<PlayerControl> crewmates { get; set; }
            public Dictionary<byte, int> crewSettings = new Dictionary<byte, int>();
        }

        private enum RoleType {
            Crewmate = 0,
            Neutral = 1,
            Impostor = 2
        }
    }
}
