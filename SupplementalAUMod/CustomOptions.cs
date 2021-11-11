using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using System;
using System.Linq;
using HarmonyLib;
using Hazel;
using System.Reflection;
using System.Text;
using static AUMod.Roles;

namespace AUMod {
public class CustomOptionHolder {
    public static string[] rates = new string[] { "0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%" };
    public static string[] presets = new string[] { "Preset 1" };

    /*
     * TODO
     * To limit admin use
        public static CustomOption adminTimer;
        public static CustomOption enabledAdminTimer;
     */

    public static CustomOption sheriffSpawnRate;
    public static CustomOption sheriffCooldown;
    public static CustomOption sheriffNumberOfShots;
    public static CustomOption sheriffCanKillNeutrals;
    public static CustomOption sheriffCanKillCrewmates;

    public static CustomOption madmateSpawnRate;
    public static CustomOption madmateCanDieToSheriff;
    public static CustomOption madmateCanEnterVents;
    public static CustomOption madmateHasImpostorVision;
    public static CustomOption madmateCanFixComm;

    public static string cs(Color c, string s)
    {
        return string.Format("<color=#{0:X2}{1:X2}{2:X2}{3:X2}>{4}</color>", ToByte(c.r), ToByte(c.g), ToByte(c.b), ToByte(c.a), s);
    }

    private static byte ToByte(float f)
    {
        f = Mathf.Clamp01(f);
        return (byte)(f * 255);
    }

    public static void Load()
    {

        // Using new id's for the options to not break compatibilty with older versions
        /*
         * TODO
        adminTimer = CustomOption.Create(100, cs(new Color(204f / 255f, 204f / 255f, 0, 1f), "Admin Map Available Duration"), 10f, 0f, 120f, 1f);
        enabledAdminTimer = CustomOption.Create(101, cs(new Color(204f / 255f, 204f / 255f, 0, 1f), "Enable Admin Map Available Duration"), false);
        */

        sheriffSpawnRate = CustomOption.Create(110, cs(Sheriff.color, "Sheriff"), rates, null, true);
        sheriffCooldown = CustomOption.Create(111, "Sheriff Cooldown", 30f, 10f, 60f, 2.5f, sheriffSpawnRate);
        sheriffNumberOfShots = CustomOption.Create(112, "Sheriff Number Of Shots", 1f, 1f, 15f, 1f, sheriffSpawnRate);
        sheriffCanKillNeutrals = CustomOption.Create(113, "Sheriff Can Kill Neutrals", false, sheriffSpawnRate);
        sheriffCanKillCrewmates = CustomOption.Create(114, "Sheriff Can Kill Crewmates", false, sheriffSpawnRate);

        madmateSpawnRate = CustomOption.Create(120, cs(Madmate.color, "Madmate"), rates, null, true);
        madmateCanDieToSheriff = CustomOption.Create(121, "Madmate Can Die To Sheriff", true, madmateSpawnRate);
        madmateCanEnterVents = CustomOption.Create(122, "Madmate Can Enter Vents", true, madmateSpawnRate);
        madmateHasImpostorVision = CustomOption.Create(123, "Madmate Has Impostor Vision", true, madmateSpawnRate);
        madmateCanFixComm = CustomOption.Create(124, "Madmate Can Fix Comm", false, madmateSpawnRate);
    }
}

public class CustomOption {
    public static List<CustomOption> options = new List<CustomOption>();
    public static int preset = 0;

    public int id;
    public string name;
    public System.Object[] selections;

    public int defaultSelection;
    public ConfigEntry<int> entry;
    public int selection;
    public OptionBehaviour optionBehaviour;
    public CustomOption parent;
    public bool isHeader;

    // Option creation
    public CustomOption(int id, string name, System.Object[] selections, System.Object defaultValue, CustomOption parent, bool isHeader)
    {
        this.id = id;
        this.name = parent == null ? name : "- " + name;
        this.selections = selections;
        int index = Array.IndexOf(selections, defaultValue);
        this.defaultSelection = index >= 0 ? index : 0;
        this.parent = parent;
        this.isHeader = isHeader;
        selection = 0;

        // id 0 is preset selection in TheOtherRoles
        if (id != 0) {
            entry = AUModPlugin.Instance.Config.Bind($"Preset{preset}", id.ToString(), defaultSelection);
            selection = Mathf.Clamp(entry.Value, 0, selections.Length - 1);
        }
        options.Add(this);
    }

    public static CustomOption Create(int id, string name, string[] selections, CustomOption parent = null, bool isHeader = false)
    {
        return new CustomOption(id, name, selections, "100%", parent, isHeader);
    }

    public static CustomOption Create(int id, string name, float defaultValue, float min, float max, float step,
        CustomOption parent = null, bool isHeader = false)
    {
        List<float> selections = new List<float>();
        for (float s = min; s <= max; s += step)
            selections.Add(s);
        return new CustomOption(id, name, selections.Cast<object>().ToArray(), defaultValue, parent, isHeader);
    }

    public static CustomOption Create(int id, string name, bool defaultValue, CustomOption parent = null, bool isHeader = false)
    {
        return new CustomOption(id, name, new string[] { "Off", "On" }, defaultValue ? "On" : "Off", parent, isHeader);
    }

    public static void ShareOptionSelections()
    {
        if (PlayerControl.AllPlayerControls.Count <= 1 || AmongUsClient.Instance?.AmHost == false && PlayerControl.LocalPlayer == null)
            return;
        /*
         * TODO
        foreach (CustomOption option in CustomOption.options) {
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId,
                (byte)CustomRPC.ShareOptionSelection,
                Hazel.SendOption.Reliable);
            messageWriter.WritePacked((uint)option.id);
            messageWriter.WritePacked((uint)Convert.ToUInt32(option.selection));
            messageWriter.EndMessage();
        }
        */
    }

    // Getter

    public int getSelection()
    {
        return selection;
    }

    public bool getBool()
    {
        return selection > 0;
    }

    public float getFloat()
    {
        return (float)selections[selection];
    }

    // Option changes

    public void updateSelection(int newSelection)
    {
        selection = Mathf.Clamp((newSelection + selections.Length) % selections.Length, 0, selections.Length - 1);
        if (optionBehaviour != null && optionBehaviour is StringOption stringOption) {
            stringOption.oldValue = stringOption.Value = selection;
            stringOption.ValueText.text = selections[selection].ToString();

            if (AmongUsClient.Instance?.AmHost == true && PlayerControl.LocalPlayer) {
                if (entry != null)
                    entry.Value = selection; // Save selection to config

                ShareOptionSelections(); // Share all selections
            }
        }
    }
}
}
