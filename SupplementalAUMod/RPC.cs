using HarmonyLib;
using Hazel;
using static AUMod.Roles;
/* using static TheOtherRoles.HudManagerStartPatch; */
using static AUMod.GameHistory;
using static AUMod.MapOptions;
using AUMod.Patches;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace AUMod {
enum RoleId {
    Sheriff,
    Madmate,
    Crewmate,
    Impostor
}

enum CustomRPC {
    // Main Controls

    ResetVaribles = 50,
    // ShareOptionSelection,
    // ForceEnd,
    SetRole,
    VersionHandshake,
    // UseUncheckedVent,
    // UncheckedMurderPlayer,
    // UncheckedCmdReportDeadBody,
    // TODO
    // ConsumeAdminTime,

    // Role functionality

    // TODO
    // SheriffKill,
}

public static class RPCProcedure {

    // Main Controls

    public static void resetVariables()
    {
        clearAndReloadMapOptions();
        clearAndReloadRoles();
        clearGameHistory();
        /* setCustomButtonCooldowns(); */
    }

    /*
     * TODO
    public static void shareOptionSelection(uint id, uint selection)
    {
        CustomOption option = CustomOption.options.FirstOrDefault(option => option.id == (int)id);
        option.updateSelection((int)selection);
    }

    public static void forceEnd()
    {
        foreach (PlayerControl player in PlayerControl.AllPlayerControls) {
            if (!player.Data.IsImpostor) {
                player.RemoveInfected();
                player.MurderPlayer(player);
                player.Data.IsDead = true;
            }
        }
    }
    */

    public static void setRole(byte roleId, byte playerId, byte flag)
    {
        foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            if (player.PlayerId == playerId) {
                switch ((RoleId)roleId) {
                case RoleId.Sheriff:
                    // TODO
                    /* Sheriff.sheriff = player; */
                    break;
                case RoleId.Madmate:
                    Madmate.madmate = player;
                    break;
                }
            }
    }

    public static void versionHandshake(int major, int minor, int build, int revision, Guid guid, int clientId)
    {
        System.Version ver;
        if (revision < 0)
            ver = new System.Version(major, minor, build);
        else
            ver = new System.Version(major, minor, build, revision);

        GameStartManagerPatch.playerVersions[clientId] = new GameStartManagerPatch.PlayerVersion(ver, guid);
    }

    /*
     * TODO
    public static void useUncheckedVent(int ventId, byte playerId, byte isEnter)
    {
        PlayerControl player = Helpers.playerById(playerId);
        if (player == null)
            return;
        // Fill dummy MessageReader and call MyPhysics.HandleRpc as the corountines cannot be accessed
        MessageReader reader = new MessageReader();
        byte[] bytes = BitConverter.GetBytes(ventId);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        reader.Buffer = bytes;
        reader.Length = bytes.Length;

        JackInTheBox.startAnimation(ventId);
        player.MyPhysics.HandleRpc(isEnter != 0 ? (byte)19 : (byte)20, reader);
    }

    public static void uncheckedMurderPlayer(byte sourceId, byte targetId)
    {
        PlayerControl source = Helpers.playerById(sourceId);
        PlayerControl target = Helpers.playerById(targetId);
        if (source != null && target != null)
            source.MurderPlayer(target);
    }

    public static void uncheckedCmdReportDeadBody(byte sourceId, byte targetId)
    {
        PlayerControl source = Helpers.playerById(sourceId);
        PlayerControl target = Helpers.playerById(targetId);
        if (source != null && target != null)
            source.ReportDeadBody(target.Data);
    }
     */

    /*
     * TODO
    public static void consumeAdminTime(float delta)
    {
        MapOptions.AdminTimer -= delta;
    }
     */

    // Role functionality

    /*
     * TODO
    public static void sheriffKill(byte targetId)
    {
        foreach (PlayerControl player in PlayerControl.AllPlayerControls) {
            if (player.PlayerId == targetId) {
                Sheriff.sheriff.MurderPlayer(player);
                return;
            }
        }
    }
     */
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
class RPCHandlerPatch {
    static void Postfix([HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        byte packetId = callId;
        switch (packetId) {
        // Main Controls
        case (byte)CustomRPC.ResetVaribles:
            RPCProcedure.resetVariables();
            break;
        /*
         * TODO
        case (byte)CustomRPC.ShareOptionSelection:
            uint id = reader.ReadPackedUInt32();
            uint selection = reader.ReadPackedUInt32();
            RPCProcedure.shareOptionSelection(id, selection);
            break;
        case (byte)CustomRPC.ForceEnd:
            RPCProcedure.forceEnd();
            break;
         */
        case (byte)CustomRPC.SetRole:
            byte roleId = reader.ReadByte();
            byte playerId = reader.ReadByte();
            byte flag = reader.ReadByte();
            RPCProcedure.setRole(roleId, playerId, flag);
            break;
        case (byte)CustomRPC.VersionHandshake:
            byte major = reader.ReadByte();
            byte minor = reader.ReadByte();
            byte patch = reader.ReadByte();
            int versionOwnerId = reader.ReadPackedInt32();
            byte revision = 0xFF;
            Guid guid;
            if (reader.Length - reader.Position >= 17) { // enough bytes left to read
                revision = reader.ReadByte();
                // GUID
                byte[] gbytes = reader.ReadBytes(16);
                guid = new Guid(gbytes);
            } else {
                guid = new Guid(new byte[16]);
            }
            RPCProcedure.versionHandshake(major, minor, patch, revision == 0xFF ? -1 : revision, guid, versionOwnerId);
            break;
            /*
         * TODO
        case (byte)CustomRPC.UseUncheckedVent:
            int ventId = reader.ReadPackedInt32();
            byte ventingPlayer = reader.ReadByte();
            byte isEnter = reader.ReadByte();
            RPCProcedure.useUncheckedVent(ventId, ventingPlayer, isEnter);
            break;
        case (byte)CustomRPC.UncheckedMurderPlayer:
            byte source = reader.ReadByte();
            byte target = reader.ReadByte();
            RPCProcedure.uncheckedMurderPlayer(source, target);
            break;
        case (byte)CustomRPC.UncheckedCmdReportDeadBody:
            byte reportSource = reader.ReadByte();
            byte reportTarget = reader.ReadByte();
            RPCProcedure.uncheckedCmdReportDeadBody(reportSource, reportTarget);
            break;
         */
            /*
         * TODO
        case (byte)CustomRPC.ConsumeAdminTime:
            float delta = reader.ReadSingle();
            RPCProcedure.consumeAdminTime(delta);
            break;
         */

            // Role functionality
            /*
         * TODO
        case (byte)CustomRPC.SheriffKill:
            RPCProcedure.sheriffKill(reader.ReadByte());
            break;
         */
        }
    }
}
}
