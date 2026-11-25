using UnityEngine;
using UnityEngine.Events;
using Talingchan.Sandbox;

namespace Talingchan.Prototype
{
    [RequireComponent(typeof(BoxCollider))]
    public class PrototypeHaunt:MonoBehaviour
    {
        public string persistentId="abandoned-house-shadow";
        public bool eventEnabled=true;
        public bool oncePerCheckpoint=true;
        public float delay=1.2f;
        public float duration=3.5f;
        public GameObject apparition;
        public AudioClip sound;
        [Range(0,1)]public float volume=.18f;
        public float pitch=.65f;
        public UnityEvent onAppear=new UnityEvent();
        [SerializeField]public bool running;
        float timer;PrototypeGame game;
        void Start(){game=UnityEngine.Object.FindAnyObjectByType<PrototypeGame>();if(apparition!=null)apparition.SetActive(false);}
        void OnTriggerEnter(Collider other){if(other.GetComponentInParent<SandboxPlayer>()!=null)Trigger();}
        public void Trigger()
        {
            if(game==null)game=UnityEngine.Object.FindAnyObjectByType<PrototypeGame>();
            if(!eventEnabled||running||game==null||oncePerCheckpoint&&game.HasFlag(persistentId))return;
            game.SetFlag(persistentId);running=true;timer=-delay;
        }
        void Update()
        {
            if(!running)return;float old=timer;timer+=Time.deltaTime;
            if(old<0&&timer>=0){if(apparition!=null)apparition.SetActive(true);game.audioSystem.At(sound!=null?sound:game.audioSystem.ghostCue,apparition!=null?apparition.transform.position:transform.position,volume,pitch);onAppear.Invoke();}
            if(timer>=duration)ResetVisual();
        }
        public void ResetVisual(){running=false;timer=0;if(apparition!=null)apparition.SetActive(false);}
    }
}
