using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using static AUMod.Roles;

namespace AUMod {
static class MapOptions {
    // Set values
    public static bool showRoleSummary = true;

    // Updating values
    public static float AdminTimer = 10f;
    public static TMPro.TextMeshPro AdminTimerText = null;

    public static void clearAndReloadMapOptions()
    {
        /* AdminTimer = CustomOptionHolder.adminTimer.getFloat(); */
        AdminTimer = 10f;
        ClearAdminTimerText();
        UpdateAdminTimerText();
        showRoleSummary = true;
    }

    public static void MeetingEndedUpdate()
    {
        ClearAdminTimerText();
        UpdateAdminTimerText();
    }

    private static void UpdateAdminTimerText()
    {
        if (HudManager.Instance == null)
            return;
        AdminTimerText = UnityEngine.Object.Instantiate(HudManager.Instance.TaskText, HudManager.Instance.transform);
        AdminTimerText.transform.localPosition = new Vector3(-3.5f, -3.5f, 0);
        if (AdminTimer > 0)
            AdminTimerText.text = $"Admin: {(int)AdminTimer} sec remaining";
        else
            AdminTimerText.text = "Admin: ran out of time";
        AdminTimerText.gameObject.SetActive(true);
    }

    private static void ClearAdminTimerText()
    {
        if (AdminTimerText == null)
            return;
        UnityEngine.Object.Destroy(AdminTimerText);
        AdminTimerText = null;
    }
}
}
