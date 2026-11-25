using UnityEngine;

namespace Talingchan.Prototype
{
    public class PrototypeAudio : MonoBehaviour
    {
        public AudioClip ambience;
        public AudioClip[] footsteps;
        public AudioClip pickup,door,impact,ghostCue;
        [Range(0,1)]public float ambienceVolume=.12f;
        [Range(0,1)]public float footstepsVolume=.22f;
        [Range(0,1)]public float pickupVolume=.25f;
        [Range(0,1)]public float eventVolume=.25f;
        public Vector2 footstepPitch=new Vector2(.9f,1.08f);
        public float walkInterval=.48f,runInterval=.29f;
        public float soundDistance=16;
        public float movementThreshold=.2f;
        AudioSource ambienceSource,foley;CharacterController body;PrototypeGame game;float nextStep;
        void Start()
        {
            game=GetComponent<PrototypeGame>();body=game.player.GetComponent<CharacterController>();
            ambienceSource=gameObject.AddComponent<AudioSource>();ambienceSource.clip=ambience;ambienceSource.loop=true;ambienceSource.spatialBlend=0;ambienceSource.volume=ambienceVolume;if(ambience!=null)ambienceSource.Play();
            foley=game.player.gameObject.AddComponent<AudioSource>();foley.spatialBlend=0;foley.playOnAwake=false;
        }
        void Update()
        {
            if(ambienceSource!=null)ambienceSource.volume=ambienceVolume;
            if(body==null||foley==null||game==null||game.IsDead)return;
            float speed=new Vector2(body.velocity.x,body.velocity.z).magnitude;
            if(!body.isGrounded||speed<movementThreshold||Time.time<nextStep||footsteps==null||footsteps.Length==0)return;
            nextStep=Time.time+(speed>game.player.walkSpeed+.5f?runInterval:walkInterval);foley.pitch=Random.Range(footstepPitch.x,footstepPitch.y);foley.PlayOneShot(footsteps[Random.Range(0,footsteps.Length)],footstepsVolume);
        }
        public void Pickup(){if(foley!=null&&pickup!=null){foley.pitch=1;foley.PlayOneShot(pickup,pickupVolume);}}
        public void At(AudioClip clip,Vector3 position,float volume=-1,float pitch=1)
        {
            if(clip==null)return;var go=new GameObject("Spatial sound");go.transform.position=position;var source=go.AddComponent<AudioSource>();source.clip=clip;source.spatialBlend=1;source.minDistance=1;source.maxDistance=soundDistance;source.rolloffMode=AudioRolloffMode.Linear;source.volume=volume<0?eventVolume:volume;source.pitch=pitch;source.Play();Destroy(go,clip.length/Mathf.Max(.1f,Mathf.Abs(pitch))+.25f);
        }
    }
}
