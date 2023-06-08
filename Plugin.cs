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
global using System.Globalization;

namespace ReplayMod
{
    [BepInPlugin("GUID_du_plugin", "ReplayMod", "0.1.6")]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<Basics>();
            ClassInjector.RegisterTypeInIl2Cpp<ReplayController>();
            ClassInjector.RegisterTypeInIl2Cpp<RecordController>();
            ClassInjector.RegisterTypeInIl2Cpp<Menu>();
            Utility.CreateConfigFile();
            Utility.DownloadMinimapDatas();

            Variables.DebugDataCallbacks = new System.Collections.Generic.Dictionary<string, System.Func<string>>();
            MenuFunctions.CheckMenuFileExists();
            MenuFunctions.LoadMenuLayout();
            MenuFunctions.RegisterDefaultCallbacks();

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

        public class Menu : MonoBehaviour
        {
            DateTime start = DateTime.Now;
            public Text textMenu;

            // Importation des fonctions de la librairie user32.dll
            [DllImport("user32.dll")]
            private static extern short GetKeyState(int nVirtKey);
            [DllImport("user32.dll")]
            private static extern short GetAsyncKeyState(int vKey);
            [DllImport("user32.dll")]
            private static extern int GetSystemMetrics(int nIndex);

            // Constantes pour la détection du clic droit et de la molette de la souris
            const int VK_RBUTTON = 0x02;

            void Update()
            {
                // Calcule la différence de temps depuis la dernière mise à jour
                TimeSpan elapsed = DateTime.Now - start;
                Variables.displayButton0 = MenuFunctions.HandleMenuDisplay(0, () => "Replay File", MenuFunctions.GetSelectedReplayFileName);
                Variables.displayButton1 = MenuFunctions.HandleMenuDisplay(1, () => "Replay Speed", () => $"= {Variables.replaySpeed}");
                Variables.displayButton2 = MenuFunctions.HandleMenuDisplay(2, () => "Start Replay", MenuFunctions.GetSelectedReplayView);
                Variables.displayButton3 = MenuFunctions.HandleMenuDisplay(3, () => "Force Record", MenuFunctions.GetForceRecordState);
                Variables.displayButton4 = MenuFunctions.HandleMenuDisplay(4, () => "Stop Replay", () => $"");

                // Exécute les actions de menu si 'menuTrigger' est vrai et assez de temps s'est écoulé
                if (Variables.menuTrigger && elapsed.TotalMilliseconds >= 150)
                {
                    Variables.menuSpeedHelper2 = 0;
                    // Met à jour le 'menuSelector' et joue le son du menu en fonction de la touche pressée
                    if (Input.GetKey("[4]") || Input.GetKey("[6]") || Input.GetKey("[5]"))
                    {

                        if (elapsed.TotalMilliseconds <= 200)
                            Variables.menuSpeedHelper += 2;
                        if (Variables.menuSpeedHelper > 8)
                            Variables.menuSpeed = 5;
                        else
                            Variables.menuSpeed = 1;

                        // Joue le son du menu si 'clientBody' est non null
                        if (Variables.clientBody != null)
                        {
                            Utility.PlayMenuSound();
                        }

                        if (Input.GetKey("[4]"))
                        {
                            if (!Variables.onButton)
                                Variables.menuSelector = Variables.menuSelector > 0 ? Variables.menuSelector - 1 : Variables.buttonStates.Length;
                            else
                            {
                                switch (Variables.menuSelector)
                                {
                                    case 0:
                                        Variables.replayFile = Variables.replayFile > 0 ? Variables.replayFile - 1 : Variables.maxReplayFile;
                                        break;
                                    case 1:
                                        Variables.replaySpeed = (float)Math.Round(Variables.replaySpeed - 0.1f * Variables.menuSpeed, 1);
                                        if (Variables.replaySpeed < 0)
                                            Variables.replaySpeed = 0;
                                        break;
                                    case 2:
                                        Variables.subMenuSelector = Variables.subMenuSelector > 0 ? Variables.subMenuSelector - 1 : 3;
                                        break;
                                    default:
                                        return;
                                }
                            }
                        }

                        if (Input.GetKey("[6]"))
                        {
                            if (!Variables.onButton)
                                Variables.menuSelector = Variables.menuSelector < Variables.buttonStates.Length ? Variables.menuSelector + 1 : 0;
                            else
                            {
                                switch (Variables.menuSelector)
                                {
                                    case 0:
                                        Variables.replayFile = Variables.replayFile < Variables.maxReplayFile ? Variables.replayFile + 1 : 0;
                                        break;
                                    case 1:
                                        Variables.replaySpeed = Variables.replaySpeed < 10 ? Variables.replaySpeed + 0.1f * Variables.menuSpeed : 10;
                                        Variables.replaySpeed = (float)Math.Round(Variables.replaySpeed, 1); // Arrondir à une décimale
                                        break;
                                    case 2:
                                        Variables.subMenuSelector = Variables.subMenuSelector < 3 ? Variables.subMenuSelector + 1 : 0;
                                        break;
                                    default:
                                        return;
                                }
                            }
                        }

                        if (Input.GetKey("[5]"))
                        {
                            if (Variables.menuSelector < Variables.buttonStates.Length)
                            {
                                if (Variables.menuSelector == 2 && !Variables.onButton)
                                    Variables.subMenuSelector = 0;
                                MenuFunctions.ExecuteSubMenuAction();
                                Variables.buttonStates[Variables.menuSelector] = !Variables.buttonStates[Variables.menuSelector];
                                Variables.onButton = Variables.buttonStates[Variables.menuSelector];

                                if (Variables.menuSelector == 4)
                                {
                                    Variables.onButton = false;
                                    Variables.buttonStates[4] = false;
                                }
                            }
                        }
                        // Met à jour le moment de la dernière action
                        start = DateTime.Now;
                    }
                }
                if (elapsed.TotalMilliseconds >= 150 + Variables.menuSpeedHelper2)
                {
                    if (Variables.menuSpeedHelper > 0)
                        Variables.menuSpeedHelper -= 1;
                    Variables.menuSpeedHelper2 += 150;


                }
            }
        }
        public class Basics : MonoBehaviour
        {
            public Text text;
            private bool isStarted = false;

