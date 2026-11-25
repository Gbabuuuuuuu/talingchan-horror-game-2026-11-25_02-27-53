using UnityEngine;
using UnityEngine.Events;

namespace Talingchan.Prototype
{
    public interface IPrototypeInteractable{string Prompt(PrototypeGame game);void Interact(PrototypeGame game);}
    public enum PrototypeStationKind{Checkpoint,Note,QuestGiver}
    public class PrototypeStation:MonoBehaviour,IPrototypeInteractable
    {
        public string id;
        public PrototypeStationKind kind;
        public string displayName;
        [TextArea(3,12)]public string text;
        public string grantFlag;
        public PrototypeQuestDefinition quest;
        public Transform checkpointSpawn;
        public UnityEvent onInteract=new UnityEvent();
        public string Prompt(PrototypeGame game)=>game.settings.interactKey+"  "+(kind==PrototypeStationKind.Checkpoint?"บันทึกเกม":kind==PrototypeStationKind.Note?"อ่าน "+displayName:"คุยกับ "+displayName);
        public void Interact(PrototypeGame game)
        {
            onInteract.Invoke();
            if(kind==PrototypeStationKind.Checkpoint)game.SaveCheckpoint(checkpointSpawn!=null?checkpointSpawn:transform);
            else if(kind==PrototypeStationKind.Note)game.OpenReading(displayName,text,grantFlag);
            else game.OpenDialogue(this);
        }
    }
}
