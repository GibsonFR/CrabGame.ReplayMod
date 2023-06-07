using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ReplayMod
{
    internal class Variables
    {
        public static Il2CppSystem.Collections.Generic.Dictionary<ulong, PlayerManager> activePlayers = null;
        public static Camera camera;
        public static DateTime startGame, fixMapTimeStamp;

        //Manager
        public static GameManager gameManager;
        public static GameModeManager gamemodeManager = null;

        //Instance 
        public static ChatBox chatBox = null;
        public static PlayerInventory clientInventory = null;
        public static PlayerMovement clientMovement = null;
        public static PlayerManager otherPlayerManager = null;

        //RigidBody
        public static Rigidbody clientBody;
        public static Rigidbody otherPlayerBody;

        //GameObject
        public static GameObject clientObject, clientClone, otherPlayerClone, minimap;

        //double[][]

        //string
        public static string replayFileName, logData, clientName, otherPlayerName, clientCloneName, otherPlayerCloneName, checkingForceRecord;
        public static readonly string mainFolderPath = "ReplayMod\\";
        public static string defaultFolderPath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Crab Game\\";
        public static string minimapURL = "https://github.com/GibsonFR/ReplayMod/raw/main/ReplayMod/minimaps.zip";
        public static string downloadPath = Path.Combine(mainFolderPath + "zipFiles\\", "minimaps.zip");
        public static string configFilePath = mainFolderPath + "config\\config.txt";
        public static string extractPath = Path.Combine(mainFolderPath, "minimaps");
        public static string customPrecisionFormatClientPosition = "F4";
        public static string customPrecisionFormatClientRotation = "F3";
        public static string customPrecisionFormatTargetPosition = "F2";
        public static string colorClient = "black";
        public static string colorOtherPlayer = "white";

        //ulong
        public static ulong clientId;

        //double

        //float
        public static float otherPlayerSpeed;
        public static float smoothedSpeed = 0;
        public static float smoothingFactor = 0.7f;
        public static float replaySpeed = 1;
        public static float posSmoothness = 0.1f;
        public static float rotSmoothness = 0.1f;
        public static float distFromPlayers = 0.8f;
        public static float minimapSize = 0.1f;
        public static float isClientCloneTagged;

        //int
        public static int mapId;
        public static int gamemodeId;
        public static int clientPlayerId;
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
        public static bool gameEnded = true;
        public static bool forceRecord = false;
        public static bool replayStop = false;
        public static bool minimapTrigger = false;
        public static bool replayTrigger = false;
        public static bool povTrigger = false;
        public static bool cinematicTrigger = false;
        public static bool isReplayReaderInitialized = false;
        public static bool tagCloneSwitch = false;
        public static bool replayEnded = false;
        public static bool isMinimapLoaded = false;
        public static bool fixedMinimap = false;
        public static bool clientCloneVisibility = true;
        public static bool otherPlayerCloneVisibility = true;
        public static bool replaySafeClose = true;
        public static bool isForceRecord = false;
        public static bool recording = false;

        //Vector3
        public static Vector3 initialMapPosition = Vector3.zero;
        public static Vector3 cinematicCameraVelocity = Vector3.zero;
        public static Vector3 newCinematicCameraPosition = Vector3.zero;
        public static Vector3 cinematicCameraLookAtPoint = Vector3.zero;
        public static Vector3 clientCloneRotation;
        public static Vector3 clientClonePosition;
        public static Vector3 otherPlayerClonePosition;

        //Quaternion
        public static Quaternion clientCloneQRotation;

        //StreamReader
        public static StreamReader csvReplayReader;
    }
}
