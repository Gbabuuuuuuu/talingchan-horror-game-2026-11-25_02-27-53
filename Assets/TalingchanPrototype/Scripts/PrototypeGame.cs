using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Talingchan.Sandbox;

namespace Talingchan.Prototype
{
    public enum PrototypeMode { Explore,Inventory,Wheel,Reading,Dialogue,Cutscene,Dead }
    [DefaultExecutionOrder(-200)]
    public partial class PrototypeGame:MonoBehaviour
    {
        [Header("Editable gameplay data")]
        public PrototypeSettings settings;
        public SandboxPlayer player;
        public PrototypeInventory inventory;
        public PrototypeAudio audioSystem;
        public PrototypeChaser chaser;
        public Transform initialCheckpoint;
        [Header("Quest and world state")]
        [SerializeField]public PrototypeMode mode;
        [SerializeField]public List<PrototypeQuestRecord> questStates=new List<PrototypeQuestRecord>();
        [SerializeField]public List<string> flags=new List<string>();
        [SerializeField]public float idleSeconds;
        [SerializeField]public bool inSafeZone;
        [SerializeField]public string lastSaveTime;
        [SerializeField]public PrototypePickup heldItem;
        [Header("Inspector event hooks")]
        public UnityEvent<string> onQuestCompleted=new UnityEvent<string>();
        public UnityEvent onSaved=new UnityEvent();
        public UnityEvent onLoaded=new UnityEvent();
        public UnityEvent onDeath=new UnityEvent();
        public UnityEvent<Vector3,float> onNoise=new UnityEvent<Vector3,float>();
        public bool IsDead=>mode==PrototypeMode.Dead;
        public bool IsExcludedFromAfk=>mode==PrototypeMode.Dead||(mode==PrototypeMode.Reading&&settings.excludeReading)||(mode==PrototypeMode.Dialogue&&settings.excludeDialogue)||(mode==PrototypeMode.Inventory&&settings.excludeInventory)||(mode==PrototypeMode.Wheel&&settings.excludeWheel)||(mode==PrototypeMode.Cutscene&&settings.excludeCutscenes)||(settings.excludeUnfocused&&!Application.isFocused)||(inSafeZone&&settings.respectSafeZones);
        public string FocusPrompt{get;private set;}="";
        public string Message{get;private set;}="";
        public bool AllStoryQuestsComplete=>settings.quests!=null&&settings.quests.Any(q=>q!=null&&q.requiredForFinale)&&settings.quests.Where(q=>q!=null&&q.requiredForFinale).All(q=>GetQuest(q).completed);
        public string SavePath=>Path.Combine(Application.persistentDataPath,Path.GetFileName(settings.saveFileName));
        public string ReadingTitle{get;private set;}
        public string ReadingText{get;private set;}
        public PrototypeStation DialogueStation{get;private set;}
        public event Action<Vector3,float> Noise;
        readonly Dictionary<string,InputAction> actions=new Dictionary<string,InputAction>();
        readonly HashSet<string> pressed=new HashSet<string>();
        readonly HashSet<string> safeZones=new HashSet<string>();
        readonly Dictionary<string,PrototypePickup> pickups=new Dictionary<string,PrototypePickup>();
        readonly List<PrototypePickup> originalPickups=new List<PrototypePickup>();
        readonly Dictionary<string,PrototypeItemDefinition> items=new Dictionary<string,PrototypeItemDefinition>();
        bool wheelHeld,activity,initialized,introReading;
        string readingFlag;
        float messageUntil,graceUntil;
        IPrototypeInteractable focus;
        PrototypePickup highlighted;
        MaterialPropertyBlock highlightBlock;
        PrototypeSaveData checkpoint;
        Transform currentCheckpoint;
        Coroutine deathRoutine;
        void Awake()
        {
            if(settings==null||player==null){enabled=false;Debug.LogError("PrototypeGame is missing settings or player.",this);return;}
            if(inventory==null)inventory=GetComponent<PrototypeInventory>();if(audioSystem==null)audioSystem=GetComponent<PrototypeAudio>();
            foreach(var item in settings.items)if(item!=null&&!string.IsNullOrEmpty(item.id))items[item.id]=item;
            inventory.Resize(settings.inventorySlots);
            Bind("interact",settings.interactKey);Bind("inventory",settings.inventoryKey);Bind("drop",settings.dropKey);Bind("use",settings.useKey);Bind("save",settings.saveKey);Bind("load",settings.loadKey);Bind("close",settings.closeKey);Bind("confirm",settings.confirmKey);Bind("debugChaser",settings.debugChaserKey);Bind("debugDeath",settings.debugDeathKey);
            for(int i=0;i<10;i++)Bind("slot"+i,i==9?Key.Digit0:(Key)((int)Key.Digit1+i));
            var wheel=new InputAction("Prototype wheel",InputActionType.Button,Binding(settings.wheelKey));wheel.performed+=c=>{wheelHeld=true;activity=true;};wheel.canceled+=c=>wheelHeld=false;actions["wheel"]=wheel;
            var carry=new InputAction("Prototype carry",InputActionType.Button,"<Mouse>/rightButton");carry.performed+=c=>{pressed.Add("carry");activity=true;};actions["carry"]=carry;
            var toss=new InputAction("Prototype throw",InputActionType.Button,"<Mouse>/leftButton");toss.performed+=c=>{pressed.Add("throw");activity=true;};actions["throw"]=toss;
            highlightBlock=new MaterialPropertyBlock();currentCheckpoint=initialCheckpoint;
        }
        static string Binding(Key key)=>"<Keyboard>/"+key.ToString().Replace("Digit","").ToLowerInvariant();
        void Bind(string id,Key key){var action=new InputAction("Prototype "+id,InputActionType.Button,Binding(key));action.performed+=c=>{pressed.Add(id);activity=true;};actions[id]=action;}
        void OnEnable(){foreach(var action in actions.Values)action.Enable();}
        void OnDisable()
        {
            foreach(var action in actions.Values)action.Disable();
            if(player!=null){player.externalMovementLocked=false;player.externalLookLocked=false;player.externalCursorManaged=false;player.externalCameraControlled=false;}
            SetHighlight(null);
        }
        void OnDestroy(){foreach(var action in actions.Values)action.Dispose();}
        void Start()
        {
            foreach(var p in UnityEngine.Object.FindObjectsByType<PrototypePickup>(FindObjectsInactive.Include)){RegisterPickup(p);if(p.sceneSpawn)originalPickups.Add(p);}
            initialized=true;
            if(settings.autoLoadOnStart&&File.Exists(SavePath)&&LoadCheckpoint())return;
            NewRun();
        }
        void Update()
        {
            if(!initialized)return;
            // Held controls follow the current device state, including after a UI/focus change.
            wheelHeld=Keyboard.current!=null&&Keyboard.current[settings.wheelKey].isPressed;
            inventory.Resize(settings.inventorySlots,(item,count)=>SpawnDrop(item,count,DropPosition(),Quaternion.identity));
            if(settings.enableDebugShortcuts&&pressed.Contains("debugDeath"))Die();
            if(settings.enableDebugShortcuts&&pressed.Contains("debugChaser"))RequestChaser(true);
            if(pressed.Contains("load"))LoadCheckpoint();
            if(!IsDead&&mode!=PrototypeMode.Cutscene)
            {
                if(pressed.Contains("close")){if(mode!=PrototypeMode.Explore)CloseModal();else ReleaseCursor();}
                if(pressed.Contains("inventory")){if(mode==PrototypeMode.Inventory)SetMode(PrototypeMode.Explore);else if(mode==PrototypeMode.Explore||mode==PrototypeMode.Wheel)SetMode(PrototypeMode.Inventory);}
                if(wheelHeld&&mode==PrototypeMode.Explore)SetMode(PrototypeMode.Wheel);
                if(!wheelHeld&&mode==PrototypeMode.Wheel)SetMode(PrototypeMode.Explore);
                if((mode==PrototypeMode.Reading||mode==PrototypeMode.Dialogue)&&pressed.Contains("confirm"))CloseModal();
                if(mode==PrototypeMode.Explore)
                {
                    if(Cursor.lockState!=CursorLockMode.Locked&&pressed.Contains("throw"))CaptureCursor();
                    else if(settings.throwWithLeftMouse&&pressed.Contains("throw")&&heldItem!=null)ReleaseHeld(true);
                    if(settings.carryWithRightMouse&&pressed.Contains("carry"))ToggleCarry();
                    UpdateFocus();
                    if(pressed.Contains("interact")){if(heldItem!=null)Collect(heldItem);else focus?.Interact(this);}
                    if(pressed.Contains("drop"))DropSelected();if(pressed.Contains("use"))UseSelected();
                    if(pressed.Contains("save"))SaveQuickCheckpoint();
                    for(int i=0;i<10&&i<inventory.Slots.Count;i++)if(pressed.Contains("slot"+i))inventory.selectedSlot=i;
                }
                else{focus=null;FocusPrompt="";SetHighlight(null);}
            }
            if(!IsDead&&player.transform.position.y<settings.playerDeathY)Die();
            UpdateAfk(Time.deltaTime,activity||HasMovementInput());
            ApplyPlayerControls();pressed.Clear();activity=false;
            if(Time.unscaledTime>messageUntil)Message="";
        }
        void LateUpdate()
        {
            if(heldItem!=null){heldItem.transform.SetParent(player.view,false);heldItem.transform.localPosition=settings.carryLocalPosition;heldItem.transform.localRotation=Quaternion.Euler(0,20,0);}
        }
        bool HasMovementInput()
        {
            var keys=Keyboard.current;var mouse=Mouse.current;
            return keys!=null&&(keys.wKey.isPressed||keys.aKey.isPressed||keys.sKey.isPressed||keys.dKey.isPressed||keys.upArrowKey.isPressed||keys.downArrowKey.isPressed||keys.leftArrowKey.isPressed||keys.rightArrowKey.isPressed)||(mouse!=null&&Cursor.lockState==CursorLockMode.Locked&&mouse.delta.ReadValue().magnitude>settings.afkMouseThreshold);
        }
        public void UpdateAfk(float seconds,bool active)
        {
            if(active){idleSeconds=0;return;}if(IsExcludedFromAfk||!settings.enableAfkChaser)return;
            idleSeconds+=Mathf.Max(0,seconds);
            if(idleSeconds>=settings.afkSeconds){if(RequestChaser())idleSeconds=0;}
        }
        public void SetMode(PrototypeMode next)
        {
            if(IsDead&&next!=PrototypeMode.Explore)return;var prior=mode;mode=next;activity=true;ApplyPlayerControls();if(next==PrototypeMode.Explore&&prior!=PrototypeMode.Explore)CaptureCursor();
        }
        void ApplyPlayerControls()
        {
            bool blocked=mode!=PrototypeMode.Explore;
            player.externalMovementLocked=blocked;player.externalLookLocked=blocked;player.externalCursorManaged=true;player.externalCameraControlled=mode==PrototypeMode.Cutscene;
            var flashlight=player.GetComponent<SandboxFlashlight>();if(flashlight!=null)flashlight.externalInputLocked=blocked;
            bool cursor=mode==PrototypeMode.Inventory||mode==PrototypeMode.Wheel||mode==PrototypeMode.Reading||mode==PrototypeMode.Dialogue;
            if(cursor){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            else if(blocked){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        }
        public void CaptureCursor(){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;player.IgnoreLookForFrames(3);}
        public void ReleaseCursor(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        public void SetSafeZone(string id,bool entered){if(entered)safeZones.Add(id);else safeZones.Remove(id);inSafeZone=safeZones.Count>0;if(inSafeZone&&settings.respectSafeZones&&chaser!=null)chaser.StopHunt();}
        public bool RequestChaser(bool force=false)
        {
            if(chaser==null||IsDead||!force&&inSafeZone&&settings.respectSafeZones||Time.time<graceUntil&&!force)return false;
            return chaser.RequestHunt(force);
        }
        public void Hint(string text){Message=text??"";messageUntil=Time.unscaledTime+settings.messageSeconds;}
        public bool HasFlag(string flag)=>!string.IsNullOrEmpty(flag)&&flags.Contains(flag);
        public void SetFlag(string flag){if(!string.IsNullOrEmpty(flag)&&!flags.Contains(flag))flags.Add(flag);}
        public PrototypeQuestRecord GetQuest(PrototypeQuestDefinition definition)
        {
            var record=questStates.Find(q=>q.id==definition.id);if(record==null){record=new PrototypeQuestRecord{id=definition.id};questStates.Add(record);}return record;
        }
        public bool QuestAvailable(PrototypeQuestDefinition quest)=>quest.prerequisites==null||quest.prerequisites.All(q=>q==null||GetQuest(q).completed);
        public bool QuestReady(PrototypeQuestDefinition quest)
        {
            if(quest==null||GetQuest(quest).completed||!GetQuest(quest).started)return false;
            return (quest.requiredFlags==null||quest.requiredFlags.All(HasFlag))&&(quest.requiredItems==null||quest.requiredItems.All(r=>r.item!=null&&inventory.Count(r.item)>=r.amount));
        }
        public bool TurnIn(PrototypeQuestDefinition quest)
        {
            if(!QuestReady(quest)){Hint(quest!=null?quest.hint:"ยังไม่พร้อม");return false;}
            if(quest.consumeItemsOnTurnIn&&quest.requiredItems!=null)foreach(var r in quest.requiredItems)inventory.Remove(r.item,r.amount);
            GetQuest(quest).completed=true;
            if(quest.rewards!=null)foreach(var r in quest.rewards){int accepted=inventory.Add(r.item,r.amount);if(accepted<r.amount)SpawnDrop(r.item,r.amount-accepted,DropPosition(),Quaternion.identity);}
            foreach(var q in settings.quests)if(q!=null&&q.startAutomatically&&QuestAvailable(q))GetQuest(q).started=true;
            Hint(quest.completionText);onQuestCompleted.Invoke(quest.id);return true;
        }
        public void OpenReading(string title,string text,string flag="",bool intro=false){ReadingTitle=title;ReadingText=text;readingFlag=flag;introReading=intro;SetMode(PrototypeMode.Reading);}
        public void OpenDialogue(PrototypeStation station){DialogueStation=station;if(station.quest!=null&&QuestAvailable(station.quest))GetQuest(station.quest).started=true;SetMode(PrototypeMode.Dialogue);}
        public void CloseModal()
        {
            if(mode==PrototypeMode.Reading){SetFlag(readingFlag);bool saveIntro=introReading&&settings.saveAfterIntro;introReading=false;readingFlag="";SetMode(PrototypeMode.Explore);CaptureCursor();if(saveIntro)SaveQuickCheckpoint();return;}
            if(IsDead)return;DialogueStation=null;wheelHeld=false;SetMode(PrototypeMode.Explore);CaptureCursor();
        }
        public void BeginCutscene(){SetMode(PrototypeMode.Cutscene);}
        public void EndCutscene(){SetMode(PrototypeMode.Explore);CaptureCursor();}
        public void EmitNoise(Vector3 point,float radius){Noise?.Invoke(point,radius);onNoise.Invoke(point,radius);}
        public void Die()
        {
            if(IsDead)return;mode=PrototypeMode.Dead;ReleaseHeld(false);if(chaser!=null)chaser.StopHunt();ApplyPlayerControls();onDeath.Invoke();deathRoutine=StartCoroutine(DeathFlow());
        }
        IEnumerator DeathFlow(){yield return new WaitForSeconds(Mathf.Max(.05f,settings.deathFadeSeconds));deathRoutine=null;if(!LoadCheckpoint()){mode=PrototypeMode.Explore;Teleport(initialCheckpoint!=null?initialCheckpoint.position:player.transform.position,initialCheckpoint!=null?initialCheckpoint.rotation:Quaternion.identity);CaptureCursor();}}
        void CancelDeathFlow(){if(deathRoutine!=null){StopCoroutine(deathRoutine);deathRoutine=null;}}
        [ContextMenu("TEST / Spawn pursuer now")]public void DebugSpawnChaser(){if(Application.isPlaying)RequestChaser(true);}
        [ContextMenu("TEST / Death and load checkpoint")]public void DebugDie(){if(Application.isPlaying)Die();}
        [ContextMenu("TEST / Save checkpoint")]public void DebugSave(){if(Application.isPlaying)SaveQuickCheckpoint();}
        [ContextMenu("TEST / Load checkpoint")]public void DebugLoad(){if(Application.isPlaying)LoadCheckpoint();}
        [ContextMenu("TEST / New run")]public void DebugNewRun(){if(Application.isPlaying)NewRun();}
    }
}
