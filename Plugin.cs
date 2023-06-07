//Using
global using BepInEx;
global using BepInEx.IL2CPP;
global using UnityEngine;
global using UnityEngine.UI;
global using UnhollowerRuntimeLib;
global using HarmonyLib;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Runtime.InteropServices;
global using System.IO;
global using System.Text;
global using TMPro;
global using Dummiesman;
global using System.Net.Http;
global using System.Threading.Tasks;
global using System.IO.Compression;

namespace ReplayMod
{
    [BepInPlugin("GUID_du_plugin", "ReplayMod", "0.1.5")]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<Basics>();
            ClassInjector.RegisterTypeInIl2Cpp<ReplayController>();
            ClassInjector.RegisterTypeInIl2Cpp<RecordController>();
            Utility.CreateConfigFile();
            Utility.DownloadMinimapDatas();

            // Plugin startup logic
            Harmony.CreateAndPatchAll(typeof(Plugin));
        }    
        public class ReplayController : MonoBehaviour
        {
            private float elapsed = 0f; // Elapsed time variable
            private bool init = false;

            public void Update()
            {
                if (!init)
                {
                    Variables.fixMapTimeStamp = DateTime.Now;
                    Variables.isReplayReaderInitialized = false;
                    Variables.clientCloneVisibility = true;
                    Variables.otherPlayerCloneVisibility = true;
                    init = true;
                }
                HandleClientBody();
                elapsed += Time.deltaTime;

                if (elapsed >= 1 / (Variables.replayFPS * Variables.replaySpeed) && Variables.replayTrigger)
                {
                    Variables.replaySafeClose = false;
                    CheckGameMode();
                    CheckPracticeMode();
                    InitializeReaderIfNecessary();
                    HandlePlayerVisibility();
                    ReadAndProcessLine();
                    elapsed = 0f;
                }
            }

            private void HandleClientBody()
            {
                if (!Variables.minimap)
                {
                    Variables.minimap = GameObject.Find("Carte") ?? new GameObject("Carte");
                }

                if (Variables.replayTrigger && Variables.mapId == Variables.replayMap && Variables.gamemodeId == 13 &&
                    (Variables.cinematicTrigger || Variables.povTrigger))
                {
                    Variables.clientBody.useGravity = false;
                    Variables.clientBody.isKinematic = true;
                }
            }

            private void CheckGameMode()
            {
                if (Variables.gamemodeId != 0 && Variables.gamemodeId != 13)
                {
                    Variables.replayStop = true;
                }
            }

            private void CheckPracticeMode()
            {
                if (GameData.GetGameModeIdAsString() != "13" && !Variables.minimapTrigger)
                {
                    Variables.replayStop = true;
                    Variables.povTrigger = false;
                    Variables.cinematicTrigger = false;
                    Variables.chatBox.ForceMessage("■<color=red>Wrong mode, are you in Practice?</color>■");
                }
            }

            private void InitializeReaderIfNecessary()
            {
                if (!Variables.isReplayReaderInitialized)
                {
                    ReplayControllerFunctions.InitializeReader();
                }
            }

            private void HandlePlayerVisibility()
            {
                if (!Variables.povTrigger && !Variables.cinematicTrigger)
                {
                    if (!Variables.clientCloneVisibility)
                    {
                        ReplayControllerFunctions.TogglePlayerVisibility(Variables.clientClone);
                        Variables.clientCloneVisibility = true;
                    }
                    if (Variables.checkingForceRecord != "force" && !Variables.otherPlayerCloneVisibility)
                    {
                        ReplayControllerFunctions.TogglePlayerVisibility(Variables.otherPlayerClone);
                        Variables.otherPlayerCloneVisibility = true;
                    }
                }
            }

            private void ReadAndProcessLine()
            {
                string line = Variables.csvReplayReader.ReadLine();

                if (Variables.checkingForceRecord == "force" && Variables.otherPlayerCloneVisibility)
                {
                    ReplayControllerFunctions.TogglePlayerVisibility(Variables.otherPlayerClone);
                    Variables.otherPlayerCloneVisibility = false;
                }

                if (line != null && !Variables.replayStop)
                {
                    ReplayControllerFunctions.ExtractData(line);

                    ReplayControllerFunctions.HandleMinimap();
                    ReplayControllerFunctions.HandleTag();
                    ReplayControllerFunctions.HandlePOVTrigger();
                    ReplayControllerFunctions.HandleCinematicTrigger();
                }
                else if (!Variables.replaySafeClose)
                {
                    ReplayControllerFunctions.HandleReplayEnd();
                }
            }
        }

        public class RecordController : MonoBehaviour
        {
            private DateTime startTimer = DateTime.Now;
            private bool init = false;

            private void Update()
            {
                if (!init)
                {
                    Variables.isForceRecord = false;
                    Variables.recording = false;
                    Variables.lastRecordTime = DateTime.Now;
                    init = true;
                }
                if (Variables.forceRecord)
                {
                    RecordControllerFunctions.HandleForceRecord();
                }
                else if (Variables.isForceRecord)
                {
                    RecordControllerFunctions.RenameFile();
                    Variables.isForceRecord = false;
                }

                if (Variables.clientObject != null && GameData.GetGameStateAsString() == "Playing")
                {
                    RecordControllerFunctions.HandleGamePlay();
                }
                else
                {
                    RecordControllerFunctions.EndGame();
                }
            }
        }

        public class Basics : MonoBehaviour
        {
            private bool isStarted = false;

            void Update()
            {
                if (!isStarted)
                {
                    Utility.LoadConfigurations();
                    isStarted = true;
                }
            }

        }

        [HarmonyPatch(typeof(ChatBox), nameof(ChatBox.AppendMessage))]
        [HarmonyPostfix]
        static void OnReceiveMessage(ChatBox __instance, ulong __0, string __1, string __2)
        {
            // If the received message is from the player
            if (__2 == ClientData.GetPlayerUsernameAsString())
            {
                Utility.ChatCommands(__1);
            }
        }
        [HarmonyPatch(typeof(MonoBehaviourPublicGaroloGaObInCacachGaUnique), "Awake")]
        [HarmonyPostfix]
        public static void UIAwakePatch(MonoBehaviourPublicGaroloGaObInCacachGaUnique __instance)
        {
            GameObject menuObject = new GameObject();
            Text text = menuObject.AddComponent<Text>();
            text.font = (Font)Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.raycastTarget = false;

            Basics basics = menuObject.AddComponent<Basics>();

            ReplayController replay = menuObject.AddComponent<ReplayController>();
            RecordController recording = menuObject.AddComponent<RecordController>();

            menuObject.transform.SetParent(__instance.transform);
            menuObject.transform.localPosition = new UnityEngine.Vector3(menuObject.transform.localPosition.x, -menuObject.transform.localPosition.y, menuObject.transform.localPosition.z);
            RectTransform rt = menuObject.GetComponent<RectTransform>();
            rt.pivot = new UnityEngine.Vector2(0, 1);
            rt.sizeDelta = new UnityEngine.Vector2(1000, 1000);
        }
    }
}
