using UnityEngine;
using UnityEngine.AI;

namespace Talingchan.Prototype
{
    public enum PrototypeHuntState { Dormant,Warning,Pursuing,Investigating }
    [RequireComponent(typeof(NavMeshAgent))]
    public class PrototypeChaser:MonoBehaviour
    {
        [Header("Scene references")]
        public PrototypeGame game;
        public NavMeshData navigation;
        public Transform visual;
        public Transform[] spawnPoints;
        public Transform leftLeg,rightLeg,leftArm,rightArm;
        [Header("Temporary chase tuning")]
        public float warningSeconds=3;
        public float movementSpeed=5.5f;
        public float acceleration=15;
        public float angularSpeed=360;
        public float catchDistance=1.05f;
        public float sightDistance=24;
        public float loseSightSeconds=9;
        public float maximumHuntSeconds=40;
        public float investigateSeconds=5;
        public float cooldownSeconds=35;
        public float minimumSpawnDistance=16;
        public float maximumSpawnDistance=32;
        public int spawnAttempts=24;
        public float pathRefreshSeconds=.2f;
        public float doorOpenDistance=2.2f;
        public bool canOpenDoors=true;
        public LayerMask sightMask=~(1<<2);
        public string warningText="มีเสียงฝีเท้าใกล้เข้ามา... รีบหาที่ปลอดภัย";
        [Header("Runtime state")]
        public PrototypeHuntState state;
        public Vector3 investigationPoint;
        public float huntSeconds;
        public float cooldownUntil;
        public bool Active=>state!=PrototypeHuntState.Dormant;
        public NavMeshAgent Agent{get;private set;}
        NavMeshDataInstance meshInstance;
        float phaseTime,lastSeen,pathTime;
        Vector3 lastKnown;
        void Awake(){Agent=GetComponent<NavMeshAgent>();Agent.enabled=false;if(navigation!=null)meshInstance=NavMesh.AddNavMeshData(navigation);if(visual!=null)visual.gameObject.SetActive(false);}
        void Start(){if(game==null)game=Object.FindAnyObjectByType<PrototypeGame>();if(game!=null)game.Noise+=HearNoise;}
        void OnDestroy(){if(game!=null)game.Noise-=HearNoise;if(meshInstance.valid)meshInstance.Remove();}
        public bool RequestHunt(bool force=false)
        {
            if(game==null||Active||!force&&Time.time<cooldownUntil)return false;
            if(!TrySpawn(out var position)){game.Hint("จุดเกิดคนไล่ยังหาเส้นทางไม่เจอ");return false;}
            transform.position=position;Agent.enabled=true;if(!Agent.isOnNavMesh){Agent.enabled=false;return false;}
            Agent.Warp(position);Agent.speed=movementSpeed;Agent.acceleration=acceleration;Agent.angularSpeed=angularSpeed;Agent.stoppingDistance=.2f;
            Agent.isStopped=true;state=PrototypeHuntState.Warning;phaseTime=huntSeconds=0;lastSeen=Time.time;lastKnown=game.player.transform.position;
            game.Hint(warningText);return true;
        }
        bool TrySpawn(out Vector3 result)
        {
            var player=game.player.transform.position;var path=new NavMeshPath();
            int start=spawnPoints!=null&&spawnPoints.Length>0?Random.Range(0,spawnPoints.Length):0;
            for(int i=0;i<spawnAttempts;i++)
            {
                Vector3 candidate;
                if(spawnPoints!=null&&i<spawnPoints.Length)candidate=spawnPoints[(start+i)%spawnPoints.Length].position;
                else {float angle=Random.value*Mathf.PI*2;candidate=player+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*Random.Range(minimumSpawnDistance,maximumSpawnDistance);}
                float distance=Vector3.Distance(candidate,player);if(distance<minimumSpawnDistance||distance>maximumSpawnDistance)continue;
                if(NavMesh.SamplePosition(candidate,out var hit,3,NavMesh.AllAreas)&&NavMesh.CalculatePath(hit.position,player,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete){result=hit.position;return true;}
            }
            result=Vector3.zero;return false;
        }
        void Update()
        {
            if(!Active||game==null)return;
            if(game.IsDead||(game.inSafeZone&&game.settings.respectSafeZones)){StopHunt();return;}
            phaseTime+=Time.deltaTime;huntSeconds+=Time.deltaTime;
            if(state==PrototypeHuntState.Warning)
            {
                if(phaseTime<warningSeconds)return;
                state=PrototypeHuntState.Pursuing;phaseTime=0;Agent.isStopped=false;if(visual!=null)visual.gameObject.SetActive(true);
            }
            if(!Agent.enabled||!Agent.isOnNavMesh){StopHunt();return;}
            var target=game.player.transform.position;float distance=Vector3.Distance(transform.position,target);
            bool visible=distance<sightDistance&&CanSee(target);
            if(visible&&state!=PrototypeHuntState.Investigating){lastSeen=Time.time;lastKnown=target;}
            if(state==PrototypeHuntState.Investigating&&phaseTime>=investigateSeconds){state=PrototypeHuntState.Pursuing;phaseTime=0;}
            if(distance<catchDistance&&CanSee(target)){game.Die();return;}
            if(huntSeconds>maximumHuntSeconds||state==PrototypeHuntState.Pursuing&&Time.time-lastSeen>loseSightSeconds){StopHunt();return;}
            if(Time.time>=pathTime)
            {
                pathTime=Time.time+pathRefreshSeconds;Vector3 destination=state==PrototypeHuntState.Investigating?investigationPoint:lastKnown;
                if(NavMesh.SamplePosition(destination,out var hit,3,NavMesh.AllAreas))Agent.SetDestination(hit.position);
                if(canOpenDoors)foreach(var door in Object.FindObjectsByType<PrototypeDoor>())if(!door.isOpen&&Vector3.Distance(door.transform.position,transform.position)<doorOpenDistance)door.SetOpen(true);
            }
            float gait=Mathf.Sin(Time.time*11)*28*Mathf.Clamp01(Agent.velocity.magnitude/2);
            if(leftLeg!=null)leftLeg.localRotation=Quaternion.Euler(gait,0,0);if(rightLeg!=null)rightLeg.localRotation=Quaternion.Euler(-gait,0,0);
            if(leftArm!=null)leftArm.localRotation=Quaternion.Euler(-gait,0,8);if(rightArm!=null)rightArm.localRotation=Quaternion.Euler(-35+gait*.3f,0,-10);
        }
        bool CanSee(Vector3 target)
        {
            Vector3 from=transform.position+Vector3.up*1.3f,to=target+Vector3.up*1.1f;
            if(!Physics.Linecast(from,to,out var hit,sightMask,QueryTriggerInteraction.Ignore))return true;
            return hit.collider.GetComponentInParent<Talingchan.Sandbox.SandboxPlayer>()!=null;
        }
        public void HearNoise(Vector3 position,float radius)
        {
            if(!Active||state==PrototypeHuntState.Warning||Vector3.Distance(transform.position,position)>radius)return;
            investigationPoint=position;state=PrototypeHuntState.Investigating;phaseTime=0;lastSeen=Time.time;
        }
        public void StopHunt()
        {
            if(Active)cooldownUntil=Time.time+cooldownSeconds;
            state=PrototypeHuntState.Dormant;huntSeconds=0;if(Agent!=null&&Agent.enabled){if(Agent.isOnNavMesh)Agent.ResetPath();Agent.enabled=false;}
            if(visual!=null)visual.gameObject.SetActive(false);
        }
    }
}
