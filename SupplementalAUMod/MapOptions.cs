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
    public static float AdminTimer = 0f;
    public static float VisibleAdminTimer = 0f;

    public static void clearAndReloadMapOptions()
    {
        /* AdminTimer = CustomOptionHolder.adminTimer.getFloat(); */
        AdminTimer = 10f;
        VisibleAdminTimer = 10f;
        showRoleSummary = true;
    }
}
}
