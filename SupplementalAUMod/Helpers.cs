using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Collections;
using UnhollowerBaseLib;
using UnityEngine;
using System.Linq;
using static AUMod.Roles;
using HarmonyLib;
using Hazel;

namespace AUMod {
public static class Helpers {

    public static Sprite loadSpriteFromResources(string path, float pixelsPerUnit)
    {
        try {
            Texture2D texture = loadTextureFromResources(path);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        } catch {
            System.Console.WriteLine("Error loading sprite from path: " + path);
        }
        return null;
    }

    public static Texture2D loadTextureFromResources(string path)
    {
        try {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(path);
            var byteTexture = new byte[stream.Length];
            var read = stream.Read(byteTexture, 0, (int)stream.Length);
            LoadImage(texture, byteTexture, false);
            return texture;
        } catch {
            System.Console.WriteLine("Error loading texture from resources: " + path);
        }
        return null;
    }

    public static Texture2D loadTextureFromDisk(string path)
    {
        try {
            if (File.Exists(path)) {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
                byte[] byteTexture = File.ReadAllBytes(path);
                LoadImage(texture, byteTexture, false);
                return texture;
            }
        } catch {
            System.Console.WriteLine("Error loading texture from disk: " + path);
        }
        return null;
    }

    internal delegate bool d_LoadImage(IntPtr tex, IntPtr data, bool markNonReadable);
    internal static d_LoadImage iCall_LoadImage;
    private static bool LoadImage(Texture2D tex, byte[] data, bool markNonReadable)
    {
        if (iCall_LoadImage == null)
            iCall_LoadImage = IL2CPP.ResolveICall<d_LoadImage>("UnityEngine.ImageConversion::LoadImage");
        var il2cppArray = (Il2CppStructArray<byte>)data;
        return iCall_LoadImage.Invoke(tex.Pointer, il2cppArray.Pointer, markNonReadable);
    }

    public static PlayerControl playerById(byte id)
    {
        foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            if (player.PlayerId == id)
                return player;
        return null;
    }

    public static Dictionary<byte, PlayerControl> allPlayersById()
    {
        Dictionary<byte, PlayerControl> res = new Dictionary<byte, PlayerControl>();
        foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            res.Add(player.PlayerId, player);
        return res;
    }

    public static bool hasFakeTasks(this PlayerControl player)
    {
        return player == Madmate.madmate;
    }

    public static void clearAllTasks(this PlayerControl player)
    {
        if (player == null)
            return;
        for (int i = 0; i < player.myTasks.Count; i++) {
            PlayerTask playerTask = player.myTasks[i];
            playerTask.OnRemove();
            UnityEngine.Object.Destroy(playerTask.gameObject);
        }
        player.myTasks.Clear();

        if (player.Data != null && player.Data.Tasks != null)
            player.Data.Tasks.Clear();
    }

    public static string GetString(this TranslationController t, StringNames key, params Il2CppSystem.Object[] parts)
    {
        return t.GetString(key, parts);
    }

    public static string cs(Color c, string s)
    {
        return string.Format("<color=#{0:X2}{1:X2}{2:X2}{3:X2}>{4}</color>", ToByte(c.r), ToByte(c.g), ToByte(c.b), ToByte(c.a), s);
    }

    private static byte ToByte(float f)
    {
        f = Mathf.Clamp01(f);
        return (byte)(f * 255);
    }

    public static KeyValuePair<byte, int> MaxPair(this Dictionary<byte, int> self, out bool tie)
    {
        tie = true;
        KeyValuePair<byte, int> result = new KeyValuePair<byte, int>(byte.MaxValue, int.MinValue);
        foreach (KeyValuePair<byte, int> keyValuePair in self) {
            if (keyValuePair.Value > result.Value) {
                result = keyValuePair;
                tie = false;
            } else if (keyValuePair.Value == result.Value) {
                tie = true;
            }
        }
        return result;
    }
}
}
