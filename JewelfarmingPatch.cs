using UnityEngine;
using HarmonyLib;
using System.Collections;
using UnityEngine.SceneManagement;
using static Jewelfarming.Jewelfarming;

namespace Jewelfarming
{
    [HarmonyPatch]
    public static class JewelfarmingPatch
    {
        [HarmonyPatch(typeof(FejdStartup), "Start")]
        [HarmonyPostfix]
        public static void FejdStartupStartPostfix()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "main")
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Jewelfarming.Instance.StartCoroutine(WaitForPrefabsAndInitialize());  // Use the instance here
            }
        }

        private static IEnumerator WaitForPrefabsAndInitialize()
        {
            string[] rawGemstoneNames =
            {
                "JC_Raw_Red_Gemstone", "JC_Raw_Blue_Gemstone", "JC_Raw_Black_Gemstone",
                "JC_Raw_Purple_Gemstone", "JC_Raw_Green_Gemstone", "JC_Raw_Yellow_Gemstone",
                "JC_Raw_Orange_Gemstone", "JC_Raw_Cyan_Gemstone"
            };

            while (true)
            {
                bool allPrefabsAvailable = true;

                foreach (var gemstoneName in rawGemstoneNames)
                {
                    if (ZNetScene.instance.GetPrefab(gemstoneName) == null)
                    {
                        allPrefabsAvailable = false;
                        Debug.Log($"Waiting for prefab {gemstoneName} to be available...");
                        break;
                    }
                }

                if (allPrefabsAvailable)
                {
                    break;
                }

                yield return new WaitForSeconds(1f); // Wait for a second before checking again
            }

            InitializeJewelfarming();
        }

        private static void InitializeJewelfarming()
        {
            if (ZNetScene.instance == null)
            {
                Debug.LogError("ZNetScene instance is null!");
                return;
            }

            Debug.Log("Initializing Jewelfarming after world load.");

            string[] rawGemstoneNames =
            {
                "JC_Raw_Red_Gemstone", "JC_Raw_Blue_Gemstone", "JC_Raw_Black_Gemstone",
                "JC_Raw_Purple_Gemstone", "JC_Raw_Green_Gemstone", "JC_Raw_Yellow_Gemstone",
                "JC_Raw_Orange_Gemstone", "JC_Raw_Cyan_Gemstone"
            };

            foreach (var gemstoneName in rawGemstoneNames)
            {
                GameObject gemstonePrefab = ZNetScene.instance.GetPrefab(gemstoneName);
                if (gemstonePrefab == null)
                {
                    Debug.LogError($"Gemstone prefab {gemstoneName} not found!");
                    continue;
                }

                if (gemstonePrefab.GetComponent<Plant>() == null)
                {
                    Plant plantComponent = gemstonePrefab.AddComponent<Plant>();
                    plantComponent.m_growTime = 600f; // Example growth time, adjust as necessary

                    if (gemstonePrefab.GetComponent<CrystalGrowth>() == null)
                    {
                        CrystalGrowth crystalGrowthComponent = gemstonePrefab.AddComponent<CrystalGrowth>();
                        crystalGrowthComponent.Initialize(gemstoneName, plantComponent.m_growTime);
                    }

                    Debug.Log($"Added Plant and CrystalGrowth components to {gemstoneName}");
                }
            }

            Debug.Log("Finished initializing Jewelfarming.");
        }
    }
}
