﻿//Using
using BepInEx;
using BepInEx.IL2CPP;
using UnityEngine;
using UnityEngine.UI;
using UnhollowerRuntimeLib;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using TMPro;

namespace ReplayMod
{
    [BepInPlugin("GUID_du_plugin", "ReplayMod", "0.1.3")]
    public class Plugin : BasePlugin
    {
        private static Il2CppSystem.Collections.Generic.Dictionary<ulong, PlayerManager> activePlayers = null;
        public static Camera camera;
        private static DateTime startGame;

        //Manager
        public static GameManager gameManager;
        public static GameModeManager gamemodeManager = null;

        //Instance 
        public static ChatBox chatBox = null;
        public static PlayerInventory agentInventory = null;
        public static PlayerMovement agentMovement = null;
        public static PlayerManager targetPlayerManager = null;

        //RigidBody
        public static Rigidbody agentBody;
        public static Rigidbody targetBody;

        //GameObject
        public static GameObject agentObject;

        //double[][]

        //string
        private static string filename;
        private static string logData;
        private static readonly string testPath = "ReplayMod\\";
        public static string player1name;
        public static string player2name; 

        //ulong
        public static ulong agentClientId;

        //double

        //float
        public static float targetSpeed;
        public static float smoothedSpeed = 0;
        public static float smoothingFactor = 0.7f;
        public static float replaySpeed = 1;
        public static float posSmoothness = 0.1f;
        public static float rotSmoothness = 0.1f;
        public static float distFromPlayer = 0.8f;

        //int
        public static int mapId;
        public static int targetId;
        public static int replayFile = 0;
        public static int replayMap = 0;
        public static int recordFPS = 30;
        public static int replayFPS = 30;
        public static int maxReplayFile = 10;
        public static int cinematicCloseHeight = 1;
        public static int cinematicFarHeight = 12;


        //boolean
        public static bool cartography = false;
        public static bool hasStick = false;
        public static bool autoTrigger = false;
        public static bool fakeTrigger = false;
        public static bool replayTrigger = false;
        public static bool povTrigger = false;
        public static bool cinematicTrigger = false;
        private static bool gameEnded = true;
        public static bool forceRecord = false;
        public static bool replayStop = false;

        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<Basics>();
            ClassInjector.RegisterTypeInIl2Cpp<ReplayController>();
            ClassInjector.RegisterTypeInIl2Cpp<RecordController>();
            CreateConfigFile();

            // Plugin startup logic
            Harmony.CreateAndPatchAll(typeof(Plugin));
        }

