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
    [BepInPlugin("GUID_du_plugin", "ReplayMod", "0.1.7")]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<Basics>();
            ClassInjector.RegisterTypeInIl2Cpp<ReplayManager>();
            ClassInjector.RegisterTypeInIl2Cpp<RecordManager>();
            ClassInjector.RegisterTypeInIl2Cpp<MenuManager>();
            ClassInjector.RegisterTypeInIl2Cpp<MedalLikeRecord>();
            Utility.CreateConfigFile();
            Utility.DownloadMinimapDatas();

            Variables.DebugDataCallbacks = new System.Collections.Generic.Dictionary<string, System.Func<string>>();
            MenuFunctions.CheckMenuFileExists();
            MenuFunctions.LoadMenuLayout();
            MenuFunctions.RegisterDefaultCallbacks();

            // Plugin startup logic
            Harmony.CreateAndPatchAll(typeof(Plugin));
        }    
        public class ReplayManager : MonoBehaviour
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
                if (GameData.GetGameModId().ToString() != "13" && !Variables.minimapTrigger)
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
                    ReplayFunctions.InitializeReader();
                }
            }

            private void HandlePlayerVisibility()
            {
                if (!Variables.povTrigger && !Variables.cinematicTrigger)
                {
                    if (!Variables.clientCloneVisibility)
                    {
                        ReplayFunctions.TogglePlayerVisibility(Variables.clientClone);
                        Variables.clientCloneVisibility = true;
                    }
                    if (Variables.checkingForceRecord != "force" && !Variables.otherPlayerCloneVisibility)
                    {
                        ReplayFunctions.TogglePlayerVisibility(Variables.otherPlayerClone);
                        Variables.otherPlayerCloneVisibility = true;
                    }
                }
            }

            private void ReadAndProcessLine()
            {
                string line = Variables.csvReplayReader.ReadLine();

                if (Variables.checkingForceRecord == "force" && Variables.otherPlayerCloneVisibility)
                {
                    ReplayFunctions.TogglePlayerVisibility(Variables.otherPlayerClone);
                    Variables.otherPlayerCloneVisibility = false;
                }

                if (line != null && !Variables.replayStop)
                {
                    ReplayFunctions.ExtractData(line);

                    ReplayFunctions.HandleMinimap();
                    ReplayFunctions.HandleTag();
                    ReplayFunctions.HandlePOVTrigger();
                    ReplayFunctions.HandleCinematicTrigger();
                }
                else if (!Variables.replaySafeClose)
                {
                    ReplayFunctions.HandleReplayEnd();
                }
            }
        }

        public class MedalLikeRecord : MonoBehaviour
        {
            bool init;
            void Update()
            {
                if (Variables.medalRecord)
                {
                    if (!init)
                    {
                        Variables.gameManager = GameObject.Find("/GameManager (1)").GetComponent<GameManager>();
                        Variables.lastRecordTimeMedal = DateTime.Now;
                        RecordFunctions.InitRecordMedal("force", "Record");
                        init = true;
                    }

                    if ((DateTime.Now - Variables.lastRecordTimeMedal).TotalSeconds >= 1.0 / Variables.recordFPS && init)
                    {
                        Utility.LogAllDataForMedal(Variables.replayFileNameMedal, Variables.startGameMedal);
                        Variables.lastRecordTimeMedal = DateTime.Now;
                    }
                }
                else if (init)
                {
                    RecordFunctions.RenameFileMedal();
                    init = false;
                }
            }
        }

        public class RecordManager : MonoBehaviour
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
                    RecordFunctions.HandleForceRecord();
                }
                else if (Variables.isForceRecord)
                {
                    RecordFunctions.RenameFile();
                    Variables.isForceRecord = false;
                }

                if (Variables.clientObject != null && GameData.GetGameStateAsString() == "Playing")
                {
                    RecordFunctions.HandleGamePlay();
                }
                else
                {
                    RecordFunctions.EndGame();
                }
            }
        }

        public class MenuManager : MonoBehaviour
        {
            DateTime lastActionTime = DateTime.Now;
            public Text textMenu;

            private const string UPKEY = "[4]";
            private const string SELECTKEY = "[5]";
            private const string DOWNKEY = "[6]";

            private const float DEFAULT_REPLAY_SPEED_INCREMENT = 0.1f;
            private const float MAX_REPLAY_SPEED = 10.0f;
            private const int MIN_REPLAY_TIME = 150;

            // Importation des fonctions de la librairie user32.dll
            [DllImport("user32.dll")]
            private static extern short GetKeyState(int nVirtKey);
            [DllImport("user32.dll")]
            private static extern short GetAsyncKeyState(int vKey);
            [DllImport("user32.dll")]
            private static extern int GetSystemMetrics(int nIndex);

            void Update()
            {
                TimeSpan elapsed = DateTime.Now - lastActionTime;

                HandleMenuDisplays();
                HandleMenuActions(elapsed);
                HandleMenuSpeedHelper(elapsed);
            }

            private void HandleMenuDisplays()
            {
                Variables.displayButton0 = MenuFunctions.HandleMenuDisplay(0, () => "Replay File", MenuFunctions.GetSelectedReplayFileName);
                Variables.displayButton1 = MenuFunctions.HandleMenuDisplay(1, () => "Replay Speed", () => $"= {Variables.replaySpeed}");
                Variables.displayButton2 = MenuFunctions.HandleMenuDisplay(2, () => "Start Replay", MenuFunctions.GetSelectedReplayView);
                Variables.displayButton3 = MenuFunctions.HandleMenuDisplay(3, () => "Force Record", MenuFunctions.GetForceRecordState);
                Variables.displayButton4 = MenuFunctions.HandleMenuDisplay(4, () => "Stop Replay", () => $"");
                Variables.displayButton5 = MenuFunctions.HandleMenuDisplay(5, () => "Format file for Bot Tech", () => $"");
                Variables.displayButton6 = MenuFunctions.HandleMenuDisplay(6, () => "Record like Medal", MenuFunctions.GetMedalRecordState);
            }

            private void HandleMenuActions(TimeSpan elapsed)
            {
                if (!Variables.menuTrigger || elapsed.TotalMilliseconds < MIN_REPLAY_TIME)
                {
                    return;
                }

                Variables.menuSpeedHelper2 = 0;

                bool f5KeyPressed = Input.GetKey(UPKEY);
                bool f6KeyPressed = Input.GetKey(SELECTKEY);
                bool f7KeyPressed = Input.GetKey(DOWNKEY);
                if (f5KeyPressed || f6KeyPressed || f7KeyPressed)
                {
                    UpdateMenuSpeed(elapsed);
                    HandleKeyActions(f5KeyPressed, f6KeyPressed, f7KeyPressed);
                    lastActionTime = DateTime.Now;
                }
            }

            private void HandleMenuSpeedHelper(TimeSpan elapsed)
            {
                if (elapsed.TotalMilliseconds >= MIN_REPLAY_TIME + Variables.menuSpeedHelper2)
                {
                    if (Variables.menuSpeedHelper > 0)
                        Variables.menuSpeedHelper -= 1;
                    Variables.menuSpeedHelper2 += MIN_REPLAY_TIME;
                }
            }

            private void UpdateMenuSpeed(TimeSpan elapsed)
            {
                if (elapsed.TotalMilliseconds <= 200)
                    Variables.menuSpeedHelper += 2;
                if (Variables.menuSpeedHelper > 8)
                    Variables.menuSpeed = 5;
                else
                    Variables.menuSpeed = 1;

                // Play menu sound if 'clientBody' is non-null
                if (Variables.clientBody != null)
                {
                    Utility.PlayMenuSound();
                }
            }

            private void HandleKeyActions(bool f5KeyPressed, bool f6KeyPressed, bool f7KeyPressed)
            {
                if (f5KeyPressed)
                {
                    HandleF5KeyPressed();
                }

                if (f6KeyPressed)
                {
                    HandleF6KeyPressed();
                }

                if (f7KeyPressed)
                {
                    HandleF7KeyPressed();
                }
            }

            private void HandleF5KeyPressed()
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
                            Variables.replaySpeed = (float)Math.Round(Variables.replaySpeed - DEFAULT_REPLAY_SPEED_INCREMENT * Variables.menuSpeed, 1);
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

            private void HandleF7KeyPressed()
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
                            Variables.replaySpeed = Variables.replaySpeed < MAX_REPLAY_SPEED ? Variables.replaySpeed + DEFAULT_REPLAY_SPEED_INCREMENT * Variables.menuSpeed : MAX_REPLAY_SPEED;
                            Variables.replaySpeed = (float)Math.Round(Variables.replaySpeed, 1); // Round to one decimal place
                            break;
                        case 2:
                            Variables.subMenuSelector = Variables.subMenuSelector < 3 ? Variables.subMenuSelector + 1 : 0;
                            break;
                        default:
                            return;
                    }
                }
            }

            public static void HandleF6KeyPressed()
            {
                if (Variables.menuSelector < Variables.buttonStates.Length)
                {
                    if (Variables.menuSelector == 2 && !Variables.onButton)
                        Variables.subMenuSelector = 0;

                    bool previousButtonState = Variables.buttonStates[Variables.menuSelector];
                    Variables.buttonStates[Variables.menuSelector] = !previousButtonState;

                    MenuFunctions.ExecuteSubMenuAction();

                    Variables.onButton = Variables.buttonStates[Variables.menuSelector];

                    if (Variables.menuSelector == 4 || Variables.menuSelector == 5)
                    {
                        Variables.onButton = false;
                        Variables.buttonStates[4] = false;
                        Variables.buttonStates[5] = false;
                    }
                }
            }
        }
        public class Basics : MonoBehaviour
        {
            public Text text;
            private float elapsedTime = 0f;
            private bool init;

            private const string MENU_ON_MSG = "■<color=orange>MenuManager <color=blue>ON</color></color>■";
            private const string MENU_OFF_MSG = "■<color=orange>MenuManager <color=red>OFF</color></color>■";
            private const string NAVIGATION_MSG = "■<color=orange>navigate the menu using the numeric keypad (VER NUM ON)</color>■";
            private const string SELECTION_MSG = "■<color=orange>press 4 or 6 to move forwards or backwards, and 5 to select</color>■";


            void Update()
            {
                if (!init)
                {
                    Utility.LoadConfigurations();
                    Variables.buttonStates = new bool[7];
                    MenuManager.HandleF6KeyPressed();
                    MenuManager.HandleF6KeyPressed();
                    init = true;
                }
                if (Input.GetKeyDown("f4"))
                {
                    if (Variables.clientBody != null)
                    {
                        PlayerInventory.Instance.woshSfx.pitch = 200;
                        PlayerInventory.Instance.woshSfx.Play();
                    }

                    Variables.menuTrigger = !Variables.menuTrigger;
                    ChatBox.Instance.ForceMessage(Variables.menuTrigger ? MENU_ON_MSG : MENU_OFF_MSG);
                    if (Variables.menuTrigger)
                    {
                        ChatBox.Instance.ForceMessage(NAVIGATION_MSG);
                        ChatBox.Instance.ForceMessage(SELECTION_MSG);
                    }
                }

                elapsedTime += Time.deltaTime;
                if (elapsedTime >= 0.2f)
                {
                    text.text = Variables.menuTrigger ? MenuFunctions.FormatLayout() : "";
                    elapsedTime = 0f; // reset the timer
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

            ReplayManager replay = menuObject.AddComponent<ReplayManager>();
            RecordManager recording = menuObject.AddComponent<RecordManager>();
            MenuManager menu = menuObject.AddComponent<MenuManager>();
            MedalLikeRecord medal = menuObject.AddComponent<MedalLikeRecord>();
            basics.text = text;

            menuObject.transform.SetParent(__instance.transform);
            menuObject.transform.localPosition = new UnityEngine.Vector3(menuObject.transform.localPosition.x, -menuObject.transform.localPosition.y, menuObject.transform.localPosition.z);
            RectTransform rt = menuObject.GetComponent<RectTransform>();
            rt.pivot = new UnityEngine.Vector2(0, 1);
            rt.sizeDelta = new UnityEngine.Vector2(1000, 1000);
        }
    }
}
