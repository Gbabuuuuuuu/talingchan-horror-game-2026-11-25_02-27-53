using System;
using UnityEngine;

namespace Talingchan.Prototype
{
    public class PrototypePickup:MonoBehaviour,IPrototypeInteractable
    {
        public string persistentId;
        public PrototypeItemDefinition item;
        [Min(0)]public int amount=1;
        public bool sceneSpawn=true;
        public Transform[] newRunSpawnCandidates;
        public float impactNoiseRadius=14;
        public float minimumImpactSpeed=1.1f;
        public float impactCooldown=.6f;
        [SerializeField]public Vector3 lastSafePosition;
        [SerializeField]public bool held;
        [SerializeField]public bool physicsObject;
        [NonSerialized]public Vector3 initialPosition;
        [NonSerialized]public Quaternion initialRotation;
        [NonSerialized]public int initialAmount;
        public Rigidbody Body{get;private set;}
        public Collider Shape{get;private set;}
        PrototypeGame game;float nextNoise;
        void Awake(){Body=GetComponent<Rigidbody>();Shape=GetComponent<Collider>();initialPosition=lastSafePosition=transform.position;initialRotation=transform.rotation;initialAmount=amount;}
        void Start(){game=UnityEngine.Object.FindAnyObjectByType<PrototypeGame>();if(game!=null)game.RegisterPickup(this);}
        public void Initialize(PrototypeGame owner,PrototypeItemDefinition definition,int quantity,string id,bool source)
        {
            game=owner;item=definition;amount=quantity;persistentId=id;sceneSpawn=source;lastSafePosition=transform.position;
            if(Body==null)Body=GetComponent<Rigidbody>();if(Shape==null)Shape=GetComponent<Collider>();if(owner!=null)owner.RegisterPickup(this);
        }
        public string Prompt(PrototypeGame owner)=>owner.settings.interactKey+"  เก็บ "+(item!=null?item.displayName:"ของ")+" ×"+amount+(item!=null&&item.canCarry?"  |  คลิกขวา ถือ":"");
        public void Interact(PrototypeGame owner){owner.Collect(this);}
        void Update()
        {
            if(game==null||held||!gameObject.activeInHierarchy)return;
            if(transform.position.y<game.settings.itemRecoveryY){if(Body!=null){Body.linearVelocity=Vector3.zero;Body.angularVelocity=Vector3.zero;}transform.position=lastSafePosition;}
            else if(Body!=null&&!Body.isKinematic&&Body.linearVelocity.sqrMagnitude<.05f&&Physics.Raycast(transform.position+Vector3.up*.15f,Vector3.down,.7f,~(1<<2),QueryTriggerInteraction.Ignore))lastSafePosition=transform.position;
        }
        void OnCollisionEnter(Collision collision)
        {
            if(game==null||held||Time.time<nextNoise||collision.relativeVelocity.magnitude<minimumImpactSpeed)return;
            nextNoise=Time.time+impactCooldown;game.EmitNoise(transform.position,impactNoiseRadius);game.audioSystem.At(game.audioSystem.impact,transform.position,.22f,UnityEngine.Random.Range(.8f,1.15f));
        }
        public void Restore(int quantity,Vector3 position,Quaternion rotation,bool dynamic)
        {
            held=false;amount=quantity;transform.SetParent(null,true);transform.SetPositionAndRotation(position,rotation);lastSafePosition=position;
            if(Shape==null)Shape=GetComponent<Collider>();if(Body==null)Body=GetComponent<Rigidbody>();
            if(Shape!=null)Shape.enabled=quantity>0;
            if(Body!=null){if(!Body.isKinematic){Body.linearVelocity=Vector3.zero;Body.angularVelocity=Vector3.zero;}Body.isKinematic=!dynamic;}
            physicsObject=dynamic;gameObject.SetActive(quantity>0);
        }
    }
}
