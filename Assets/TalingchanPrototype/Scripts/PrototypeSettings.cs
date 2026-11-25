using UnityEngine;
using UnityEngine.InputSystem;

namespace Talingchan.Prototype
{
    [CreateAssetMenu(menuName="Talingchan Prototype/Gameplay Settings")]
    public class PrototypeSettings : ScriptableObject
    {
        [Header("Inventory / interaction")]
        [Min(1)]public int inventorySlots=10;
        public float interactionDistance=2.8f;
        public Vector3 carryLocalPosition=new Vector3(.30f,-.25f,.65f);
        public float throwSpeed=10;
        public float dropDistance=1.2f;
        public float itemRecoveryY=-5;
        public float playerDeathY=-6;
        public LayerMask interactionMask=~(1<<2);
        [Header("AFK - seconds")]
        public bool enableAfkChaser=true;
        [Min(1)]public float afkSeconds=120;
        public float afkMouseThreshold=2;
        public bool excludeReading=true;
        public bool excludeDialogue=true;
        public bool excludeInventory=true;
        public bool excludeWheel=true;
        public bool excludeCutscenes=true;
        public bool excludeUnfocused=true;
        public bool respectSafeZones=true;
        [Header("Checkpoints")]
        public string saveFileName="talingchan-gameplay-prototype-v1.json";
        public bool autoLoadOnStart=true;
        public bool saveAfterIntro=true;
        public bool allowSavingDuringChase=false;
        public float deathFadeSeconds=1.5f;
        public float respawnGraceSeconds=4;
        [Header("New run / temporary content")]
        public bool randomizeNewRunLoot=false;
        public int newRunSeed=0;
        public bool showIntro=true;
        public bool startWithFlashlightItem=true;
        public PrototypeItemDefinition flashlightItem;
        public PrototypeItemDefinition[] items;
        public PrototypeQuestDefinition[] quests;
        public string gameTitle="บ้านริมคลอง";
        public string introTitle="คืนที่ไม่คุ้นเคย";
        [TextArea(4,12)]public string introText;
        [Header("Input")]
        public Key interactKey=Key.E;
        public Key inventoryKey=Key.Tab;
        public Key wheelKey=Key.LeftAlt;
        public Key dropKey=Key.Q;
        public Key useKey=Key.X;
        public Key closeKey=Key.Escape;
        public Key confirmKey=Key.Enter;
        public bool carryWithRightMouse=true;
        public bool throwWithLeftMouse=true;
        public Key saveKey=Key.F5;
        public Key loadKey=Key.F9;
        public Key debugChaserKey=Key.F8;
        public Key debugDeathKey=Key.F7;
        public bool enableDebugShortcuts=true;
        public bool allowQuickSaveAnywhere=false;
        public bool highlightFocusedItems=true;
        [Header("Presentation")]
        public Font uiFont;
        public float messageSeconds=4;
        public float uiScale=1;
        public int bodyFontSize=18;
        public int titleFontSize=25;
        public Rect questRect=new Rect(910,18,350,235);
        public Rect inventoryRect=new Rect(125,105,1030,490);
        public Rect modalRect=new Rect(245,125,790,465);
        public float wheelRadius=210;
        public string controlsText="E ใช้งาน  |  Tab กระเป๋า  |  Alt วงล้อ  |  คลิกขวา ถือ/วาง  |  คลิกซ้าย ปา";
        void OnValidate(){inventorySlots=Mathf.Clamp(inventorySlots,1,40);interactionDistance=Mathf.Max(.5f,interactionDistance);afkSeconds=Mathf.Max(1,afkSeconds);uiScale=Mathf.Max(.5f,uiScale);}
    }
}
