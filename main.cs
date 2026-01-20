using BepInEx;
using HarmonyLib;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static MP3PlayerAffectsHappiness.SharedState; 
using static MP3PlayerAffectsHappiness.MP3PlayerAffectsHappiness;
using BepInEx.Logging;
using System.Runtime.CompilerServices;
using System.CodeDom.Compiler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;

namespace MP3PlayerAffectsHappiness
{
    public static class SharedState
    {
        public static bool MP3IsPlaying;
        public static int MP3HappinessMode = int.MinValue;
    }

    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class MP3PlayerAffectsHappiness : BaseUnityPlugin
    {
        public static ManualLogSource logger;
        public const string pluginGuid = "shushu.casualtiesunknown.mp3playeraffectshappiness";
        public const string pluginName = "MP3 Player Affects Happiness";
        public const string pluginVersion = "1.12.25";

        public static MP3PlayerAffectsHappiness Instance;

        public void Awake()
        {
            Instance = this;
            logger = Logger;

            logger.LogInfo("Awake() ran - mod loaded!");

            Harmony harmony = new Harmony(pluginGuid);
            
            harmony.PatchAll();

            //var ToMainMenuOriginal = AccessTools.Method(typeof(PlayerCamera), "ToMainMenu");
            //var ToMainMenuPost = typeof(MyPatches).GetMethod("ToMainMenu_MyPatch");

            //var SaveGameOriginal = AccessTools.Method(typeof(SaveSystem), "SaveGame");
            //var SaveGamePost = typeof(MyPatches).GetMethod("SaveGame_MyPatch");

            //var TryLoadGameOriginal = AccessTools.Method(typeof(SaveSystem), "TryLoadGame");
            //var TryloadGamePre = typeof(MyPatches).GetMethod("TryLoadGame_MyPatch");

            //var PlayOriginal = AccessTools.Method(typeof(MP3Menu), "Play");
            //var PlayPost = typeof(MyPatches).GetMethod("Play_MyPatch");

            //var StartOriginal = AccessTools.Method(typeof(PlayerCamera), "Start");
            //var StartPost = typeof(MyPatches).GetMethod("Start_MyPatch");

            //harmony.Patch(ToMainMenuOriginal, postfix: new HarmonyMethod(ToMainMenuPost));
            //Log("Patched ToMainMenu");

            //harmony.Patch(PlayOriginal, postfix: new HarmonyMethod(PlayPost));
            //Log("Patched Play");

            //harmony.Patch(StartOriginal, postfix: new HarmonyMethod(StartPost));
            //Log("Patched Start");

            //harmony.Patch(SaveGameOriginal, postfix: new HarmonyMethod(SaveGamePost));
            //Log("Patched SaveGame");

            //harmony.Patch(TryLoadGameOriginal, prefix: new HarmonyMethod(TryloadGamePre));
            //Log("Patched TryLoadGame");
        }

        public static void Log(string message)
        {
            logger.LogInfo(message);
        }

        public static IEnumerator GiveHappiness(MP3Menu mp3)
        {
            Log("Give Happiness Instance Enumerator Initiated");

            var body = PlayerCamera.main?.body;
            while (MP3IsPlaying)
            {
                if (MusicManager.main.timeSinceFinishedPlaying == 0)
                {
                    if (body.happiness > -65 && body.happiness < 75)
                    {
                        body.happiness += MP3HappinessMode * 0.005f;
                    }
                } else
                {
                    MP3IsPlaying = false;
                    yield break;
                }

                yield return new WaitForSeconds(1f);
            }
        }
    }

    public class MyPatches
    {
        [HarmonyPatch(typeof(PlayerCamera))]
        [HarmonyPatch("ToMainMenu")]
        [HarmonyPostfix]
        public static void ToMainMenu_MyPatch(PlayerCamera __instance)
        {
            MP3IsPlaying = false;
            MP3HappinessMode = int.MinValue;
            Log("Reset MP3HappinessMode");
        }

        [HarmonyPatch(typeof(SaveSystem))]
        [HarmonyPatch("SaveGame")]
        [HarmonyPostfix]
        public static void SaveGame_MyPatch(SaveSystem __instance)
        {
            Log("SaveGame ran");
            string uncompressed = Traverse.Create(__instance).Field("uncompressed").GetValue<string>();
            if (string.IsNullOrEmpty(uncompressed))
            {
                Log("SaveGame patch: uncompressed JSON was empty, skipping. MP3HappinessMode will be rerolled upon waking up.");
                return;
            }
            JObject saveInfo = JObject.Parse(uncompressed);

            try
            {
                saveInfo["MP3HappinessMode"] = MP3HappinessMode;
            } catch
            {
                Log("SaveGame patch: MP3HappinessMode doesn't exist");
                return;
            }

            string newUncompressed = JsonConvert.SerializeObject(saveInfo, Formatting.None,
                new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
            File.WriteAllBytes(Application.persistentDataPath + "\\save.sv",
                SaveSystem.Zip(newUncompressed));
        }

        [HarmonyPatch(typeof(SaveSystem))]
        [HarmonyPatch("TryLoadGame")]
        [HarmonyPrefix]
        public static void TryLoadGame_MyPatch(SaveSystem __instance)
        {
            Log("TryLoadGame ran");
            JObject jobject;
            if (!SaveSystem.loadedRun || !SaveSystem.HasSave())
            {
                return;
            }
            try 
            {
                jobject = JObject.Parse(SaveSystem.Unzip(File.ReadAllBytes(Application.persistentDataPath + "\\save.sv")));
            } catch
            {
                Log("TryLoadGame patch: jobject SaveInfo was empty, skipping. MP3HappinessMode will be rerolled upon waking up.");
                return;
            }
            if (jobject.ContainsKey("MP3HappinessMode"))
            {
                MP3HappinessMode = (int)jobject["MP3HappinessMode"];
            } else
            {
                Log("TryLoadGame patch: jobject SaveInfo did not have MP3HappinessMode, skipping. MP3HappinessMode will be rerolled upon waking up.");
            }
        }

        [HarmonyPatch(typeof(MP3Menu))]
        [HarmonyPatch("Play")]
        [HarmonyPostfix]
        public static void Play_MyPatch(MP3Menu __instance)
        {
            Log("Play Patch loaded");

            MP3IsPlaying = true;
            MP3PlayerAffectsHappiness.Instance.StartCoroutine(GiveHappiness(__instance));
        }

        [HarmonyPatch(typeof(PlayerCamera))]
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Start_MyPatch(PlayerCamera __instance)
        {
            Log("Start ran");
            if (MP3HappinessMode == int.MinValue)
            {
                int RandHappinessRoll = UnityEngine.Random.Range(0, 101);
                switch (RandHappinessRoll)
                {
                    case 0:
                    case 1:
                        MP3HappinessMode = -1;
                        break;

                    case 2:
                    case 3:
                    case 4:
                        MP3HappinessMode = 0;
                        break;

                    default:
                        MP3HappinessMode = 1;
                        break;
                }
                Log("Randomly rolled MP3HappinessMode as " + MP3HappinessMode);
            }
        }
    }
}