            DateTime start = DateTime.Now;
            void Update()
            {
                DateTime end = DateTime.Now;
                if (!isStarted)
                {
                    Utility.LoadConfigurations();

                    Variables.buttonStates = new bool[5];
                    Variables.buttonStates[0] = false; 
                    Variables.buttonStates[1] = false;
                    Variables.buttonStates[2] = false;
                    Variables.buttonStates[3] = false;
                    Variables.buttonStates[4] = false;

                    isStarted = true;
                }

                if (Input.GetKeyDown("f4"))
                {
                    if (Variables.clientBody != null)
                    {
                        PlayerInventory.Instance.woshSfx.pitch = 200;
                        PlayerInventory.Instance.woshSfx.Play();
                    }
                    Variables.menuTrigger = !Variables.menuTrigger;
                    if (Variables.menuTrigger)
                    {
                        ChatBox.Instance.ForceMessage("■<color=orange>Menu <color=blue>ON</color></color>■");
                        ChatBox.Instance.ForceMessage("■<color=orange>navigate the menu using the numeric keypad (VER NUM ON)</color>■");
                        ChatBox.Instance.ForceMessage("■<color=orange>press 4 or 6 to move forwards or backwards, and 5 to select</color>■");
                    }
                    else
                        ChatBox.Instance.ForceMessage("■<color=orange>Menu <color=red>OFF</color></color>■");
                }

                TimeSpan ts = (end - start);


                if (ts.TotalMilliseconds >= 200)
                {
                    start = DateTime.Now;
                    text.text = Variables.menuTrigger ? MenuFunctions.FormatLayout() : "";
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
            text.fontSize = 16;
            text.raycastTarget = false;

            Basics basics = menuObject.AddComponent<Basics>();

            ReplayController replay = menuObject.AddComponent<ReplayController>();
            RecordController recording = menuObject.AddComponent<RecordController>();
            Menu menu = menuObject.AddComponent<Menu>();
            basics.text = text;

            menuObject.transform.SetParent(__instance.transform);
            menuObject.transform.localPosition = new UnityEngine.Vector3(menuObject.transform.localPosition.x, -menuObject.transform.localPosition.y, menuObject.transform.localPosition.z);
            RectTransform rt = menuObject.GetComponent<RectTransform>();
            rt.pivot = new UnityEngine.Vector2(0, 1);
            rt.sizeDelta = new UnityEngine.Vector2(1000, 1000);
        }
    }
}
