using UnityEngine;
using Talingchan.Sandbox;

namespace Talingchan.Prototype
{
    [RequireComponent(typeof(BoxCollider))]
    public class PrototypeForbiddenZone:MonoBehaviour
    {
        public bool triggerEnabled=true;
        public float cooldownSeconds=45;
        public string warning="มีเสียงฝีเท้าตามมา...";
        float nextAllowed;
        void OnTriggerEnter(Collider other)
        {
            if(!triggerEnabled||Time.time<nextAllowed||other.GetComponentInParent<SandboxPlayer>()==null)return;
            var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGame>();if(game==null)return;
            nextAllowed=Time.time+cooldownSeconds;game.Hint(warning);game.RequestChaser();
        }
    }
}
