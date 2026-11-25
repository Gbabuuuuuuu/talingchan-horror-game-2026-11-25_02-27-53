using UnityEngine;

namespace Talingchan.Prototype
{
    public enum PrototypeItemUse { Material, Readable, Flashlight, Throwable }
    [CreateAssetMenu(menuName="Talingchan Prototype/Item")]
    public class PrototypeItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2,6)] public string description;
        [Min(1)] public int maxStack=10;
        public bool canDrop=true;
        public bool canCarry=true;
        public bool canThrow=true;
        public PrototypeItemUse use;
        public Color color=Color.white;
        public Texture2D icon;
        public GameObject worldPrefab;
        public float worldMass=.4f;
        void OnValidate(){maxStack=Mathf.Max(1,maxStack);worldMass=Mathf.Max(.02f,worldMass);}
    }
}
