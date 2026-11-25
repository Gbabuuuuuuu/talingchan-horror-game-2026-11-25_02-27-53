using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Talingchan.Prototype
{
    public partial class PrototypeGame
    {
        public void NewRun()
        {
            CancelDeathFlow();
            ReleaseHeld(false);if(chaser!=null)chaser.StopHunt();
            foreach(var pickup in pickups.Values.ToArray())if(pickup!=null&&!pickup.sceneSpawn){pickups.Remove(pickup.persistentId);pickup.gameObject.SetActive(false);Destroy(pickup.gameObject);}
            if(settings.newRunSeed!=0)UnityEngine.Random.InitState(settings.newRunSeed);
            foreach(var p in originalPickups)
            {
                if(p==null)continue;Vector3 position=p.initialPosition;Quaternion rotation=p.initialRotation;
                if(settings.randomizeNewRunLoot&&p.newRunSpawnCandidates!=null&&p.newRunSpawnCandidates.Length>0)
                {var target=p.newRunSpawnCandidates[UnityEngine.Random.Range(0,p.newRunSpawnCandidates.Length)];if(target!=null){position=target.position;rotation=target.rotation;}}
                p.Restore(p.initialAmount,position,rotation,false);
            }
            inventory.Clear();inventory.Resize(settings.inventorySlots);
            if(settings.startWithFlashlightItem&&settings.flashlightItem!=null)inventory.Add(settings.flashlightItem,1);
            flags.Clear();questStates.Clear();
            foreach(var q in settings.quests)if(q!=null)GetQuest(q).started=q.startAutomatically&&QuestAvailable(q);
            foreach(var door in UnityEngine.Object.FindObjectsByType<PrototypeDoor>())door.SetOpen(door.startOpen,true);
            foreach(var haunt in UnityEngine.Object.FindObjectsByType<PrototypeHaunt>())haunt.ResetVisual();
            currentCheckpoint=initialCheckpoint;safeZones.Clear();inSafeZone=false;idleSeconds=0;mode=PrototypeMode.Explore;wheelHeld=false;
            if(initialCheckpoint!=null)Teleport(initialCheckpoint.position,initialCheckpoint.rotation);
            checkpoint=CaptureSnapshot(initialCheckpoint!=null?initialCheckpoint.position:player.transform.position,initialCheckpoint!=null?initialCheckpoint.rotation:player.transform.rotation);
            graceUntil=Time.time+settings.respawnGraceSeconds;ApplyPlayerControls();CaptureCursor();
            if(settings.showIntro)OpenReading(settings.introTitle,settings.introText,"intro-read",true);
            else if(settings.saveAfterIntro)SaveQuickCheckpoint();
        }
        public PrototypeSaveData CaptureSnapshot(Vector3 position,Quaternion rotation)
        {
            var data=new PrototypeSaveData{sceneName=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,playerPosition=position,playerRotation=rotation,selectedSlot=inventory.selectedSlot,flags=new List<string>(flags)};
            foreach(var s in inventory.Slots)data.inventory.Add(new PrototypeSavedSlot{itemId=s.Empty?"":s.item.id,amount=s.Empty?0:s.amount});
            foreach(var p in pickups.Values)
            {
                if(p==null||p.item==null)continue;
                data.pickups.Add(new PrototypeSavedPickup{id=p.persistentId,itemId=p.item.id,amount=p.amount,position=p.held?p.lastSafePosition:p.transform.position,rotation=p.transform.rotation,dynamic=p.physicsObject,sceneSpawn=p.sceneSpawn});
            }
            foreach(var q in questStates)data.quests.Add(new PrototypeQuestRecord{id=q.id,started=q.started,completed=q.completed});
            foreach(var door in UnityEngine.Object.FindObjectsByType<PrototypeDoor>())data.doors.Add(new PrototypeSavedDoor{id=door.id,open=door.isOpen});
            var flash=player.GetComponent<Talingchan.Sandbox.SandboxFlashlight>();if(flash!=null){data.flashlightDrawn=flash.State==Talingchan.Sandbox.SandboxFlashlight.EquipState.Equipped||flash.State==Talingchan.Sandbox.SandboxFlashlight.EquipState.Drawing;data.flashlightPower=flash.powerRequested;}
            return data;
        }
        public bool SaveCheckpoint(Transform point)
        {
            if(IsDead||point==null)return false;if(chaser!=null&&chaser.Active&&!settings.allowSavingDuringChase){Hint("ยังบันทึกไม่ได้ มีคนกำลังตามมา");return false;}
            ReleaseHeld(false);var data=CaptureSnapshot(point.position,point.rotation);
            try
            {
                string path=SavePath;Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(data,true));
                if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
                checkpoint=data;currentCheckpoint=point;lastSaveTime=DateTime.Now.ToString("HH:mm:ss");Hint("บันทึกเกมแล้ว");onSaved.Invoke();return true;
            }
            catch(Exception error){Debug.LogWarning("Checkpoint save failed: "+error.Message,this);Hint("บันทึกไม่สำเร็จ ลองอีกครั้ง");return false;}
        }
        public bool SaveQuickCheckpoint()
        {
            if(settings.allowQuickSaveAnywhere)
            {
                var marker=new GameObject("Temporary checkpoint pose").transform;marker.SetPositionAndRotation(player.transform.position,player.transform.rotation);
                bool saved=SaveCheckpoint(marker);if(currentCheckpoint==marker)currentCheckpoint=initialCheckpoint;Destroy(marker.gameObject);return saved;
            }
            return SaveCheckpoint(currentCheckpoint!=null?currentCheckpoint:initialCheckpoint);
        }
        public bool LoadCheckpoint()
        {
            PrototypeSaveData data=checkpoint;
            if(File.Exists(SavePath))
            {
                try{data=JsonUtility.FromJson<PrototypeSaveData>(File.ReadAllText(SavePath));}
                catch(Exception error){Debug.LogWarning("Checkpoint parse failed: "+error.Message,this);Hint("อ่านไฟล์บันทึกไม่ได้");return false;}
            }
            if(!RestoreSnapshot(data))return false;checkpoint=data;Hint("กลับสู่จุดบันทึกล่าสุด");onLoaded.Invoke();return true;
        }
        public bool RestoreSnapshot(PrototypeSaveData data)
        {
            if(data==null||data.version!=1||data.sceneName!=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name){Hint("ยังไม่มีจุดบันทึกของพื้นที่นี้");return false;}
            if(data.inventory==null||data.pickups==null||data.quests==null||data.flags==null||data.doors==null)return false;
            if(data.pickups.Any(p=>string.IsNullOrEmpty(p.id))||data.pickups.Select(p=>p.id).Distinct().Count()!=data.pickups.Count)return false;
            if(data.inventory.Any(s=>s.amount<0||s.amount>0&&!items.ContainsKey(s.itemId))||data.pickups.Any(p=>p.amount<0||!items.ContainsKey(p.itemId))){Hint("ข้อมูลไอเทมในเซฟไม่ตรงกับชุดทดลอง");return false;}
            CancelDeathFlow();ReleaseHeld(false);if(chaser!=null)chaser.StopHunt();SetHighlight(null);
            var saved=data.pickups.ToDictionary(p=>p.id,p=>p);
            foreach(var pair in pickups.ToArray())
            {
                var p=pair.Value;if(p==null){pickups.Remove(pair.Key);continue;}
                if(saved.TryGetValue(pair.Key,out var state))p.Restore(state.amount,state.position,state.rotation,state.dynamic);
                else if(!p.sceneSpawn){pickups.Remove(pair.Key);p.gameObject.SetActive(false);Destroy(p.gameObject);}
                else p.Restore(p.initialAmount,p.initialPosition,p.initialRotation,false);
            }
            foreach(var state in data.pickups)if(!pickups.TryGetValue(state.id,out var existing)||existing==null)
            {
                var p=SpawnDrop(items[state.itemId],Mathf.Max(1,state.amount),state.position,state.rotation,state.id);if(p!=null){p.sceneSpawn=state.sceneSpawn;p.Restore(state.amount,state.position,state.rotation,state.dynamic);}
            }
            inventory.Clear();inventory.Resize(Mathf.Max(settings.inventorySlots,data.inventory.Count));
            for(int i=0;i<data.inventory.Count;i++){var savedSlot=data.inventory[i];var slot=inventory.Slots[i];if(savedSlot.amount>0){slot.item=items[savedSlot.itemId];slot.amount=savedSlot.amount;}}
            inventory.Resize(settings.inventorySlots,(item,count)=>SpawnDrop(item,count,data.playerPosition+Vector3.up,Quaternion.identity));inventory.selectedSlot=Mathf.Clamp(data.selectedSlot,0,inventory.Slots.Count-1);inventory.NotifyChanged();
            flags=new List<string>(data.flags);questStates=data.quests.Select(q=>new PrototypeQuestRecord{id=q.id,started=q.started,completed=q.completed}).ToList();
            foreach(var q in settings.quests)if(q!=null&&q.startAutomatically&&QuestAvailable(q))GetQuest(q).started=true;
            foreach(var door in UnityEngine.Object.FindObjectsByType<PrototypeDoor>()){var state=data.doors.Find(d=>d.id==door.id);door.SetOpen(state!=null?state.open:door.startOpen,true);}
            foreach(var haunt in UnityEngine.Object.FindObjectsByType<PrototypeHaunt>())haunt.ResetVisual();
            var flashlight=player.GetComponent<Talingchan.Sandbox.SandboxFlashlight>();if(flashlight!=null){flashlight.SetPower(data.flashlightPower);flashlight.SetEquipped(data.flashlightDrawn);}
            DialogueStation=null;readingFlag="";introReading=false;safeZones.Clear();inSafeZone=false;idleSeconds=0;wheelHeld=false;pressed.Clear();mode=PrototypeMode.Explore;
            Teleport(data.playerPosition,data.playerRotation);graceUntil=Time.time+settings.respawnGraceSeconds;ApplyPlayerControls();CaptureCursor();return true;
        }
        public void Teleport(Vector3 position,Quaternion rotation)
        {
            var controller=player.GetComponent<CharacterController>();controller.enabled=false;player.transform.SetPositionAndRotation(position,rotation);controller.enabled=true;player.ResetView();Physics.SyncTransforms();
            // Disabling a CharacterController during teleport may omit OnTriggerExit.
            safeZones.Clear();inSafeZone=false;
            foreach(var zone in UnityEngine.Object.FindObjectsByType<PrototypeSafeZone>())
            {
                var box=zone.GetComponent<BoxCollider>();Vector3 center=position+Vector3.up*controller.height*.5f;
                if(zone.enabledForGameplay&&box.enabled&&(box.ClosestPoint(center)-center).sqrMagnitude<.0001f)SetSafeZone(zone.id,true);
            }
        }
    }
}
