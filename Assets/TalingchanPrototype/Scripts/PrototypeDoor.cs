using UnityEngine;

namespace Talingchan.Prototype
{
    public class PrototypeDoor:MonoBehaviour,IPrototypeInteractable
    {
        public string id;
        public string displayName="ประตู";
        public Quaternion closedRotation=Quaternion.identity;
        public float openAngle=-82;
        public float animationSpeed=4;
        public bool startOpen;
        [SerializeField]public bool isOpen;
        [SerializeField,Range(0,1)]public float openAmount;
        PrototypeGame game;
        void Awake(){SetOpen(startOpen,true);}
        void Start(){game=UnityEngine.Object.FindAnyObjectByType<PrototypeGame>();}
        void Update(){openAmount=Mathf.MoveTowards(openAmount,isOpen?1:0,Time.deltaTime*animationSpeed);transform.localRotation=Quaternion.Slerp(closedRotation,closedRotation*Quaternion.Euler(0,openAngle,0),Mathf.SmoothStep(0,1,openAmount));}
        public string Prompt(PrototypeGame owner)=>owner.settings.interactKey+"  "+(isOpen?"ปิด ":"เปิด ")+displayName;
        public void Interact(PrototypeGame owner){SetOpen(!isOpen);}
        public void SetOpen(bool open,bool immediate=false)
        {
            bool changed=isOpen!=open;isOpen=open;
            if(immediate){openAmount=open?1:0;transform.localRotation=closedRotation*Quaternion.Euler(0,open?openAngle:0,0);}
            else if(changed&&game!=null)game.audioSystem.At(game.audioSystem.door,transform.position,.18f,.8f);
        }
    }
}