        public static byte ConvertKeyToByte(string key)
        {
            Dictionary<string, byte> keyCodes = new Dictionary<string, byte>
            {
                {"A", 0x41},
                {"B", 0x42},
                {"C", 0x43},
                {"D", 0x44},
                {"E", 0x45},
                {"F", 0x46},
                {"G", 0x47},
                {"H", 0x48},
                {"I", 0x49},
                {"J", 0x4A},
                {"K", 0x4B},
                {"L", 0x4C},
                {"M", 0x4D},
                {"N", 0x4E},
                {"O", 0x4F},
                {"P", 0x50},
                {"Q", 0x51},
                {"R", 0x52},
                {"S", 0x53},
                {"T", 0x54},
                {"U", 0x55},
                {"V", 0x56},
                {"W", 0x57},
                {"X", 0x58},
                {"Y", 0x59},
                {"Z", 0x5A},
                { "0", 0x30 },
                { "1", 0x31 },
                { "2", 0x32 },
                { "3", 0x33 },
                { "4", 0x34 },
                { "5", 0x35 },
                { "6", 0x36 },
                { "7", 0x37 },
                { "8", 0x38 },
                { "9", 0x39 },
                { "F1", 0x70 },
                { "F2", 0x71 },
                { "F3", 0x72 },
                { "F4", 0x73 },
                { "F5", 0x74 },
                { "F6", 0x75 },
                { "F7", 0x76 },
                { "F8", 0x77 },
                { "F9", 0x78 },
                { "F10", 0x79 },
                { "F11", 0x7A },
                { "F12", 0x7B },
                { "Tab", 0x09 },
                { "CapsLock", 0x14 },
                { "Shift", 0x10 },
                { "Ctrl", 0x11 },
                { "Alt", 0x12 },
                { "Esc", 0x1B },
                { "Backspace", 0x08 },
                { "Enter", 0x0D },
                { "Space", 0x20 },
                { "LeftArrow", 0x25 },
                { "UpArrow", 0x26 },
                { "RightArrow", 0x27 },
                { "DownArrow", 0x28 },
                { "Insert", 0x2D },
                { "Delete", 0x2E },
                { "Home", 0x24 },
                { "End", 0x23 },
                { "PageUp", 0x21 },
                { "PageDown", 0x22 },
                { "NumLock", 0x90 },
                { "ScrollLock", 0x91 },
                { "PrintScreen", 0x2C },
                { "Pause", 0x13 },
            };

            if (keyCodes.ContainsKey(key))
            {
                return keyCodes[key];
            }
            else
            {
                throw new ArgumentException("Invalid key.");
            }
        }
        public static long GetUnixTime()
        {
            return ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
        }
        public static bool DoesFunctionCrash(Action function)
        {
            try
            {
                function.Invoke(); // Appel de la fonction à vérifier
                return false; // La fonction s'est exécutée sans erreur
            }
            catch (Exception ex)
            {
                // La fonction a généré une exception
                Debug.LogError($"Erreur : {ex.Message}");
                return true;
            }
        }
        public static void PressKey(byte key)
        {
            [DllImport("user32.dll")]
            static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

            const int KEYEVENTF_EXTENDEDKEY = 0x0001;
            const int KEYEVENTF_KEYUP = 0x0002;
            byte VK_KEY = key;

            keybd_event(VK_KEY, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero); // press the key
            keybd_event(VK_KEY, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // release the key
        }
        public static string GetTimestamp()
        {
            long actualTime = GetUnixTime();
            long endingTime = 1680127199000;

            string timeLeft = ((endingTime - actualTime) / 60000).ToString();

            return timeLeft;
        }

        public static ulong GetClientId()
        {
            return GetPlayerManager().steamProfile.m_SteamID;
        }
        private static LobbyManager GetLobbyManager()
        {
            return LobbyManager.Instance;
        }
        public static string GetGameModeNameAsString()
        {
            return UnityEngine.Object.FindObjectOfType<LobbyManager>().gameMode.modeName.ToString();
        }
        public static string GetGameModeIdAsString()
        {
            return GetLobbyManager().gameMode.id.ToString();
        }
        private static int GetCurrentGameTimer()
        {
            return (UnityEngine.Object.FindObjectOfType<TimerUI>().field_Private_TimeSpan_0.Seconds + 1 + UnityEngine.Object.FindObjectOfType<TimerUI>().field_Private_TimeSpan_0.Minutes * 60 + UnityEngine.Object.FindObjectOfType<TimerUI>().field_Private_TimeSpan_0.Hours * 3600);
        }
        private static int GetMapId()
        {
            return GetLobbyManager().map.id;
        }
        private static int GetGameModId()
        {
            return GetLobbyManager().gameMode.id;
        }
        private static int GetPlayersAlive()
        {
            if (GameManager.Instance != null)
            {
                return GameManager.Instance.GetPlayersAlive();
            }
            else
            {
                return 0;
            }
        }

        private static GameObject GetPlayerObject()
        {
            return GameObject.Find("/Player");
        }
        private static PlayerManager GetPlayerManager()
        {
            return GetPlayerObject().GetComponent<PlayerManager>();
        }
        public static Rigidbody GetPlayerBody()
        {
            return GameObject.Find("/Player") == null ? null : GameObject.Find("/Player").GetComponent<Rigidbody>();
        }
        public static Rigidbody GetPlayerBodySafe()
        {
            if (agentBody == null)
            {
                agentBody = GetPlayerBody();
            }
            return agentBody;
        }
        private static string GetPlayerUsernameAsString()
        {
            return GetPlayerManager() == null ? "Player's name is unknown" : GetPlayerManager().username.ToString();
        }
        private static Camera GetCamera()
        {
            return UnityEngine.Object.FindObjectOfType<Camera>();
        }
        private static Vector3? GetPlayerRotation()
        {
            return GetCamera()?.transform.rotation.eulerAngles;
        }
        private static string GetPlayerRotationAsString()
        {
            Vector3? rotation = GetPlayerRotation();

            return !rotation.HasValue ? "ERROR" : rotation.Value.ToString("F2");
        }
        public static string GetPlayerPositionAsString()
        {
            return GetPlayerBodySafe() == null ? "0.0" : GetPlayerBodySafe().transform.position.ToString();
        }
        public static string GetPlayerSpeedAsString()
        {
            Vector3 velocity = Vector3.zero;
            if (GetPlayerBodySafe() != null)
                velocity = new UnityEngine.Vector3(GetPlayerBodySafe().velocity.x, 0f, GetPlayerBodySafe().velocity.z);
            return velocity.magnitude.ToString("0.00");
        }
        public static string GetGameStateAsString()
        {
            return UnityEngine.Object.FindObjectOfType<GameManager>()?.gameMode.modeState.ToString();
        }
        public static int FindEnemies()
        {
            gameManager = GameObject.Find("/GameManager (1)").GetComponent<GameManager>();
            int u = -1; // Initialiser u à -1 pour commencer

            while (u == -1 || gameManager.activePlayers.entries.ToList()[u].value.username == GetPlayerUsernameAsString() || gameManager.activePlayers.entries.ToList()[u].value.dead)
            {
                u = new System.Random().Next(0, gameManager.activePlayers.count); // Choisir un nombre aléatoire entre 0 et le nombre de joueurs actifs

                if (gameManager.activePlayers.entries.ToList()[u].value.username != GetPlayerUsernameAsString() && !gameManager.activePlayers.entries.ToList()[u].value.dead)
                {
                    targetId = u;
                    return u;
                }
            }
            return u;
        }

        public static Rigidbody GetOtherPlayerBody(int selector)
        {
            Rigidbody rb = null;

            bool result = DoesFunctionCrash(() => {
                gameManager.activePlayers.entries.ToList()[selector].value.GetComponent<Rigidbody>();
            });

            if (result)
            {
                rb = null;
            }
            else
            {
                rb = gameManager.activePlayers.entries.ToList()[selector].value.GetComponent<Rigidbody>();
            }

            return rb;
        }
        public static string GetOtherPlayerUsername(int selector)
        {
            return GetOtherPlayerBody(selector) == null ? "<color=red>N/A</color>" : gameManager.activePlayers.entries.ToList()[selector].value.username.ToString();
        }
        public static UnityEngine.Vector3 GetOtherPlayerPosition(int selector)
        {
            return GetOtherPlayerBody(selector) == null ? Vector3.zero : new Vector3(GetOtherPlayerBody(selector).position.x, GetOtherPlayerBody(selector).position.y, GetOtherPlayerBody(selector).position.z);
        }
        public static string GetOtherPlayerPositionAsString(int selector)
        {
            return GetOtherPlayerBody(selector) == null ? Vector3.zero.ToString() : new Vector3(GetOtherPlayerBody(selector).position.x, GetOtherPlayerBody(selector).position.y, GetOtherPlayerBody(selector).position.z).ToString();
        }
        public static void HasStickCheck()
        {
            bool result = DoesFunctionCrash(() => {
                if (PlayerInventory.Instance.currentItem.name == "Stick(Clone)") ;
            });

            if (result)
            {
                if (hasStick)
                {
                    hasStick = false;
                }
            }
            else
            {
                if (!hasStick)
                {
                    hasStick = true;
                }
            }
        }
        private static PlayerInventory GetPlayerInventory()
        {
            return PlayerInventory.Instance;
        }
        private static bool GetPlayerIsTagged()
        {
            return GetPlayerInventory().currentItem != null;
        }
        private static string GetIsTaggedAsString()
        {
            return GetPlayerIsTagged() ? "1" : "0";
        }
       
        private static void WriteOnFile(string path, string line)
        {
            using System.IO.StreamWriter file = new(path, append: true);
            file.WriteLine(line);
        }

        public void CreateConfigFile()
        {
            // Create the "ReplayMod" and "config" folders if they do not already exist
            string path = Path.Combine("ReplayMod", "config");
            Directory.CreateDirectory(path);

            // Creation of the configuration file
            string configFilePath = Path.Combine(path, "config.txt");

            // Check that the file exists. If the file does not exist, it is created.
            if (!File.Exists(configFilePath))
            {
                using (StreamWriter sw = File.CreateText(configFilePath))
                {
                    sw.WriteLine("recordFPS=60");
                    sw.WriteLine("maxReplayFiles=10");
                    sw.WriteLine("posSmoothness=0,1");
                    sw.WriteLine("rotSmoothness=0,1");
                    sw.WriteLine("distFromPlayer=0,8");
                    sw.WriteLine("cinematicCloseHeight=1");
                    sw.WriteLine("cinematicFarHeight=12");
                }
            }
        }
        private static void LogAllData(string filename, DateTime start)
        {
            // Interrupted represents all match interruption problems without any deaths
            // write the status of the match as Interrupted by default and change the status if all goes well
            string path = testPath + filename + ";Recording.txt";

            DateTime end = DateTime.Now;

            TimeSpan ts = (end - start);
            string[] stringArray;
            int timestamp = (int)ts.TotalMilliseconds;
            if (!forceRecord)
            {
                stringArray = new string[]
                {
                timestamp.ToString(),
                GetPlayerPositionAsString(),
                GetIsTaggedAsString(),
                GetOtherPlayerPositionAsString(targetId),
                GetPlayerRotationAsString(),
                };
            }
            else
            {
                stringArray = new string[]
                {
                timestamp.ToString(),
                GetPlayerPositionAsString(),
                "1",
                GetPlayerPositionAsString(),
                GetPlayerRotationAsString(),
                };
            }


            WriteOnFile(path, StringsArrayToCsvLine(stringArray));
            string logDataOld = logData;
            logData = logDataOld + "\n" + StringsArrayToCsvLine(stringArray);
        }
        private static string DefaultFormatCsv(string originalString)
        {
            return originalString.Replace("(", "").Replace(")", "").Replace(" ", "").Replace(",", ";").Replace(".", ",");
        }
        private static string StringsArrayToCsvLine(string[] array)
        {
            string result = "";

            if (array.Length > 0)
            {
                StringBuilder sb = new();

                string formattedString;

                foreach (string s in array)
                {
                    formattedString = DefaultFormatCsv(s);
                    sb.Append(formattedString).Append(";");
                }

                result = sb.Remove(sb.Length - 1, 1).ToString();
            }

            return result;
        }
        public static float RandomFloat()
        {

            System.Random random = new System.Random();

            // Générer un nombre aléatoire entre 0 et 1
            float randomFloat = (float)(random.NextDouble() * 10 - 5);

            return randomFloat;

        }
        public static int RandomInt()
        {
            System.Random random = new System.Random();

            // Générer un entier aléatoire entre 0 et 30
            int randomInt = random.Next(0, 10);

            return randomInt;
        }
        public static void SendRandomMessage()
        {
            List<string> elements = new List<string> {
                "Im a bot!",
                "Welcome!",
                };
            System.Random rand = new System.Random();
            int index = rand.Next(elements.Count);
            ChatBox.Instance.SendMessage(elements[index]);
        }
        public static void PlayMenuSound()
        {
            agentInventory.woshSfx.pitch = 200;
            agentInventory.woshSfx.Play();
        }
        public static void debugChat()
        {
            chatBox.overlay.color = Color.black;
            chatBox.overlay.gameObject.transform.localScale = new Vector3(1.3f, 3f, 1);
            chatBox.inputField.customCaretColor = true;
            chatBox.inputField.caretColor = Color.green;
            chatBox.inputField.caretWidth = 3;
            chatBox.inputField.name = "GibsonBot Console";
            chatBox.inputField.selectionColor = Color.white;
            chatBox.inputField.gameObject.transform.localScale = new Vector3(0.5f, 0.3f, 0.3f);
            chatBox.messages.fontSize = 5;
            chatBox.messages.gameObject.transform.localScale = new Vector3(2.5f, 1f, 1f);
            chatBox.messages.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center;
            chatBox.messages.fontStyle = TMPro.FontStyles.Superscript;
        }

        public class ReplayController : MonoBehaviour
        {
            private StreamReader csvReader;
            private GameObject player1, player2;
            private bool isReaderInitialized = false;
            private bool tagSwitch = false;
            private bool replayEnded = false;
            private bool player1Visibility = true;
            private bool player2Visibility = true;
            private string playername1 = "";
            private string playername2 = "";

            public float positionSmoothTime = posSmoothness; // Duration for smoothing camera position movement
            public float rotationSmoothTime = rotSmoothness; // Duration for smoothing camera rotation movement
            private Vector3 velocity = Vector3.zero; // Current velocity, modified by SmoothDamp method
            string checking = "no";
            Vector3 newCameraPosition = Vector3.zero;
            Vector3 lookAtPoint = Vector3.zero;

            private float elapsed = 0f; // Elapsed time variable

            public void Update()
            {
                if (replayTrigger)
                {
                    if (cinematicTrigger || povTrigger)
                    {
                        agentBody.useGravity = false;
                        agentBody.isKinematic = true;
                    }
                }

                elapsed += Time.deltaTime;

                if (elapsed >= 1 / (replayFPS * replaySpeed))
                {
                    if (replayTrigger)
                    {
                        if (GetGameModeIdAsString() != "13")
                        {
                            chatBox.ForceMessage("Wrong mode, are you in Practice?");
                            replayTrigger = false;
                            chatBox.ForceMessage("■<color=orange>Mode Replay OFF</color>■");
                            return;
                        }

                        if (!isReaderInitialized)
                        {
                            DirectoryInfo directory = new DirectoryInfo("ReplayMod\\");
                            FileInfo[] files = directory.GetFiles("*.txt");

                            if (files.Length == 0)
                            {
                                chatBox.ForceMessage("No .txt files found in the directory.");
                                return;
                            }

                            // Sort files by descending last write time
                            Array.Sort(files, (x, y) => y.LastWriteTime.CompareTo(x.LastWriteTime));

                            int fileIndexToRead = replayFile; // Modify this value based on the index of the file you want to read
                            if (fileIndexToRead >= files.Length)
                            {
                                chatBox.ForceMessage("Not enough records to fulfill your request.");
                                return;
                            }

                            csvReader = new StreamReader(files[fileIndexToRead].FullName);

                            string fullFilePath = csvReader.BaseStream is FileStream fileStream ? fileStream.Name : null;
                            if (fullFilePath != null)
                            {
                                string fileName = Path.GetFileNameWithoutExtension(fullFilePath);
                                string[] parts = fileName.Split(';');
                                if (parts.Length < 4)
                                {
                                    chatBox.ForceMessage("The file name does not contain enough parts to extract the requested element.");
                                    return;
                                }

                                string value = parts[3]; // Get the 4th element
                                replayMap = int.Parse(value);
                                replayFPS = int.Parse(parts[4]);

                                playername1 = parts[0];
                                playername2 = parts[1];
                                checking = parts[0];
                            }
                            else
                            {
                                chatBox.ForceMessage("The file path could not be retrieved.");
                            }

                            if (GetMapId() == replayMap)
                                isReaderInitialized = true;
                            else
                            {
                                ServerSend.LoadMap(replayMap, 13, GetClientId());
                                isReaderInitialized = true;
                            }

                            if (checking != "force")
                            {
                                player1 = CreatePlayer(Color.black, Color.gray, playername1);
                                player2 = CreatePlayer(Color.white, Color.gray, playername2);
                            }
                            else
                            {
                                player1 = CreatePlayer(Color.black, Color.gray, GetPlayerUsernameAsString());
                                player2 = CreatePlayer(Color.white, Color.gray, "");
                            }
                        }

                        if (!povTrigger && !cinematicTrigger)
                        {
                            if (!player1Visibility)
                            {
                                TogglePlayerVisibility(player1);
                                player1Visibility = true;
                            }
                            if (!player2Visibility && checking != "force")
                            {
                                TogglePlayerVisibility(player1);
                                player2Visibility = true;
                            }
                        }

                        string line = csvReader.ReadLine();

                        if (checking == "force")
                        {
                            if (player2Visibility)
                            {
                                TogglePlayerVisibility(player2);
                                player2Visibility = false;
                            }
                        }

                        if (line != null && !replayStop)
                        {
                            string[] data = line.Split(';');

                            float tagged = float.Parse(data[4]);

                            float x1 = float.Parse(data[1]);
                            float y1 = float.Parse(data[2]);
                            float z1 = float.Parse(data[3]);

                            player1.transform.position = new Vector3(x1, y1, z1);

                            float x2 = float.Parse(data[5]);
                            float y2 = float.Parse(data[6]);
                            float z2 = float.Parse(data[7]);

                            player2.transform.position = new Vector3(x2, y2, z2);

                            float rx1 = float.Parse(data[8]);
                            float ry1 = float.Parse(data[9]);
                            float rz1 = float.Parse(data[10]);

                            player1.transform.rotation = Quaternion.Euler(new Vector3(rx1, ry1, rz1));

                            Vector3 position1 = new Vector3(x1, y1, z1);
                            Vector3 position2 = new Vector3(x2, y2, z2);
                            Vector3 rotation1 = new Vector3(rx1, ry1, rz1);

                            if (tagged == 1)
                            {
                                if (!tagSwitch)
                                {
                                    PlayMenuSound();
                                    tagSwitch = true;
                                }

                                ChangeHeadColor(player1, "Head", Color.red);
                                ChangeHeadColor(player2, "Head", Color.blue);

                                if (cinematicTrigger)
                                {
                                    newCameraPosition = position1 * distFromPlayer + position2 * (1 - distFromPlayer) + new Vector3(0, cinematicCloseHeight, 0);
                                    lookAtPoint = position1;
                                }
                            }
                            else
                            {
                                if (tagSwitch)
                                {
                                    PlayMenuSound();
                                    tagSwitch = false;
                                }

                                ChangeHeadColor(player1, "Head", Color.blue);
                                ChangeHeadColor(player2, "Head", Color.red);

                                if (cinematicTrigger)
                                {
                                    newCameraPosition = position1 * (1 - distFromPlayer + 0.1f) + position2 * (distFromPlayer - 0.1f) + new Vector3(0, cinematicFarHeight, 0);
                                    lookAtPoint = position2;
                                }
                            }

                            if (povTrigger)
                            {
                                if (player1Visibility)
                                {
                                    TogglePlayerVisibility(player1);
                                    player1Visibility = false;
                                }
                                agentBody.transform.position = position1;
                                Vector3 eulerRotation = rotation1;
                                Quaternion rotation = Quaternion.Euler(eulerRotation);
                                Vector3 targetPosition = agentMovement.playerCam.position + (rotation * Vector3.forward);
                                agentMovement.playerCam.LookAt(targetPosition);
                            }

                            if (cinematicTrigger)
                            {
                                if (!player1Visibility)
                                {
                                    TogglePlayerVisibility(player1);
                                    player1Visibility = true;
                                }
                                if (!player2Visibility && checking != "force")
                                {
                                    TogglePlayerVisibility(player2);
                                    player2Visibility = true;
                                }

                                RaycastHit hit;
                                if (Physics.Linecast(agentBody.transform.position, newCameraPosition, out hit))
                                {
                                    newCameraPosition = hit.point - (lookAtPoint - newCameraPosition).normalized * 1.0f;
                                }

                                agentBody.transform.position = Vector3.SmoothDamp(agentBody.transform.position, newCameraPosition, ref velocity, positionSmoothTime);

                                Quaternion targetRotation = Quaternion.LookRotation(lookAtPoint - newCameraPosition);
                                Quaternion interpolatedRotation = Quaternion.Slerp(agentMovement.playerCam.rotation, targetRotation, rotationSmoothTime);
                                Vector3 newLookAtPoint = newCameraPosition + interpolatedRotation * Vector3.forward;

                                agentMovement.playerCam.LookAt(newLookAtPoint);
                            }
                        }
                        else
                        {
                            player1Visibility = true;
                            player2Visibility = true;
                            Destroy(player1);
                            Destroy(player2);

                            csvReader.Close();
                            replayStop = false;
                            replayEnded = true;
                            isReaderInitialized = false;
                            replayTrigger = false;
                            replaySpeed = 1;

                            if (cinematicTrigger || povTrigger)
                                agentBody.transform.position = new Vector3(0, 0, 0);
                            agentBody.useGravity = true;
                            agentBody.isKinematic = false;
                            povTrigger = false;
                            cinematicTrigger = false;

                            chatBox.ForceMessage("■<color=orange>Mode Replay OFF</color>■");
                        }

                        elapsed = 0f;
                    }
                }
            }
            private void ChangeHeadColor(GameObject player, string headName, Color newColor)
            {
                Transform headTransform = player.transform.Find(headName);

                if (headTransform != null)
                {
                    Renderer headRenderer = headTransform.GetComponent<Renderer>();

                    if (headRenderer != null)
                    {
                        Material headMaterial = headRenderer.material;
                        Material newHeadMaterial = new Material(headMaterial);
                        newHeadMaterial.color = newColor;

                        headRenderer.material = newHeadMaterial;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            private void TogglePlayerVisibility(GameObject player)
            {
                // Find the body Renderer
                Renderer bodyRenderer = player.transform.Find("Body").GetComponent<Renderer>();
                if (bodyRenderer != null)
                {
                    bodyRenderer.enabled = !bodyRenderer.enabled;
                }

                // Find the head Renderer
                Renderer headRenderer = player.transform.Find("Head").GetComponent<Renderer>();
                if (headRenderer != null)
                {
                    headRenderer.enabled = !headRenderer.enabled;
                }

                // Find the label Renderer
                GameObject label = player.transform.Find("Label").gameObject;
                if (label != null)
                {
                    label.GetComponent<TextMeshPro>().enabled = !label.GetComponent<TextMeshPro>().enabled;
                }
            }

            private GameObject CreatePlayer(Color bodyColor, Color headColor, string playerName)
            {
                // Create the player
                GameObject player = new GameObject("Player");
                Rigidbody rb = player.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;

                // Create and configure the player's body
                GameObject body = CreatePlayerComponent(player, PrimitiveType.Capsule, "Body", bodyColor, new Vector3(1.2f, 1.5f, 1.2f));

                // Create and configure the player's head
                GameObject head = CreatePlayerComponent(player, PrimitiveType.Sphere, "Head", headColor, new Vector3(1.2f, 1.2f, 1.2f), new Vector3(0f, 2.2f, 0f));

                // Create and configure the player's label
                GameObject label = CreatePlayerLabel(player, playerName, new Vector3(0, 3.2f, 0));

                return player;
            }

            private GameObject CreatePlayerComponent(GameObject parent, PrimitiveType type, string name, Color color, Vector3 localScale, Vector3? localPosition = null)
            {
                GameObject component = GameObject.CreatePrimitive(type);

                component.name = name;
                component.transform.parent = parent.transform;
                component.transform.localScale = localScale;

                if (localPosition.HasValue)
                {
                    component.transform.localPosition = localPosition.Value;
                }
                else
                {
                    component.transform.localPosition = Vector3.zero;
                }

                Material material = new Material(Shader.Find("Standard"));
                material.color = color;
                Renderer renderer = component.GetComponent<Renderer>();
                renderer.material = material;

                return component;
            }

            private GameObject CreatePlayerLabel(GameObject parent, string labelText, Vector3 localPosition)
            {
                GameObject label = new GameObject("Label");
                label.transform.SetParent(parent.transform);
                label.transform.localPosition = localPosition;

                TextMeshPro labelTMP = label.AddComponent<TextMeshPro>();
                labelTMP.text = labelText;
                labelTMP.fontSize = 5;
                labelTMP.alignment = TextAlignmentOptions.Center;

                return label;
            }
        }

        public class RecordController : MonoBehaviour
        {
            private DateTime startTimer = DateTime.Now;
            private DateTime lastRecordTime;
            private bool forceActive = false;
            private bool inTwoPlayersGame = false;

            private void Update()
            {
                if (forceRecord)
                {
                    HandleForceRecord();
                }
                else if (forceActive)
                {
                    RenameFile();
                    forceActive = false;
                }

                if (agentObject != null && GetGameStateAsString() == "Playing")
                {
                    HandleGamePlay();
                }
                else
                {
                    EndGame();
                }
            }

            private void HandleForceRecord()
            {
                if (!forceActive)
                {
                    InitRecord("force", "Record");
                    forceActive = true;
                }

                if ((DateTime.Now - lastRecordTime).TotalSeconds >= 1.0 / recordFPS)
                {
                    LogAllData(filename, startGame);
                    lastRecordTime = DateTime.Now;
                }
            }

            private void HandleGamePlay()
            {
                if (!inTwoPlayersGame && !forceRecord)
                {
                    gameManager = GameObject.Find("/GameManager (1)").GetComponent<GameManager>();
                    activePlayers = GameManager.Instance.activePlayers;
                    targetId = FindEnemies();
                    player1name = GetPlayerUsernameAsString();
                    player2name = GetOtherPlayerUsername(targetId);
                    InitRecord(player1name, player2name);
                    inTwoPlayersGame = true;
                }

                if ((DateTime.Now - lastRecordTime).TotalSeconds >= 1.0 / recordFPS)
                {
                    LogAllData(filename, startGame);
                    lastRecordTime = DateTime.Now;
                }
            }

            private void InitRecord(string first, string second)
            {
                startGame = DateTime.Now;
                long startGameTimeMilliseconds = new DateTimeOffset(startGame).ToUnixTimeMilliseconds();
                string[] filenameArray =
                {
            first,
            second,
            startGameTimeMilliseconds.ToString(),
            GetMapId().ToString(),
            recordFPS.ToString(),
        };
                filename = StringsArrayToCsvLine(filenameArray);
                logData = "";

                CreateDirectoryIfNotExists("ReplayMod");
                CleanDirectory("ReplayMod\\", maxReplayFile);

                lastRecordTime = DateTime.Now;
            }

            private void CreateDirectoryIfNotExists(string directory)
            {
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
            }

            private void CleanDirectory(string directoryPath, int numberOfFilesToKeep)
            {
                var files = new DirectoryInfo(directoryPath).GetFiles();
                var orderedFiles = files.OrderByDescending(f => f.CreationTime).ToList();

                while (orderedFiles.Count > numberOfFilesToKeep)
                {
                    orderedFiles.Last().Delete();
                    orderedFiles.RemoveAt(orderedFiles.Count - 1);
                }
            }

            private void RenameFile()
            {
                string sourceFile = "ReplayMod\\" + filename + ";Recording.txt";
                System.IO.FileInfo fi = new System.IO.FileInfo(sourceFile);
                if (fi.Exists)
                {
                    fi.MoveTo("ReplayMod\\" + filename + ".txt");
                }
            }

            private void EndGame()
            {
                if (!gameEnded)
                {
                    RenameFile();
                }

                inTwoPlayersGame = false;
                gameEnded = true;
            }
        }

        public class Basics : MonoBehaviour
        {
            private bool isStarted = false;
            private string configFilePath = "ReplayMod\\config\\config.txt";

            void Update()
            {
                if (!isStarted)
                {
                    LoadConfigurations();
                    isStarted = true;
                }
            }

            private void LoadConfigurations()
            {
                string[] lines = System.IO.File.ReadAllLines(configFilePath);
                Dictionary<string, string> config = new Dictionary<string, string>();

                foreach (string line in lines)
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();
                        config[key] = value;
                    }
                }

                maxReplayFile = int.Parse(config["maxReplayFiles"]) - 1;
                recordFPS = int.Parse(config["recordFPS"]);
                posSmoothness = float.Parse(config["posSmoothness"]);
                rotSmoothness = float.Parse(config["rotSmoothness"]);
                distFromPlayer = float.Parse(config["distFromPlayer"]);
                cinematicCloseHeight = int.Parse(config["cinematicCloseHeight"]);
                cinematicFarHeight = int.Parse(config["cinematicFarHeight"]);

                agentClientId = GetClientId();
                mapId = GetMapId();
                agentBody = GetPlayerBody();
                agentObject = GetPlayerObject();
                gameManager = GameObject.Find("/GameManager (1)").GetComponent<GameManager>();
                agentMovement = agentObject.GetComponent<PlayerMovement>();
                camera = UnityEngine.Object.FindObjectOfType<Camera>();
                chatBox = ChatBox.Instance;
                agentInventory = PlayerInventory.Instance;
                //debugChat();
            }
        }

        [HarmonyPatch(typeof(ChatBox), nameof(ChatBox.AppendMessage))]
        [HarmonyPostfix]
        static void OnReceiveMessage(ChatBox __instance, ulong __0, string __1, string __2)
        {
            // If the received message is from the player
            if (__2 == GetPlayerUsernameAsString())
            {
                string inputCommand = __1.ToLower();

                if (inputCommand == "forcerecord" || inputCommand == "fr")
                {
                    // Invert the state of forceRecord
                    forceRecord = !forceRecord;

                    // User message
                    string message = forceRecord ? "■<color=red>ForceRecord mode ON</color>■" : "■<color=black>ForceRecord mode OFF</color>■";
                    chatBox.ForceMessage(message);
                }

                string[] commandParts = inputCommand.Split(' ');

                // Ensure a command has been entered
                if (commandParts.Length > 0)
                {
                    string mainCommand = commandParts[0];

                    // 'replay' or 'r' command with no additional argument
                    if ((mainCommand == "replay" || mainCommand == "r") && commandParts.Length == 1)
                    {
                        PlayMenuSound();
                        replayTrigger = true;
                        chatBox.ForceMessage("■<color=yellow>Replay mode ON</color>■");
                    }

                    // 'replay' or 'r' command with additional arguments
                    if ((mainCommand == "replay" || mainCommand == "r") && commandParts.Length > 1)
                    {
                        string argument = commandParts[1];

                        // Action based on the argument
                        switch (argument.ToLower())
                        {
                            case "pov":
                                chatBox.ForceMessage("■<color=yellow>POV mode ON</color>■");
                                replayTrigger = true;
                                povTrigger = true;
                                cinematicTrigger = false;
                                break;
                            case "cinematic":
                            case "cine":
                            case "c":
                                chatBox.ForceMessage("■<color=yellow>Cinematic mode ON</color>■");
                                replayTrigger = true;
                                cinematicTrigger = true;
                                povTrigger = false;
                                break;
                            case "pause":
                            case "p":
                                chatBox.ForceMessage("■<color=black>Replay Paused</color>■");
                                povTrigger = false;
                                cinematicTrigger = false;
                                replayTrigger = false;
                                break;
                            case "stop":
                            case "s":
                                chatBox.ForceMessage("■<color=black>Replay Stopped</color>■");
                                replayStop = true;
                                break;
                            case "file":
                                if (commandParts.Length > 2)
                                {
                                    replayFile = int.Parse(commandParts[2]);
                                    chatBox.ForceMessage("■<color=yellow>New replay file selected</color>■");
                                }
                                break;
                            default:
                                if (commandParts.Length > 2)
                                {
                                    replaySpeed = float.Parse(commandParts[2].Replace(".", ","));
                                    chatBox.ForceMessage("■<color=yellow>New replay speed set</color>■");
                                }
                                break;
                        }
                    }
                }
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