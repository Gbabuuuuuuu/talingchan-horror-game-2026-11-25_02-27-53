using UnityEngine;
using Talingchan.Sandbox;

namespace Talingchan.Prototype
{
    [RequireComponent(typeof(BoxCollider))]
    public class PrototypeSafeZone:MonoBehaviour
    {
        public string id="safe-house";
        public string displayName="จุดพัก";
        public bool enabledForGameplay=true;
        void OnTriggerEnter(Collider other){Set(other,true);}
        void OnTriggerStay(Collider other){Set(other,true);}
        void OnTriggerExit(Collider other){Set(other,false);}
        void Set(Collider other,bool entered){if(other.GetComponentInParent<SandboxPlayer>()==null)return;var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGame>();if(game!=null)game.SetSafeZone(id,entered&&enabledForGameplay);}
    }
}
