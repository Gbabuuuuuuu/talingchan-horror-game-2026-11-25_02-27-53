using System;
using System.Collections.Generic;
using UnityEngine;

namespace Talingchan.Prototype
{
    [Serializable]public class PrototypeSavedSlot{public string itemId;public int amount;}
    [Serializable]public class PrototypeSavedPickup{public string id,itemId;public int amount;public Vector3 position;public Quaternion rotation;public bool dynamic;public bool sceneSpawn;}
    [Serializable]public class PrototypeQuestRecord{public string id;public bool started,completed;}
    [Serializable]public class PrototypeSavedDoor{public string id;public bool open;}
    [Serializable]public class PrototypeSaveData
    {
        public int version=1;
        public string sceneName;
        public Vector3 playerPosition;
        public Quaternion playerRotation;
        public int selectedSlot;
        public List<PrototypeSavedSlot> inventory=new List<PrototypeSavedSlot>();
        public List<PrototypeSavedPickup> pickups=new List<PrototypeSavedPickup>();
        public List<PrototypeQuestRecord> quests=new List<PrototypeQuestRecord>();
        public List<PrototypeSavedDoor> doors=new List<PrototypeSavedDoor>();
        public List<string> flags=new List<string>();
        public bool flashlightDrawn,flashlightPower;
    }
}
