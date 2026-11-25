using UnityEngine;
using UnityEngine.InputSystem;

namespace Talingchan.Prototype
{
    public class PrototypeHud:MonoBehaviour
    {
        public PrototypeGame game;
        [Header("Inspector presentation")]
        public Color panelColor=new Color(.035f,.045f,.05f,.94f);
        public Color accentColor=new Color(.86f,.73f,.4f);
        public bool showQuestTracker=true;
        public bool showControls=true;
        public bool showTestStatus=true;
        public float slotWidth=180,slotHeight=94;
        public int inventoryColumns=5;
        GUIStyle text,title,button,small;
        Vector2 scroll;
        void OnGUI()
        {
            if(game==null||game.settings==null)return;
            var s=game.settings;var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f*s.uiScale,Screen.height/720f*s.uiScale,1));
            text=new GUIStyle(GUI.skin.label){font=s.uiFont,fontSize=s.bodyFontSize,wordWrap=true,richText=true};text.normal.textColor=Color.white;
            title=new GUIStyle(text){fontSize=s.titleFontSize};title.normal.textColor=accentColor;
            small=new GUIStyle(text){fontSize=Mathf.Max(12,s.bodyFontSize-3)};
            button=new GUIStyle(GUI.skin.button){font=s.uiFont,fontSize=s.bodyFontSize,wordWrap=true};
            if(showQuestTracker&&game.mode==PrototypeMode.Explore)DrawQuests(s.questRect);
            if(game.mode==PrototypeMode.Explore)
            {
                GUI.Label(new Rect(634,345,20,25),"·",title);
                if(!string.IsNullOrEmpty(game.FocusPrompt))Panel(new Rect(270,535,740,48),game.FocusPrompt,text);
                var slot=game.inventory.Selected;string selected=slot==null||slot.Empty?"มือว่าง":slot.item.displayName+" ×"+slot.amount;
                GUI.Label(new Rect(20,592,650,30),"ของที่เลือก: "+selected+"   [X ใช้ / Q ทิ้ง]",text);
                var flash=game.player.GetComponent<Talingchan.Sandbox.SandboxFlashlight>();if(flash!=null)GUI.Label(new Rect(20,558,500,28),"ไฟฉาย: "+(flash.EquipProgress<.01f?"เก็บอยู่":flash.IsOn?"เปิด":"ปิด"),small);
                if(showControls)GUI.Label(new Rect(20,635,1240,60),s.controlsText+"\nWASD เดิน • Shift วิ่ง • Space กระโดด • F หยิบ/เก็บไฟฉาย • G เปิด/ปิด",small);
                if(showTestStatus)GUI.Label(new Rect(20,18,600,30),"PROTOTYPE  •  "+(game.inSafeZone?"จุดปลอดภัยชั่วคราว":"นอกจุดปลอดภัย")+"  •  AFK "+Mathf.FloorToInt(game.idleSeconds)+"/"+s.afkSeconds+"s",small);
            }
            if(game.mode==PrototypeMode.Inventory)DrawInventory(s.inventoryRect);
            if(game.mode==PrototypeMode.Wheel)DrawWheel();
            if(game.mode==PrototypeMode.Reading)DrawReading();
            if(game.mode==PrototypeMode.Dialogue)DrawDialogue();
            if(game.IsDead)Panel(new Rect(0,0,1280,720),"",title);
            if(game.IsDead)GUI.Label(new Rect(430,320,500,100),"กลับสู่จุดบันทึกล่าสุด...",title);
            if(!string.IsNullOrEmpty(game.Message))Panel(new Rect(270,475,740,55),game.Message,text);
            GUI.matrix=old;
        }
        void Panel(Rect r,string label,GUIStyle style){var old=GUI.color;GUI.color=panelColor;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;if(!string.IsNullOrEmpty(label))GUI.Label(new Rect(r.x+14,r.y+8,r.width-28,r.height-12),label,style);}
        bool Button(Rect r,string label)=>GUI.Button(r,label,button);
        void DrawQuests(Rect r)
        {
            Panel(r,"",text);float y=r.y+10;GUI.Label(new Rect(r.x+15,y,r.width-30,32),"สิ่งที่ต้องทำ",title);y+=37;
            foreach(var q in game.settings.quests)
            {
                if(q==null)continue;var state=game.GetQuest(q);if(!state.started)continue;
                GUI.Label(new Rect(r.x+15,y,r.width-30,55),(state.completed?"✓ ":"• ")+q.title,text);y+=48;
                if(state.completed)continue;
                foreach(var need in q.requiredItems){GUI.Label(new Rect(r.x+25,y,r.width-40,25),need.item.displayName+" "+game.inventory.Count(need.item)+"/"+need.amount,small);y+=25;}
                for(int i=0;i<q.requiredFlags.Length;i++){var flag=q.requiredFlags[i];string label=q.requiredFlagLabels!=null&&i<q.requiredFlagLabels.Length?q.requiredFlagLabels[i]:flag;GUI.Label(new Rect(r.x+25,y,r.width-40,25),(game.HasFlag(flag)?"✓ ":"○ ")+label,small);y+=25;}
                GUI.Label(new Rect(r.x+15,y+4,r.width-30,60),game.QuestReady(q)?"ครบแล้ว: กลับไปส่งเควส":q.hint,small);y+=62;
            }
        }
        void DrawInventory(Rect r)
        {
            Panel(r,"",text);GUI.Label(new Rect(r.x+22,r.y+15,r.width-44,40),"กระเป๋า "+game.inventory.Slots.Count+" ช่อง",title);
            int cols=Mathf.Max(1,inventoryColumns),rows=Mathf.CeilToInt(game.inventory.Slots.Count/(float)cols);var area=new Rect(r.x+20,r.y+62,r.width-40,r.height-180);
            scroll=GUI.BeginScrollView(area,scroll,new Rect(0,0,cols*slotWidth,rows*slotHeight));
            for(int i=0;i<game.inventory.Slots.Count;i++)Slot(new Rect(i%cols*slotWidth,i/cols*slotHeight,slotWidth-8,slotHeight-8),i);
            GUI.EndScrollView();
            var selected=game.inventory.Selected;
            if(selected!=null&&!selected.Empty)GUI.Label(new Rect(r.x+24,r.yMax-112,r.width-50,44),selected.item.description,small);
            if(Button(new Rect(r.x+25,r.yMax-58,180,40),"ใช้ของที่เลือก")){game.UseSelected();}
            if(Button(new Rect(r.x+220,r.yMax-58,180,40),"ทิ้ง 1 ชิ้น"))game.DropSelected();
            if(Button(new Rect(r.xMax-200,r.yMax-58,175,40),"ปิด [Tab / Esc]"))game.CloseModal();
        }
        void Slot(Rect r,int i)
        {
            var slot=game.inventory.Slots[i];var old=GUI.backgroundColor;GUI.backgroundColor=i==game.inventory.selectedSlot?accentColor:Color.gray;
            string label=(i<10?(i==9?"0":(i+1).ToString())+"  ":"")+(slot.Empty?"ว่าง":slot.item.displayName+"\n×"+slot.amount);
            if(Button(r,label))game.inventory.selectedSlot=i;GUI.backgroundColor=old;
        }
        void DrawWheel()
        {
            Vector2 center=new Vector2(640,360);Panel(new Rect(300,50,680,610),"",text);GUI.Label(new Rect(400,63,500,40),"เลือกของ • ปล่อย Alt เพื่อกลับ",title);
            int count=game.inventory.Slots.Count;float radius=game.settings.wheelRadius;
            for(int i=0;i<count;i++){float angle=(-90+i*360f/count)*Mathf.Deg2Rad;Vector2 point=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;var r=new Rect(point.x-61,point.y-28,122,56);Slot(r,i);
                if(r.Contains(Event.current.mousePosition))game.inventory.selectedSlot=i;}
            GUI.Label(new Rect(535,335,210,65),"โลกยังดำเนินต่อ\nตัวละครหยุดเดิน",small);
        }
        void DrawReading()
        {
            var r=game.settings.modalRect;Panel(r,"",text);GUI.Label(new Rect(r.x+25,r.y+18,r.width-50,50),game.ReadingTitle,title);
            GUI.Label(new Rect(r.x+28,r.y+90,r.width-56,r.height-175),game.ReadingText,text);
            if(Button(new Rect(r.xMax-215,r.yMax-65,185,40),"อ่านแล้ว [Enter]"))game.CloseModal();
        }
        void DrawDialogue()
        {
            var station=game.DialogueStation;if(station==null){game.CloseModal();return;}var r=game.settings.modalRect;Panel(r,"",text);
            GUI.Label(new Rect(r.x+25,r.y+18,r.width-50,50),station.displayName,title);GUI.Label(new Rect(r.x+28,r.y+82,r.width-56,145),station.text,text);
            var quest=station.quest;
            if(quest!=null)
            {
                string summary=game.GetQuest(quest).completed?quest.completionText:quest.description+"\n"+quest.hint;
                GUI.Label(new Rect(r.x+28,r.y+225,r.width-56,140),summary,text);
                if(!game.GetQuest(quest).completed&&game.QuestReady(quest)&&Button(new Rect(r.x+28,r.yMax-65,240,40),"ส่งของให้พระ (ทดลอง)"))game.TurnIn(quest);
            }
            if(Button(new Rect(r.xMax-215,r.yMax-65,185,40),"จบบทสนทนา"))game.CloseModal();
        }
    }
}
