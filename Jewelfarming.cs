using UnityEngine;
using HarmonyLib;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using ItemManager;
using PieceManager;
using ServerSync;
using System.Linq;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using static Jewelfarming.Jewelfarming;

namespace Jewelfarming
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency("org.bepinex.plugins.jewelcrafting")]
    public class Jewelfarming : BaseUnityPlugin
    {
        public static Jewelfarming Instance { get; private set; }  // Add this line
        private const string ModName = "Jewelfarming";
        private const string ModVersion = "1.0.1";
        private const string ModGUID = "org.bepinex.plugins.jewelfarming";

        private static readonly ConfigSync configSync = new(ModName) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        private static ConfigEntry<Toggle> serverConfigLocked = null!;

        private static ConfigEntry<float> growthTimeRed, growthTimeBlue, growthTimeBlack, growthTimePurple;
        private static ConfigEntry<float> growthTimeGreen, growthTimeYellow, growthTimeOrange, growthTimeCyan;

        private enum Toggle { On = 1, Off = 0 }

        public void Awake()
        {
            Instance = this;  // Initialize the instance

            SceneManager.sceneLoaded += OnSceneLoaded;

            // Apply Harmony patches
            Harmony harmony = new(ModGUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Debug.Log("Harmony patches applied.");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainScene")
            {
                StartCoroutine(WaitForZNetSceneAndInitialize());
            }
        }
        private IEnumerator WaitForZNetSceneAndInitialize()
        {
            while (ZNetScene.instance == null)
            {
                Debug.Log("Waiting for ZNetScene instance...");
                yield return null;
            }

            Debug.Log("ZNetScene instance found.");

            while (!ArePrefabsAvailable())
            {
                Debug.Log("Waiting for prefabs to be available...");
                yield return new WaitForSeconds(1f);
            }

            Debug.Log("Prefabs available.");

            growthTimeRed = config("Growth", "Red Crystal Growth Time", 600f, "Time in seconds for red crystal to grow.");
            growthTimeBlue = config("Growth", "Blue Crystal Growth Time", 600f, "Time in seconds for blue crystal to grow.");
            growthTimeBlack = config("Growth", "Black Crystal Growth Time", 600f, "Time in seconds for black crystal to grow.");
            growthTimePurple = config("Growth", "Purple Crystal Growth Time", 600f, "Time in seconds for purple crystal to grow.");
            growthTimeGreen = config("Growth", "Green Crystal Growth Time", 600f, "Time in seconds for green crystal to grow.");
            growthTimeYellow = config("Growth", "Yellow Crystal Growth Time", 600f, "Time in seconds for yellow crystal to grow.");
            growthTimeOrange = config("Growth", "Orange Crystal Growth Time", 600f, "Time in seconds for orange crystal to grow.");
            growthTimeCyan = config("Growth", "Cyan Crystal Growth Time", 600f, "Time in seconds for cyan crystal to grow.");

            serverConfigLocked = config("General", "Lock Configuration", Toggle.On, "If on, only server admins can change config.");
            configSync.AddLockingConfigEntry(serverConfigLocked);

            RegisterCrystalShards();
            Debug.Log("Crystal shards registered.");
        }

        private void RegisterCrystalShards()
        {
            RegisterPiece("JC_Shattered_Red_Crystal", "JC_Raw_Red_Gemstone", growthTimeRed.Value);
            RegisterPiece("JC_Shattered_Blue_Crystal", "JC_Raw_Blue_Gemstone", growthTimeBlue.Value);
            RegisterPiece("JC_Shattered_Black_Crystal", "JC_Raw_Black_Gemstone", growthTimeBlack.Value);
            RegisterPiece("JC_Shattered_Purple_Crystal", "JC_Raw_Purple_Gemstone", growthTimePurple.Value);
            RegisterPiece("JC_Shattered_Green_Crystal", "JC_Raw_Green_Gemstone", growthTimeGreen.Value);
            RegisterPiece("JC_Shattered_Yellow_Crystal", "JC_Raw_Yellow_Gemstone", growthTimeYellow.Value);
            RegisterPiece("JC_Shattered_Orange_Crystal", "JC_Raw_Orange_Gemstone", growthTimeOrange.Value);
            RegisterPiece("JC_Shattered_Cyan_Crystal", "JC_Raw_Cyan_Gemstone", growthTimeCyan.Value);
        }
        private void RegisterPiece(string shardPrefabName, string grownPrefabName, float growthTime)
        {
            // Registration logic for each piece
            GameObject shardPrefab = ZNetScene.instance?.GetPrefab(shardPrefabName);
            if (shardPrefab == null)
            {
                Debug.LogError($"Prefab {shardPrefabName} not found in ZNetScene!");
                return;
            }

            Piece shardPieceComponent = shardPrefab.GetComponent<Piece>();
            if (shardPieceComponent == null)
            {
                Debug.LogError($"Prefab {shardPrefabName} does not have a Piece component!");
                return;
            }

            BuildPiece shardPiece = new BuildPiece(shardPrefabName, shardPrefabName); // Changed second parameter to the name

            // Assign Cultivator as the required tool
            shardPiece.Tool.Add("Cultivator");

            // Set required resources
            shardPiece.RequiredItems.Add(shardPrefabName, 1, true);

            // Assign proper category
            shardPiece.Category.Set(BuildPieceCategory.Misc);

            Debug.Log($"Registered {shardPrefabName} to be planted using the Cultivator.");
        }

        private bool ArePrefabsAvailable()
        {
            string[] prefabNames =
            {
                "JC_Shattered_Red_Crystal", "JC_Shattered_Blue_Crystal", "JC_Shattered_Black_Crystal",
                "JC_Shattered_Purple_Crystal", "JC_Shattered_Green_Crystal", "JC_Shattered_Yellow_Crystal",
                "JC_Shattered_Orange_Crystal", "JC_Shattered_Cyan_Crystal"
            };

            return prefabNames.All(name => ZNetScene.instance?.GetPrefab(name) != null);
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description) => Config.Bind(group, name, value, new ConfigDescription(description));
        public class CrystalGrowth : MonoBehaviour
        {
            private string _finalPrefab;
            private float _growthTime;
            private ZNetView _zNetView;

            public void Initialize(string finalPrefab, float growthTime)
            {
                _finalPrefab = finalPrefab;
                _growthTime = growthTime;
                _zNetView = GetComponent<ZNetView>();

                if (_zNetView == null)
                {
                    Debug.LogError("ZNetView missing on CrystalGrowth object!");
                    return;
                }

                if (_zNetView.IsOwner() && !_zNetView.GetZDO().GetBool("isGrowing", false))
                {
                    _zNetView.GetZDO().Set("growthStart", ZNet.instance.GetTime().Ticks);
                    _zNetView.GetZDO().Set("isGrowing", true);
                }
            }

            private void Update()
            {
                if (ZNet.instance?.IsServer() != true || _zNetView == null || !_zNetView.IsOwner()) return;

                long startTime = _zNetView.GetZDO().GetLong("growthStart", 0);
                if (startTime == 0) return;

                double elapsedSeconds = (ZNet.instance.GetTime().Ticks - startTime) / 1e7;
                if (elapsedSeconds >= _growthTime)
                {
                    Grow();
                }
            }

            private void Grow()
            {
                GameObject grownPrefab = ZNetScene.instance.GetPrefab(_finalPrefab);
                if (grownPrefab == null)
                {
                    Debug.LogError($"Grown prefab {_finalPrefab} not found!");
                    return;
                }

                Instantiate(grownPrefab, transform.position, transform.rotation);
                ZNetScene.instance.Destroy(gameObject);
            }
        }
    }
}