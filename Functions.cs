using UnityEngine;

namespace ReplayMod
{
    public class Utility
    {
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
        public static string GetTimestamp(long endTime)
        {
            long actualTime = GetUnixTime();
            long endingTime = endTime;

            string timeLeft = ((endingTime - actualTime) / 60000).ToString();

            return timeLeft;
        }
        public static async Task DownloadAndExtractZipAsync(string url, string downloadPath, string extractPath)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                using (FileStream fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    // Copy the content from the response message to the file stream
                    await response.Content.CopyToAsync(fileStream);
                }
            }

            // Ensure the extract path exists
            Directory.CreateDirectory(extractPath);

            // Extract the zip file if the extract path is empty
            if (Directory.GetFiles(extractPath).Length == 0 && Directory.GetDirectories(extractPath).Length == 0)
            {
                ZipFile.ExtractToDirectory(downloadPath, extractPath, true);
            }
        }
        public static void DownloadMinimapDatas()
        {
            // Check if the directory exists, if not, create it
            string directoryPath = Path.GetDirectoryName(Variables.downloadPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Check if the file already exists, if not, download it
            if (!File.Exists(Variables.downloadPath))
            {
                try
                {
                    DownloadAndExtractZipAsync(Variables.minimapURL, Variables.downloadPath, Variables.extractPath).Wait();
                }
                catch (Exception ex)
                {
                    // Handle exceptions here
                    Console.WriteLine("Error downloading file: " + ex.Message);
                }
            }
            else
            {
                // If the file exists, check if the extraction folder is empty, if it is, extract the files
                if (Directory.GetFiles(Variables.extractPath).Length == 0 && Directory.GetDirectories(Variables.extractPath).Length == 0)
                {
                    try
                    {
                        // Ensure the extract path exists
                        Directory.CreateDirectory(Variables.extractPath);

                        // Extract the zip file
                        ZipFile.ExtractToDirectory(Variables.downloadPath, Variables.extractPath, true);
                    }
                    catch (Exception ex)
                    {
                        // Handle exceptions here
                        Console.WriteLine("Error extracting file: " + ex.Message);
                    }
                }
            }
        }
        public static void WriteOnFile(string path, string line)
        {
            using System.IO.StreamWriter file = new(path, append: true);
            file.WriteLine(line);
        }
        public static void CreateConfigFile()
        {
            string path = Path.Combine("ReplayMod", "config");
            Directory.CreateDirectory(path);

            string configFilePath = Path.Combine(path, "config.txt");

            Dictionary<string, string> configDefaults = new Dictionary<string, string>
            {
                {"version", "v0.1.5"},
                {"recordFPS", "120"},
                {"maxReplayFiles", "10"},
                {"posSmoothness", "0,1"},
                {"rotSmoothness", "0,1"},
                {"distFromPlayers", "0,8"},
                {"cinematicCloseHeight", "1"},
                {"cinematicFarHeight", "12"},
                {"minimapSize", "5"},
                {"customPrecisionFormatClientPosition", "F4"},
                {"customPrecisionFormatClientRotation","F3"},
                {"customPrecisionFormatTargetPosition", "F2"},
                {"colorClient", "black"},
                {"colorOtherPlayer", "white"},
                {"defaultFolderPath","C:\\Program Files (x86)\\Steam\\steamapps\\common\\Crab Game\\"}
            };

            Dictionary<string, string> currentConfig = new Dictionary<string, string>();

            // If the file exists, read current config
            if (File.Exists(configFilePath))
            {
                string[] lines = File.ReadAllLines(configFilePath);

                foreach (string line in lines)
                {
                    string[] keyValue = line.Split('=');
                    if (keyValue.Length == 2)
                    {
                        currentConfig[keyValue[0]] = keyValue[1];
                    }
                }
            }

            // Merge current config with defaults
            foreach (KeyValuePair<string, string> pair in configDefaults)
            {
                if (!currentConfig.ContainsKey(pair.Key))
                {
                    currentConfig[pair.Key] = pair.Value;
                }
            }

            // Save merged config
            using (StreamWriter sw = File.CreateText(configFilePath))
            {
                foreach (KeyValuePair<string, string> pair in currentConfig)
                {
                    sw.WriteLine(pair.Key + "=" + pair.Value);
                }
            }
        }
        public static void LogAllData(string filename, DateTime start)
        {
            GameData.HasStickCheck();

            string path = Variables.mainFolderPath + "replays\\" + filename + ";Recording.txt";

            DateTime end = DateTime.Now;
            TimeSpan ts = (end - start);

            string[] stringArray;
            int timestamp = (int)ts.TotalMilliseconds;
            if (!Variables.forceRecord)
            {
                stringArray = new string[]
                {
                timestamp.ToString(),
                ClientData.GetPlayerPositionAsString(),
                ClientData.GetIsTaggedAsString(),
                MultiplayerData.GetOtherPlayerPositionAsString(Variables.clientPlayerId),
                ClientData.GetPlayerRotationAsString(),
                };
            }
            else
            {
                stringArray = new string[]
                {
                timestamp.ToString(),
                ClientData.GetPlayerPositionAsString(),
                "1",
                ClientData.GetPlayerPositionAsString(),
                ClientData.GetPlayerRotationAsString(),
                };
            }


            WriteOnFile(path, StringsArrayToCsvLine(stringArray));
            string logDataOld = Variables.logData;
            Variables.logData = logDataOld + "\n" + StringsArrayToCsvLine(stringArray);
        }
        public static string DefaultFormatCsv(string originalString)
        {
            return originalString.Replace("(", "").Replace(")", "").Replace(" ", "").Replace(",", ";").Replace(".", ",");
        }
        public static string StringsArrayToCsvLine(string[] array)
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
            Variables.clientInventory.woshSfx.pitch = 5 * Variables.menuSpeed;
            Variables.clientInventory.woshSfx.Play();
        }
        public static void debugChat()
        {
            Variables.chatBox.overlay.color = Color.black;
            Variables.chatBox.overlay.gameObject.transform.localScale = new Vector3(1.3f, 3f, 1);
            Variables.chatBox.inputField.customCaretColor = true;
            Variables.chatBox.inputField.caretColor = Color.green;
            Variables.chatBox.inputField.caretWidth = 3;
            Variables.chatBox.inputField.name = "GibsonBot Console";
            Variables.chatBox.inputField.selectionColor = Color.white;
            Variables.chatBox.inputField.gameObject.transform.localScale = new Vector3(0.5f, 0.3f, 0.3f);
            Variables.chatBox.messages.fontSize = 5;
            Variables.chatBox.messages.gameObject.transform.localScale = new Vector3(2.5f, 1f, 1f);
            Variables.chatBox.messages.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center;
            Variables.chatBox.messages.fontStyle = TMPro.FontStyles.Superscript;
        }
        public static Color GetColorFromString(string colorName)
        {
            switch (colorName.ToLower())
            {
                case "red":
                    return Color.red;
                case "blue":
                    return Color.blue;
                case "green":
                    return Color.green;
                case "black":
                    return Color.black;
                case "white":
                    return Color.white;
                case "magenta":
                    return Color.magenta;
                case "cyan":
                    return Color.cyan;
                case "grey":
                case "gray":
                    return Color.grey;


                default:
                    return Color.white;
            }
        }
        public static void ChatCommands(string msg)
        {
            string inputCommand = msg.ToLower();
            string[] commandParts = inputCommand.Split(' ');

            // Ensure a command has been entered
            if (commandParts.Length > 0)
            {
                string mainCommand = commandParts[0];

                if (mainCommand == "forcerecord" || inputCommand == "fr" && commandParts.Length == 1)
                {
                    // Invert the state of forceRecord
                    Variables.forceRecord = !Variables.forceRecord;

                    // User message
                    string message = Variables.forceRecord ? "■<color=yellow>ForceRecord mode ON</color>■" : "■<color=black>ForceRecord mode OFF</color>■";
                    Variables.chatBox.ForceMessage(message);
                }

                // 'replay' or 'r' command with no additional argument
                if ((mainCommand == "replay" || mainCommand == "r") && commandParts.Length == 1)
                {
                    PlayMenuSound();
                    Variables.replayTrigger = true;
                    Variables.chatBox.ForceMessage("■<color=yellow>Replay mode ON</color>■");
                }

                // 'replay' or 'r' command with additional arguments
                if ((mainCommand == "replay" || mainCommand == "r") && commandParts.Length > 1)
                {
                    string argument = commandParts[1];

                    // Action based on the argument
                    switch (argument.ToLower())
                    {
                        case "pov":
                            if (GameData.GetMapId() == Variables.replayMap || !Variables.minimapTrigger)
                            {
                                Variables.chatBox.ForceMessage("■<color=yellow>POV mode ON</color>■");
                                Variables.replayTrigger = true;
                                Variables.povTrigger = true;
                                Variables.cinematicTrigger = false;
                                if (commandParts.Length > 2)
                                {
                                    if (float.TryParse(commandParts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out float newReplaySpeed))
                                    {
                                        Variables.replaySpeed = newReplaySpeed;
                                        Variables.chatBox.ForceMessage("■<color=yellow>New replay speed set</color>■");
                                    }
                                }
                            }
                            else if (Variables.minimapTrigger && Variables.mapId != Variables.replayMap && Variables.gamemodeId == 13)
                            {
                                Variables.chatBox.ForceMessage("■<color=red>Error, you can't use this now, switch into the good map</color>■");
                            }
                            else
                            {
                                Variables.minimapTrigger = false;
                            }
                            break;
                        case "cinematic":
                        case "cine":
                        case "c":
                            if (GameData.GetMapId() == Variables.replayMap || !Variables.minimapTrigger)
                            {
                                Variables.chatBox.ForceMessage("■<color=yellow>Cinematic mode ON</color>■");
                                Variables.replayTrigger = true;
                                Variables.cinematicTrigger = true;
                                Variables.povTrigger = false;
                                if (commandParts.Length > 2)
                                {
                                    if (float.TryParse(commandParts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out float newReplaySpeed))
                                    {
                                        Variables.replaySpeed = newReplaySpeed;
                                        Variables.chatBox.ForceMessage("■<color=yellow>New replay speed set</color>■");
                                    }
                                }
                            }
                            else if (Variables.minimapTrigger && Variables.mapId != Variables.replayMap && Variables.gamemodeId == 13)
                            {
                                Variables.chatBox.ForceMessage("■<color=red>Error, you can't use this now, switch into the good map</color>■");
                            }
                            else
                            {
                                Variables.minimapTrigger = false;
                            }
                            break;
                        case "mini":
                        case "m":
                            if (GameData.GetGameModId() == 0 || GameData.GetGameModId() == 13)
                            {
                                Variables.chatBox.ForceMessage("■<color=yellow>Minimap mode ON</color>■");
                                Variables.chatBox.ForceMessage("■<color=yellow>Press F to fix the map</color>■");
                                Variables.replayTrigger = true;
                                Variables.minimapTrigger = true;
                                Variables.povTrigger = false;
                                Variables.cinematicTrigger = false;
                                if (commandParts.Length > 2)
                                {
                                    if (float.TryParse(commandParts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out float newReplaySpeed))
                                    {
                                        Variables.replaySpeed = newReplaySpeed;
                                        Variables.chatBox.ForceMessage("■<color=yellow>New replay speed set</color>■");
                                    }
                                }

                            }
                            else
                            {
                                Variables.chatBox.ForceMessage("■<color=red>You can't use that during a game, wait the Lobby!</color>■");
                            }
                            break;
                        case "pause":
                        case "p":
                            Variables.chatBox.ForceMessage("■<color=black>Replay Paused</color>■");
                            Variables.povTrigger = false;
                            Variables.cinematicTrigger = false;
                            Variables.replayTrigger = false;
                            break;
                        case "stop":
                        case "s":
                            Variables.chatBox.ForceMessage("■<color=black>Replay Stopped</color>■");
                            Variables.replayStop = true;
                            break;
                        case "file":
                        case "f":
                            if (commandParts.Length > 2)
                            {
                                if (int.TryParse(commandParts[2], out int newReplayFile))
                                {
                                    Variables.replayFile = newReplayFile;
                                    Variables.chatBox.ForceMessage("■<color=yellow>New replay file selected</color>■");
                                }
                            }
                            break;
                        default:
                            if (commandParts.Length > 2)
                            {
                                if (float.TryParse(commandParts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out float newReplaySpeed))
                                {
                                    Variables.replaySpeed = newReplaySpeed;
                                    Variables.chatBox.ForceMessage("■<color=yellow>New replay speed set</color>■");
                                }
                                Variables.replayTrigger = true;
                            }
                            break;
                    }
                }
            }
        }
        public static void ReadConfigFile()
        {
            string[] lines = System.IO.File.ReadAllLines(Variables.configFilePath);
            Dictionary<string, string> config = new Dictionary<string, string>();
            CultureInfo cultureInfo = new CultureInfo("fr-FR");
            float resultFloat;
            int resultInt;
            bool parseSuccess;

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

            parseSuccess = int.TryParse(config["maxReplayFiles"], out resultInt);
            Variables.maxReplayFile = parseSuccess ? resultInt - 1 : 0;

            parseSuccess = int.TryParse(config["recordFPS"], out resultInt);
            Variables.recordFPS = parseSuccess ? resultInt : 0;

            parseSuccess = float.TryParse(config["posSmoothness"], NumberStyles.Any, cultureInfo, out resultFloat);
            Variables.posSmoothness = parseSuccess ? resultFloat : 0;

            parseSuccess = float.TryParse(config["rotSmoothness"], NumberStyles.Any, cultureInfo, out resultFloat);
            Variables.rotSmoothness = parseSuccess ? resultFloat : 0;

            parseSuccess = float.TryParse(config["distFromPlayers"], NumberStyles.Any, cultureInfo, out resultFloat);
            Variables.distFromPlayers = parseSuccess ? resultFloat : 0;

            parseSuccess = int.TryParse(config["cinematicCloseHeight"], out resultInt);
            Variables.cinematicCloseHeight = parseSuccess ? resultInt : 0;

            parseSuccess = int.TryParse(config["cinematicFarHeight"], out resultInt);
            Variables.cinematicFarHeight = parseSuccess ? resultInt : 0;

            parseSuccess = float.TryParse(config["minimapSize"], NumberStyles.Any, cultureInfo, out resultFloat);
            Variables.minimapSize = parseSuccess ? resultFloat / 100 : 0;

            Variables.customPrecisionFormatClientPosition = config["customPrecisionFormatClientPosition"];
            Variables.customPrecisionFormatClientRotation = config["customPrecisionFormatClientRotation"];
            Variables.customPrecisionFormatTargetPosition = config["customPrecisionFormatTargetPosition"];
            Variables.colorClient = config["colorClient"];
            Variables.colorOtherPlayer = config["colorOtherPlayer"];
        }
        public static void LoadConfigurations()
        {
            ReadConfigFile();

            Variables.clientId = ClientData.GetClientId();
            Variables.mapId = GameData.GetMapId();
            Variables.gamemodeId = GameData.GetGameModId();
            Variables.clientBody = ClientData.GetPlayerBody();
            Variables.clientObject = ClientData.GetPlayerObject();
            Variables.gameManager = GameObject.Find("/GameManager (1)").GetComponent<GameManager>();
            Variables.clientMovement = Variables.clientObject.GetComponent<PlayerMovement>();
            Variables.camera = UnityEngine.Object.FindObjectOfType<Camera>();
            Variables.chatBox = ChatBox.Instance;
            Variables.clientInventory = PlayerInventory.Instance;
            //debugChat();
        }
        public static string ConvertUnixTimestamp()
        {
            long unixTimestamp = Variables.replayDate;

            if (unixTimestamp == 0)
            {
                return "N/A";
            }

            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp);
            string formattedDateTime = dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss");
            return formattedDateTime;
        }
        public static string ConvertUnixTimestamp(long unixTimestamp)
        {
            if (unixTimestamp == 0)
            {
                return "N/A";
            }

            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp);
            string formattedDateTime = dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss");
            return formattedDateTime;
        }
    }
    public class ClientData
    {
        public static ulong GetClientId()
        {
            return GetPlayerManager().steamProfile.m_SteamID;
        }
        public static GameObject GetPlayerObject()
        {
            return GameObject.Find("/Player");
        }
        public static PlayerManager GetPlayerManager()
        {
            return GetPlayerObject().GetComponent<PlayerManager>();
        }
        public static Rigidbody GetPlayerBody()
        {
            return GameObject.Find("/Player") == null ? null : GameObject.Find("/Player").GetComponent<Rigidbody>();
        }
        public static Rigidbody GetPlayerBodySafe()
        {
            if (Variables.clientBody == null)
            {
                Variables.clientBody = GetPlayerBody();
            }
            return Variables.clientBody;
        }
        public static string GetPlayerUsernameAsString()
        {
            return GetPlayerManager() == null ? "Player's name is unknown" : GetPlayerManager().username.ToString();
        }
        public static Camera GetCamera()
        {
            return UnityEngine.Object.FindObjectOfType<Camera>();
        }
        public static Vector3? GetPlayerRotation()
        {
            return GetCamera()?.transform.rotation.eulerAngles;
        }
        public static string GetPlayerRotationAsString()
        {
            Vector3? rotation = GetPlayerRotation();

            return !rotation.HasValue ? "ERROR" : rotation.Value.ToString(Variables.customPrecisionFormatClientRotation);
        }
        public static string GetPlayerPositionAsString()
        {
            return GetPlayerBodySafe() == null ? "0.00" : GetPlayerBodySafe().transform.position.ToString(Variables.customPrecisionFormatClientPosition);
        }
        public static string GetPlayerSpeedAsString()
        {
            Vector3 velocity = Vector3.zero;
            if (GetPlayerBodySafe() != null)
                velocity = new UnityEngine.Vector3(GetPlayerBodySafe().velocity.x, 0f, GetPlayerBodySafe().velocity.z);
            return velocity.magnitude.ToString("0.00");
        }
        public static string GetIsTaggedAsString()
        {
            return Variables.hasStick ? "1" : "0";
        }
    }

    public class MultiplayerData
    {
        public static int FindEnemies()
        {
            Variables.gameManager = GameObject.Find("/GameManager (1)").GetComponent<GameManager>();
            int u = -1; // Initialiser u à -1 pour commencer

            while (u == -1 || Variables.gameManager.activePlayers.entries.ToList()[u].value.username == ClientData.GetPlayerUsernameAsString() || Variables.gameManager.activePlayers.entries.ToList()[u].value.dead)
            {
                u = new System.Random().Next(0, Variables.gameManager.activePlayers.count); // Choisir un nombre aléatoire entre 0 et le nombre de joueurs actifs

                if (Variables.gameManager.activePlayers.entries.ToList()[u].value.username != ClientData.GetPlayerUsernameAsString() && !Variables.gameManager.activePlayers.entries.ToList()[u].value.dead)
                {
                    Variables.clientPlayerId = u;
                    return u;
                }
            }
            return u;
        }
        public static Rigidbody GetOtherPlayerBody(int selector)
        {
            Rigidbody rb = null;

            bool result = Utility.DoesFunctionCrash(() =>
            {
                Variables.gameManager.activePlayers.entries.ToList()[selector].value.GetComponent<Rigidbody>();
            });

            if (result)
            {
                rb = null;
            }
            else
            {
                rb = Variables.gameManager.activePlayers.entries.ToList()[selector].value.GetComponent<Rigidbody>();
            }

            return rb;
        }
        public static string GetOtherPlayerUsername(int selector)
        {
            return GetOtherPlayerBody(selector) == null ? "<color=red>N/A</color>" : Variables.gameManager.activePlayers.entries.ToList()[selector].value.username.ToString();
        }
        public static UnityEngine.Vector3 GetOtherPlayerPosition(int selector)
        {
            return GetOtherPlayerBody(selector) == null ? Vector3.zero : new Vector3(GetOtherPlayerBody(selector).position.x, GetOtherPlayerBody(selector).position.y, GetOtherPlayerBody(selector).position.z);
        }
        public static string GetOtherPlayerPositionAsString(int selector)
        {
            return GetOtherPlayerBody(selector) == null ? Vector3.zero.ToString(Variables.customPrecisionFormatTargetPosition) : new Vector3(GetOtherPlayerBody(selector).position.x, GetOtherPlayerBody(selector).position.y, GetOtherPlayerBody(selector).position.z).ToString(Variables.customPrecisionFormatTargetPosition);
        }
    }


    public class GameData
    {
        public static void HasStickCheck()
        {
            bool result = Utility.DoesFunctionCrash(() =>
            {
                if (PlayerInventory.Instance.currentItem.name == "Stick(Clone)") ;
            });

            if (result)
            {
                if (Variables.hasStick)
                {
                    Variables.hasStick = false;
                }
            }
            else
            {
                if (!Variables.hasStick)
                {
                    Variables.hasStick = true;
                }
            }
        }
        public static LobbyManager GetLobbyManager()
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
        public static int GetCurrentGameTimer()
        {
            return (UnityEngine.Object.FindObjectOfType<TimerUI>().field_Private_TimeSpan_0.Seconds + 1 + UnityEngine.Object.FindObjectOfType<TimerUI>().field_Private_TimeSpan_0.Minutes * 60 + UnityEngine.Object.FindObjectOfType<TimerUI>().field_Private_TimeSpan_0.Hours * 3600);
        }
        public static int GetMapId()
        {
            return GetLobbyManager().map.id;
        }
        public static int GetGameModId()
        {
            return GetLobbyManager().gameMode.id;
        }
        public static int GetPlayersAlive()
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
        public static string GetGameStateAsString()
        {
            return UnityEngine.Object.FindObjectOfType<GameManager>()?.gameMode.modeState.ToString();
        }
    }

    public class ReplayControllerFunctions
    {
        public static Vector3 ParseVector3(string[] info, int startIndex)
        {
            return new Vector3(
                float.Parse(info[startIndex].Replace("(", string.Empty).Replace(".", ",")),
                float.Parse(info[startIndex + 1].Replace(".", ",")),
                float.Parse(info[startIndex + 2].Replace(")", string.Empty).Replace(".", ","))
            );
        }
        public static Quaternion ParseQuaternion(string[] info, int startIndex)
        {
            return new Quaternion(
                float.Parse(info[startIndex].Replace("(", string.Empty).Replace(".", ",")),
                float.Parse(info[startIndex + 1].Replace(".", ",")),
                float.Parse(info[startIndex + 2].Replace(".", ",")),
                float.Parse(info[startIndex + 3].Replace(")", string.Empty).Replace(".", ","))
            );
        }
        public static void InitializeReader()
        {
            DirectoryInfo directory = new DirectoryInfo(Variables.mainFolderPath + "replays\\");
            FileInfo[] files = directory.GetFiles("*.txt");

            if (files.Length == 0)
            {
                Variables.chatBox.ForceMessage("■<color=red>No .txt files found in the directory</color>■");
                Variables.replayStop = true;
                return;
            }

            // Sort files by descending last write time
            Array.Sort(files, (x, y) => y.LastWriteTime.CompareTo(x.LastWriteTime));

            int fileIndexToRead = Variables.replayFile; // Modify this value based on the index of the file you want to read
            if (fileIndexToRead >= files.Length)
            {
                Variables.chatBox.ForceMessage("■<color=red>Not enough records to fulfill your request</color>■");
                Variables.replayStop = true;
                return;
            }

            Variables.csvReplayReader = new StreamReader(files[fileIndexToRead].FullName);

            string fullFilePath = Variables.csvReplayReader.BaseStream is FileStream fileStream ? fileStream.Name : null;
            if (fullFilePath != null)
            {
                string fileName = Path.GetFileNameWithoutExtension(fullFilePath);
                string[] parts = fileName.Split(';');
                if (parts.Length < 4)
                {
                    Variables.chatBox.ForceMessage("■<color=red>The file name does not contain enough parts to extract the requested element</color>■");
                    Variables.replayStop = true;
                    return;
                }

                string value = parts[3]; // Get the 4th element
                Variables.replayMap = int.Parse(value);
                Variables.replayFPS = int.Parse(parts[4]);
                Variables.replayDate = long.Parse(parts[2]);

                Variables.clientCloneName = parts[0];
                Variables.otherPlayerCloneName = parts[1];
                Variables.checkingForceRecord = parts[0];
            }
            else
            {
                Variables.chatBox.ForceMessage("■<color=red>The file path could not be retrieved</color>■");
                Variables.replayStop = true;
            }
            Variables.isReplayReaderInitialized = true;

            if (!Variables.minimapTrigger && Variables.replayMap != GameData.GetMapId())
            {
                ServerSend.LoadMap(Variables.replayMap, 13, ClientData.GetClientId());
            }

            Variables.clientCloneName = Variables.checkingForceRecord != "force" ? Variables.clientCloneName : ClientData.GetPlayerUsernameAsString();
            Variables.otherPlayerCloneName = Variables.checkingForceRecord != "force" ? Variables.otherPlayerCloneName : "";

            Variables.clientClone = CreatePlayer(Utility.GetColorFromString(Variables.colorClient), Color.gray, Variables.clientCloneName);
            Variables.otherPlayerClone = CreatePlayer(Utility.GetColorFromString(Variables.colorOtherPlayer), Color.gray, Variables.otherPlayerCloneName);
        }
        public static void ExtractData(string line)
        {
            System.Globalization.CultureInfo cultureInfo = new System.Globalization.CultureInfo("fr-FR");

            string[] data = line.Split(';');

            Variables.isClientCloneTagged = float.Parse(data[4], cultureInfo);

            Variables.clientClonePosition = new Vector3(
                float.Parse(data[1], cultureInfo),
                float.Parse(data[2], cultureInfo),
                float.Parse(data[3], cultureInfo)
            );

            Variables.otherPlayerClonePosition = new Vector3(
                float.Parse(data[5], cultureInfo),
                float.Parse(data[6], cultureInfo),
                float.Parse(data[7], cultureInfo)
            );

            Variables.clientCloneRotation = new Vector3(
                float.Parse(data[8], cultureInfo),
                float.Parse(data[9], cultureInfo),
                float.Parse(data[10], cultureInfo)
            );

            Variables.clientCloneQRotation = Quaternion.Euler(Variables.clientCloneRotation);
        }
        public static void HandleMinimap()
        {
            if (Variables.minimapTrigger)
            {
                if (!Variables.fixedMinimap)
                    Variables.minimap.transform.position = Variables.camera.transform.position + Variables.camera.transform.forward * 3f;

                if (!Variables.isMinimapLoaded)
                {
                    if (Variables.povTrigger || Variables.cinematicTrigger)
                    {
                        Variables.clientBody.useGravity = true;
                        Variables.clientBody.isKinematic = false;
                        Variables.povTrigger = false;
                        Variables.cinematicTrigger = false;
                    }

                    Variables.initialMapPosition = Variables.camera.transform.position + Variables.camera.transform.forward * 3f;

                    try
                    {
                        string filePath = Variables.defaultFolderPath + "ReplayMod\\minimaps\\mapsObjectsDatas\\" + Variables.replayMap.ToString() + ".txt";
                        string[] lines = File.ReadAllLines(filePath);

                        foreach (string line in lines)
                        {
                            string[] info = line.Split(',');

                            string name = info[0];
                            Vector3 position = ReplayControllerFunctions.ParseVector3(info, 1);
                            Vector3 scale = ReplayControllerFunctions.ParseVector3(info, 4);
                            Quaternion rotation = ReplayControllerFunctions.ParseQuaternion(info, 8);

                            string objPath = Variables.defaultFolderPath + "ReplayMod\\minimaps\\mapsObjects\\" + Variables.replayMap.ToString() + "\\" + name + ".obj";
                            GameObject newObj = new OBJLoader().Load(objPath, null);

                            Rigidbody rb = newObj.AddComponent<Rigidbody>();
                            rb.useGravity = false;
                            rb.isKinematic = true;

                            newObj.transform.position = position;
                            newObj.transform.localScale = scale;
                            newObj.transform.rotation = rotation;
                            newObj.transform.parent = Variables.minimap.transform;
                        }

                        Variables.minimap.transform.localScale *= Variables.minimapSize;
                        Variables.isMinimapLoaded = true;
                    }
                    catch (FileNotFoundException)
                    {
                        Variables.chatBox.ForceMessage("■<color=red>This minimap doesn't support Minimap replay</color>■");
                        Variables.minimapTrigger = false;
                        Variables.replayStop = true;
                    }
                }

                DateTime end = DateTime.Now;
                TimeSpan ts = end - Variables.fixMapTimeStamp;

                if (Input.GetKey("f") && ts.TotalMilliseconds >= 150)
                {
                    Variables.fixedMinimap = !Variables.fixedMinimap;
                    Variables.fixMapTimeStamp = DateTime.Now;

                    if (Variables.fixedMinimap)
                        Variables.chatBox.ForceMessage("■<color=yellow>Fixed map</color>■");
                    else
                        Variables.chatBox.ForceMessage("■<color=yellow>Free map</color>■");
                }

                Vector3 scalePlayer = new Vector3(Variables.minimapSize, Variables.minimapSize, Variables.minimapSize);
                Variables.clientClone.transform.localScale = scalePlayer;
                Variables.otherPlayerClone.transform.localScale = scalePlayer;

                Vector3 relativePosPlayer1 = (Variables.clientClonePosition - Variables.initialMapPosition) * Variables.minimapSize;
                Vector3 relativePosPlayer2 = (Variables.otherPlayerClonePosition - Variables.initialMapPosition) * Variables.minimapSize;

                Variables.clientClone.transform.position = relativePosPlayer1 + Variables.minimap.transform.position;
                Variables.otherPlayerClone.transform.position = relativePosPlayer2 + Variables.minimap.transform.position;

                Variables.clientClone.transform.rotation = Variables.clientCloneQRotation;
            }
            else
            {
                Variables.clientClone.transform.position = Variables.clientClonePosition;
                Variables.otherPlayerClone.transform.position = Variables.otherPlayerClonePosition;
                Variables.clientClone.transform.rotation = Variables.clientCloneQRotation;
            }
        }
        public static void HandleTag()
        {
            if (Variables.isClientCloneTagged == 1)
            {
                if (!Variables.tagCloneSwitch)
                {
                    Utility.PlayMenuSound();
                    Variables.tagCloneSwitch = true;
                }

                ChangeHeadColor(Variables.clientClone, "Head", Color.red);
                ChangeHeadColor(Variables.otherPlayerClone, "Head", Color.blue);
            }
            else
            {
                if (Variables.tagCloneSwitch)
                {
                    Utility.PlayMenuSound();
                    Variables.tagCloneSwitch = false;
                }

                ChangeHeadColor(Variables.clientClone, "Head", Color.blue);
                ChangeHeadColor(Variables.otherPlayerClone, "Head", Color.red);
            }

            if (Variables.cinematicTrigger)
            {
                float distFactor = (Variables.isClientCloneTagged == 1f) ? Variables.distFromPlayers : (1 - Variables.distFromPlayers + 0.1f);
                float height = (Variables.isClientCloneTagged == 1f) ? Variables.cinematicCloseHeight : Variables.cinematicFarHeight;
                Variables.newCinematicCameraPosition = Variables.clientClonePosition * distFactor + Variables.otherPlayerClonePosition * (1 - distFactor) + new Vector3(0, height, 0);
                Variables.cinematicCameraLookAtPoint = Variables.clientClonePosition;
            }
        }
        public static void ResetMinimapAndScale()
        {
            if (Variables.minimapTrigger)
            {
                UnityEngine.Object.Destroy(Variables.minimap);
                Variables.minimapTrigger = false;
                Variables.isMinimapLoaded = false;
                Variables.fixedMinimap = false;
                Variables.clientClone.transform.localScale = new Vector3(1f, 1f, 1f);
                Variables.otherPlayerClone.transform.localScale = new Vector3(1f, 1f, 1f);
            }
        }
        public static void HandlePOVTrigger()
        {
            if (Variables.povTrigger && GameData.GetMapId() == Variables.replayMap && !Variables.replayStop)
            {
                ResetMinimapAndScale();

                if (Variables.clientCloneVisibility)
                {
                    TogglePlayerVisibility(Variables.clientClone);
                    Variables.clientCloneVisibility = false;
                }
                Variables.clientBody.transform.position = Variables.clientClonePosition;
                Vector3 eulerRotation = Variables.clientCloneRotation;
                Quaternion rotation = Quaternion.Euler(eulerRotation);
                Vector3 targetPosition = Variables.clientMovement.playerCam.position + (rotation * Vector3.forward);
                Variables.clientMovement.playerCam.LookAt(targetPosition);
            }
        }
        public static void HandleCinematicTrigger()
        {
            if (Variables.cinematicTrigger && GameData.GetMapId() == Variables.replayMap && !Variables.replayStop)
            {
                ResetMinimapAndScale();

                if (!Variables.clientCloneVisibility)
                {
                    TogglePlayerVisibility(Variables.clientClone);
                    Variables.clientCloneVisibility = true;
                }
                if (!Variables.otherPlayerCloneVisibility && Variables.checkingForceRecord != "force")
                {
                    TogglePlayerVisibility(Variables.otherPlayerClone);
                    Variables.otherPlayerCloneVisibility = true;
                }

                RaycastHit hit;
                if (Physics.Linecast(Variables.clientBody.transform.position, Variables.newCinematicCameraPosition, out hit))
                {
                    Variables.newCinematicCameraPosition = hit.point - (Variables.cinematicCameraLookAtPoint - Variables.newCinematicCameraPosition).normalized * 1.0f;
                }

                Variables.clientBody.transform.position = Vector3.SmoothDamp(Variables.clientBody.transform.position, Variables.newCinematicCameraPosition, ref Variables.cinematicCameraVelocity, Variables.posSmoothness);

                Quaternion targetRotation = Quaternion.LookRotation(Variables.cinematicCameraLookAtPoint - Variables.newCinematicCameraPosition);
                Quaternion interpolatedRotation = Quaternion.Slerp(Variables.clientMovement.playerCam.rotation, targetRotation, Variables.rotSmoothness);
                Vector3 newLookAtPoint = Variables.newCinematicCameraPosition + interpolatedRotation * Vector3.forward;

                Variables.clientMovement.playerCam.LookAt(newLookAtPoint);
            }
        }
        public static void HandleReplayEnd()
        {
            Variables.clientCloneVisibility = true;
            Variables.otherPlayerCloneVisibility = true;

            // Destruction of objects
            UnityEngine.Object.Destroy(Variables.clientClone);
            UnityEngine.Object.Destroy(Variables.otherPlayerClone);
            UnityEngine.Object.Destroy(Variables.minimap);

            Variables.csvReplayReader.Close();

            // Resetting variables
            Variables.replayStop = false;
            Variables.replayEnded = true;
            Variables.isReplayReaderInitialized = false;
            Variables.replayTrigger = false;
            Variables.replayDate = 0;
            Variables.replayMap = 100;

            if ((Variables.cinematicTrigger || Variables.povTrigger) && !Variables.replayStop && GameData.GetGameModeIdAsString() == "13")
            {
                Variables.clientBody.transform.position = Variables.initialMapPosition;
            }

            Variables.clientBody.useGravity = true;
            Variables.clientBody.isKinematic = false;
            Variables.povTrigger = false;
            Variables.cinematicTrigger = false;
            Variables.minimapTrigger = false;
            Variables.fixedMinimap = false;
            Variables.isMinimapLoaded = false;
            Variables.replaySafeClose = true;

            Variables.chatBox.ForceMessage("■<color=orange>Mode Replay OFF</color>■");
        }
        public static void ChangeHeadColor(GameObject player, string headName, Color newColor)
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
        public static void TogglePlayerVisibility(GameObject player)
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
        public static GameObject CreatePlayer(Color bodyColor, Color headColor, string playerName)
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
        public static GameObject CreatePlayerComponent(GameObject parent, PrimitiveType type, string name, Color color, Vector3 localScale, Vector3? localPosition = null)
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
        public static GameObject CreatePlayerLabel(GameObject parent, string labelText, Vector3 localPosition)
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
        public static void HandleClientBody()
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
    }

    public class RecordControllerFunctions
    {
        public static void HandleForceRecord()
        {
            if (!Variables.isForceRecord)
            {
                InitRecord("force", "Record");
                Variables.isForceRecord = true;
            }

            if ((DateTime.Now - Variables.lastRecordTime).TotalSeconds >= 1.0 / Variables.recordFPS)
            {
                Utility.LogAllData(Variables.replayFileName, Variables.startGame);
                Variables.lastRecordTime = DateTime.Now;
            }
        }
        public static void HandleGamePlay()
        {
            if (!Variables.recording && !Variables.forceRecord)
            {
                Variables.gameManager = GameObject.Find("/GameManager (1)").GetComponent<GameManager>();
                Variables.activePlayers = GameManager.Instance.activePlayers;
                Variables.clientPlayerId = MultiplayerData.FindEnemies();
                Variables.clientName = ClientData.GetPlayerUsernameAsString();
                Variables.otherPlayerName = MultiplayerData.GetOtherPlayerUsername(Variables.clientPlayerId);
                InitRecord(Variables.clientName, Variables.otherPlayerName);
                Variables.recording = true;
                Variables.gameEnded = false;
            }

            if ((DateTime.Now - Variables.lastRecordTime).TotalSeconds >= 1.0 / Variables.recordFPS)
            {
                Utility.LogAllData(Variables.replayFileName, Variables.startGame);
                Variables.lastRecordTime = DateTime.Now;
            }
        }
        public static void InitRecord(string first, string second)
        {
            Variables.startGame = DateTime.Now;
            long startGameTimeMilliseconds = new DateTimeOffset(Variables.startGame).ToUnixTimeMilliseconds();
            string[] filenameArray =
            {
                    first,
                    second,
                    startGameTimeMilliseconds.ToString(),
                    GameData.GetMapId().ToString(),
                    Variables.recordFPS.ToString(),
                };
            Variables.replayFileName = Utility.StringsArrayToCsvLine(filenameArray);
            Variables.logData = "";

            CreateDirectoryIfNotExists(Variables.mainFolderPath + "replays\\");
            CleanDirectory(Variables.mainFolderPath + "replays\\", Variables.maxReplayFile);

            Variables.lastRecordTime = DateTime.Now;
        }
        public static void CreateDirectoryIfNotExists(string directory)
        {
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
        }
        public static void CleanDirectory(string directoryPath, int numberOfFilesToKeep)
        {
            var files = new DirectoryInfo(directoryPath).GetFiles();
            var orderedFiles = files.OrderByDescending(f => f.CreationTime).ToList();

            while (orderedFiles.Count > numberOfFilesToKeep)
            {
                orderedFiles.Last().Delete();
                orderedFiles.RemoveAt(orderedFiles.Count - 1);
            }
        }
        public static void RenameFile()
        {
            string sourceFile = Variables.mainFolderPath + "replays\\" + Variables.replayFileName + ";Recording.txt";
            System.IO.FileInfo fi = new System.IO.FileInfo(sourceFile);
            if (fi.Exists)
            {
                fi.MoveTo(Variables.mainFolderPath + "replays\\" + Variables.replayFileName + ".txt");
            }
        }
        public static void EndGame()
        {
            if (!Variables.gameEnded)
            {
                RenameFile();
            }

            Variables.recording = false;
            Variables.gameEnded = true;
        }
    }

    public class MenuFunctions
    {
        public static void RegisterDataCallback(string s, System.Func<string> f)
        {
            Variables.DebugDataCallbacks.Add(s, f);
        }
        public static void RegisterDataCallbacks(System.Collections.Generic.Dictionary<string, System.Func<string>> dict)
        {
            foreach (System.Collections.Generic.KeyValuePair<string, System.Func<string>> pair in dict)
            {
                Variables.DebugDataCallbacks.Add(pair.Key, pair.Value);
            }
        }
        public static void CheckMenuFileExists()
        {
            if (!System.IO.File.Exists(Variables.menuPath))
            {
                System.IO.File.WriteAllText(Variables.menuPath, "\t\r\n\tReplay ID selected  : [REPLAYFILE]  |  Replay speed : [REPLAYSPEED]  |  View : [VIEWMODE]  |  Map : [MAP]  |  Date : [DATE]  | Replay State : [REPLAYSTATUS]\r\n\r\n\r\n\t______________________________________________________________________\r\n\r\n<b>\r\n\r\n\t[MENUBUTTON0]\r\n\r\n\r\n\r\n\t[MENUBUTTON1]\r\n\r\n\r\n\r\n\t[MENUBUTTON2]\r\n\r\n\r\n\r\n\t[MENUBUTTON3]\r\n\r\n\r\n\r\n\t[MENUBUTTON4]\r\n\r\n</b>", System.Text.Encoding.UTF8);
            }
            if (!System.IO.File.Exists(Variables.mapNamePath))
            {
                System.IO.File.WriteAllText(Variables.mapNamePath, "0 : Bitter Beach\r\n1 : Blueline\r\n2 : Cocky Containers\r\n3 : Color Climb\r\n4 : Crusty Rocks\r\n5 : Desert\r\n6 : Dorm\r\n7 : Funky Field\r\n8 : Glass Jump\r\n9 : Hasty Hill\r\n10 : Icy Crack\r\n11 : Icy Islands\r\n12 : Icy Rocks\r\n13 : Islands\r\n14 : Karlson\r\n15 : Lanky Lava\r\n16 : Lava Lake\r\n17 : Plains\r\n18 : Playground\r\n19 : Playground 2\r\n20 : Return to Monke\r\n21 : Sandstorm\r\n22 : Slippery Slope\r\n23 : (S) Color Climb\r\n24 : (S) Glass Jump\r\n25 : (S) Hill\r\n26 : (S) Icy Islands\r\n27 : (S) Islands\r\n28 : (S) Playground\r\n29 : Snowtop\r\n30 : Splat\r\n31 : Splot\r\n32 : Sunny Saloon\r\n33 : Toxic Train\r\n34 : Twisted Towers\r\n35 : Mini Monke\r\n36 : (S) Beach\r\n37 : (S) Saloon\r\n38 : (S) Containers\r\n39 : Tiny Town 2\r\n40 : Tiny Town\r\n41 : Dodgy Fields\r\n42 : Dodgy Ice\r\n43 : Dodgy Snow\r\n44 : Dodgy Streets\r\n45 : Sandy Islands\r\n46 : (S) Sandy Islands\r\n47 : Cheeky Chamber\r\n48 : Lava Drop\r\n49 : Lava Dump\r\n50 : Peaceful Platform\r\n51 : Salty Island\r\n52 : Skybox\r\n53 : Saucy Stage\r\n54 : Lava Climb\r\n55 : Macaroni Mountain\r\n56 : Sussy Sandcastle\r\n57 : Sussy Slope\r\n58 : Sandy Stones\r\n59 : Crabfields\r\n60 : Crabheat\r\n61 : Crabland", System.Text.Encoding.UTF8);
            }

            if (!System.IO.Directory.Exists(Variables.mainFolderPath + "replays"))
            {
                System.IO.Directory.CreateDirectory(Variables.mainFolderPath + "replays");
            }
            if (!System.IO.File.Exists(Variables.replayInitPath))
            {
                System.IO.File.WriteAllText(Variables.replayInitPath, "", System.Text.Encoding.UTF8);
            }

        }
        public static void LoadMenuLayout()
        {
            Variables.layout = System.IO.File.ReadAllText(Variables.menuPath, System.Text.Encoding.UTF8);
        }
        public static void RegisterDefaultCallbacks()
        {
            RegisterDataCallbacks(new System.Collections.Generic.Dictionary<string, System.Func<string>>(){
                {"REPLAYFILE", () => Variables.replayFile.ToString()},
                {"REPLAYSPEED", () => Variables.replaySpeed.ToString()},
                {"VIEWMODE", GetViewMode},
                {"MAP",GetMapName},
                {"DATE", Utility.ConvertUnixTimestamp},
                {"REPLAYSTATUS", GetReplayStatus},
                {"RECORDSTATUS",GetRecordStatus},
                {"MENUBUTTON0",() => Variables.displayButton0},
                {"MENUBUTTON1",() => Variables.displayButton1},
                {"MENUBUTTON2",() => Variables.displayButton2},
                {"MENUBUTTON3",() => Variables.displayButton3},
                {"MENUBUTTON4",() => Variables.displayButton4},
            });
        }

        public static string FormatLayout()
        {
            string formatted = Variables.layout;
            foreach (System.Collections.Generic.KeyValuePair<string, System.Func<string>> pair in Variables.DebugDataCallbacks)
            {
                formatted = formatted.Replace("[" + pair.Key + "]", pair.Value());
            }
            return formatted;
        }
        public static string GetViewMode()
        {
            switch (true)
            {
                case var _ when !Variables.minimapTrigger && !Variables.povTrigger && !Variables.cinematicTrigger:
                    return "Classic";
                case var _ when Variables.minimapTrigger:
                    return "Minimap";
                case var _ when Variables.povTrigger:
                    return "POV";
                case var _ when Variables.cinematicTrigger:
                    return "Cinematic";
                default:
                    return "";
            }
        }
        public static string GetMapName()
        {
            int mapId = Variables.replayMap;

            // Lecture du contenu du fichier texte
            string[] lines = System.IO.File.ReadAllLines(Variables.defaultFolderPath + "ReplayMod\\minimaps\\mapsObjectsDatas\\MapName.txt");

            // Parcourir les lignes et rechercher l'ID correspondant
            foreach (string line in lines)
            {
                string[] parts = line.Split(':');
                if (parts.Length == 2)
                {
                    int id;
                    if (int.TryParse(parts[0], out id))
                    {
                        if (id == mapId)
                        {
                            return parts[1];
                        }
                    }
                }
            }

            return "Unknown"; // ID de map non trouvé dans le fichier
        }
        public static string GetMapName(int mapId)
        {
            // Lecture du contenu du fichier texte
            string[] lines = System.IO.File.ReadAllLines(Variables.defaultFolderPath + "ReplayMod\\minimaps\\mapsObjectsDatas\\MapName.txt");

            // Parcourir les lignes et rechercher l'ID correspondant
            foreach (string line in lines)
            {
                string[] parts = line.Split(':');
                if (parts.Length == 2)
                {
                    int id;
                    if (int.TryParse(parts[0], out id))
                    {
                        if (id == mapId)
                        {
                            return parts[1];
                        }
                    }
                }
            }

            return "Unknown"; // ID de map non trouvé dans le fichier
        }
        public static string GetReplayStatus()
        {
            switch (true)
            {
                case var _ when Variables.replayTrigger && !Variables.replayStop:
                    return "Active";
                case var _ when !Variables.replayTrigger && !Variables.replayStop:
                    return "Inactive";
                default:
                    return "Error";
            }
        }
        public static string GetRecordStatus()
        {
            switch (true)
            {
                case var _ when Variables.forceRecord:
                    return "Force recording";
                case var _ when !Variables.recording:
                    return "recording";
                default:
                    return "Inactive";
            }
        }
        public static string GetSelectedReplayFileName()
        {
            bool force = false;
            DirectoryInfo directory = new DirectoryInfo(Variables.mainFolderPath + "replays\\");
            FileInfo[] files = directory.GetFiles("*.txt");

            if (files.Length == 0)
            {
                return "N/A";
            }

            Array.Sort(files, (x, y) => y.LastWriteTime.CompareTo(x.LastWriteTime));

            int fileIndexToRead = Variables.replayFile;
            if (fileIndexToRead >= files.Length)
            {
                return "N/A";
            }
            string[] parts = files[fileIndexToRead].Name.Split(';');
            if (parts.Length < 4)
            {
                return "Wrong nomenclature, check the file name";
            }
            if (parts[0] == "force")
                force = true;

            if (force)
                return "ForceRecord  |  " + GetMapName(int.Parse(parts[3])) + "  |  " + Utility.ConvertUnixTimestamp(long.Parse(parts[2]));

            else
                return parts[0] + " vs " + parts[1] + "  |  " + GetMapName(int.Parse(parts[3])) + "  |  " + Utility.ConvertUnixTimestamp(long.Parse(parts[2]));
        }
        public static string GetSelectedReplayView()
        {
            switch (Variables.subMenuSelector)
            {
                case 0:
                    return "■<color=orange>Classic</color>■    POV    Cinematic    Minimap";
                case 1:
                    return "  Classic  ■<color=orange>POV</color>■  Cinematic    Minimap";
                case 2:
                    return "  Classic    POV  ■<color=orange>Cinematic</color>■  Minimap";
                case 3:
                    return "  Classic    POV    Cinematic  ■<color=orange>Minimap</color>■";
                default:
                    return "";
            }
        }
        public static string GetForceRecordState()
        {
            if (Variables.menuSelector == 3 && Variables.onButton) 
                return "<b><color=red>ON</color></b>";
            else if (Variables.menuSelector == 3)
                return "<b><color=blue>OFF</color></b>";
            return "";

        }
        public static string HandleMenuDisplay(int buttonIndex, Func<string> getButtonLabel, Func<string> getButtonSpecificData)
        {
            string buttonLabel = getButtonLabel();

            if (Variables.menuSelector != buttonIndex)
            {
                Variables.buttonStates[buttonIndex] = false;
                return $" {buttonLabel} ";
            }

            if (!Variables.buttonStates[buttonIndex])
            {
                return $"■<color=yellow>{buttonLabel}</color>■  <b>{getButtonSpecificData()}</b>";
            }
            else
            {
                return $"<color=red>■</color><color=yellow>{buttonLabel}</color><color=red>■</color>  <b>{getButtonSpecificData()}</b>";
            }
        }
        public static void ExecuteSubMenuAction()
        {
            if (!Variables.onButton)
            {
                var selectors = (Variables.menuSelector, Variables.subMenuSelector);

                switch (selectors)
                {
                    case (3, -1):
                        Variables.forceRecord = true;
                        string message = "■<color=yellow>ForceRecord mode ON</color>■";
                        Variables.chatBox.ForceMessage(message);
                        break;
                    case (4, -1):
                        Variables.chatBox.ForceMessage("■<color=black>Replay Stopped</color>■");
                        Variables.replayStop = true;
                        break;
                }
            }
            if (Variables.onButton)
            {
                var selectors = (Variables.menuSelector, Variables.subMenuSelector);

                switch (selectors)
                {
                    case (2, 0):
                        Variables.replayTrigger = true;
                        Variables.chatBox.ForceMessage("■<color=yellow>Replay mode ON</color>■");
                        break;
                    case (2, 1):
                        if (GameData.GetMapId() == Variables.replayMap || !Variables.minimapTrigger)
                        {
                            Variables.chatBox.ForceMessage("■<color=yellow>POV mode ON</color>■");
                            Variables.replayTrigger = true;
                            Variables.povTrigger = true;
                            Variables.cinematicTrigger = false;
                        }
                        else if (Variables.minimapTrigger && Variables.mapId != Variables.replayMap && Variables.gamemodeId == 13)
                        {
                            Variables.chatBox.ForceMessage("■<color=red>Error, you can't use this now, switch into the good map</color>■");
                        }
                        else
                        {
                            Variables.minimapTrigger = false;
                        }
                        break;
                    case (2, 2):
                        if (GameData.GetMapId() == Variables.replayMap || !Variables.minimapTrigger)
                        {
                            Variables.chatBox.ForceMessage("■<color=yellow>Cinematic mode ON</color>■");
                            Variables.replayTrigger = true;
                            Variables.cinematicTrigger = true;
                            Variables.povTrigger = false;
                        }
                        else if (Variables.minimapTrigger && Variables.mapId != Variables.replayMap && Variables.gamemodeId == 13)
                        {
                            Variables.chatBox.ForceMessage("■<color=red>Error, you can't use this now, switch into the good map</color>■");
                        }
                        else
                        {
                            Variables.minimapTrigger = false;
                        }
                        break;
                    case (2, 3):
                        if (GameData.GetGameModId() == 0 || GameData.GetGameModId() == 13)
                        {
                            Variables.chatBox.ForceMessage("■<color=yellow>Minimap mode ON</color>■");
                            Variables.chatBox.ForceMessage("■<color=yellow>Press F to fix the map</color>■");
                            Variables.replayTrigger = true;
                            Variables.minimapTrigger = true;
                            Variables.povTrigger = false;
                            Variables.cinematicTrigger = false;
                        }
                        else
                        {
                            Variables.chatBox.ForceMessage("■<color=red>You can't use that during a game, wait the Lobby!</color>■");
                        }
                        break;
                    case (3, -1):
                        Variables.forceRecord = false;

                        string message = "■<color=black>ForceRecord mode OFF</color>■";
                        Variables.chatBox.ForceMessage(message);
                        break;
                }
                Variables.subMenuSelector = -1;
            }
        }
    }
}
