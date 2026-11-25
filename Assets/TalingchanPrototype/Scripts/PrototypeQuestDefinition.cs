using System;
using UnityEngine;

namespace Talingchan.Prototype
{
    [Serializable] public class PrototypeItemAmount
    {
        public PrototypeItemDefinition item;
        [Min(1)] public int amount=1;
    }
    [CreateAssetMenu(menuName="Talingchan Prototype/Quest")]
    public class PrototypeQuestDefinition : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea(2,8)]public string description;
        [TextArea(2,6)]public string hint;
        public bool startAutomatically=true;
        public bool requiredForFinale=true;
        public PrototypeQuestDefinition[] prerequisites=Array.Empty<PrototypeQuestDefinition>();
        public PrototypeItemAmount[] requiredItems=Array.Empty<PrototypeItemAmount>();
        public string[] requiredFlags=Array.Empty<string>();
        public string[] requiredFlagLabels=Array.Empty<string>();
        public bool consumeItemsOnTurnIn=true;
        public PrototypeItemAmount[] rewards=Array.Empty<PrototypeItemAmount>();
        [TextArea]public string completionText;
    }
}
