using UnityEngine;
using HarmonyLib;
using static Jewelfarming.Jewelfarming;

namespace Jewelfarming
{
    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    public static class ZNetSceneAwakePatch
    {
        private static void Postfix(ZNetScene __instance)
        {
            if (__instance == null)
            {
                Debug.LogError("ZNetScene instance is null!");
                return;
            }

            string[] rawGemstoneNames =
            {
                "JC_Raw_Red_Gemstone", "JC_Raw_Blue_Gemstone", "JC_Raw_Black_Gemstone",
                "JC_Raw_Purple_Gemstone", "JC_Raw_Green_Gemstone", "JC_Raw_Yellow_Gemstone",
                "JC_Raw_Orange_Gemstone", "JC_Raw_Cyan_Gemstone"
            };

            foreach (var gemstoneName in rawGemstoneNames)
            {
                GameObject gemstonePrefab = __instance.GetPrefab(gemstoneName);
                if (gemstonePrefab == null)
                {
                    Debug.LogError($"Gemstone prefab {gemstoneName} not found!");
                    continue;
                }

                if (gemstonePrefab.GetComponent<Plant>() == null)
                {
                    Plant plantComponent = gemstonePrefab.AddComponent<Plant>();
                    plantComponent.m_growTime = 600f; // Example growth time, adjust as necessary

                    // Add CrystalGrowth component if not present
                    if (gemstonePrefab.GetComponent<CrystalGrowth>() == null)
                    {
                        CrystalGrowth crystalGrowthComponent = gemstonePrefab.AddComponent<CrystalGrowth>();
                        crystalGrowthComponent.Initialize(gemstoneName, plantComponent.m_growTime);
                    }

                    Debug.Log($"Added Plant and CrystalGrowth components to {gemstoneName}");
                }
            }
        }
    }
}
