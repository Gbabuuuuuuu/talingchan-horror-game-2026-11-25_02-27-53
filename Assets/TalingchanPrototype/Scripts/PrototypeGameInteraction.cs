using System;
using System.Collections;
using UnityEngine;

namespace Talingchan.Prototype
{
    public partial class PrototypeGame
    {
        void UpdateFocus()
        {
            focus=null;FocusPrompt="";
            if(heldItem!=null){FocusPrompt=settings.interactKey+" เก็บเข้ากระเป๋า  |  คลิกขวา วาง  |  คลิกซ้าย ปา";SetHighlight(null);return;}
            if(Physics.Raycast(player.view.position,player.view.forward,out var hit,settings.interactionDistance,settings.interactionMask,QueryTriggerInteraction.Ignore))
            {
                foreach(var behaviour in hit.collider.GetComponentsInParent<MonoBehaviour>())if(behaviour is IPrototypeInteractable candidate){focus=candidate;break;}
            }
            if(focus!=null)FocusPrompt=focus.Prompt(this);SetHighlight(focus as PrototypePickup);
        }
        void SetHighlight(PrototypePickup pickup)
        {
            if(highlighted==pickup)return;
            if(highlighted!=null)foreach(var renderer in highlighted.GetComponentsInChildren<Renderer>())renderer.SetPropertyBlock(null);
            highlighted=pickup;
            if(pickup!=null&&settings.highlightFocusedItems){highlightBlock.SetColor("_EmissionColor",new Color(.22f,.18f,.05f));foreach(var renderer in pickup.GetComponentsInChildren<Renderer>())renderer.SetPropertyBlock(highlightBlock);}
        }
        public void RegisterPickup(PrototypePickup pickup)
        {
            if(pickup==null)return;if(string.IsNullOrEmpty(pickup.persistentId))pickup.persistentId=Guid.NewGuid().ToString("N");
            if(pickups.TryGetValue(pickup.persistentId,out var other)&&other!=null&&other!=pickup){Debug.LogError("Duplicate pickup ID: "+pickup.persistentId,pickup);return;}
            pickups[pickup.persistentId]=pickup;
        }
        public int Collect(PrototypePickup pickup)
        {
            if(pickup==null||pickup.item==null||pickup.amount<=0)return 0;
            int accepted=inventory.Add(pickup.item,pickup.amount);
            if(accepted==0){Hint("กระเป๋าเต็ม เลือกทิ้งของบางอย่างก่อน");return 0;}
            pickup.amount-=accepted;audioSystem.Pickup();Hint("เก็บ "+pickup.item.displayName+" ×"+accepted);
            if(pickup.amount==0){if(heldItem==pickup)ReleaseHeld(false);SetHighlight(null);pickup.gameObject.SetActive(false);}
            return accepted;
        }
        public Vector3 DropPosition()
        {
            Vector3 origin=player.view.position,direction=player.view.forward;
            if(Physics.Raycast(origin,direction,out var hit,settings.dropDistance,settings.interactionMask,QueryTriggerInteraction.Ignore))return hit.point-direction*.22f;
            return origin+direction*settings.dropDistance;
        }
        public PrototypePickup SpawnDrop(PrototypeItemDefinition item,int amount,Vector3 position,Quaternion rotation,string id=null)
        {
            if(item==null||item.worldPrefab==null||amount<=0)return null;
            var go=Instantiate(item.worldPrefab,position,rotation);go.name=item.displayName+" (dropped)";go.isStatic=false;
            var pickup=go.GetComponent<PrototypePickup>();pickup.Initialize(this,item,amount,id??Guid.NewGuid().ToString("N"),false);
            pickup.physicsObject=true;go.SetActive(true);
            if(pickup.Body!=null){pickup.Body.isKinematic=false;pickup.Body.mass=item.worldMass;}
            if(pickup.Shape!=null)StartCoroutine(IgnorePlayerBriefly(pickup.Shape));
            return pickup;
        }
        IEnumerator IgnorePlayerBriefly(Collider shape)
        {
            var controller=player.GetComponent<CharacterController>();if(shape==null)yield break;Physics.IgnoreCollision(shape,controller,true);yield return new WaitForSeconds(.5f);if(shape!=null&&controller!=null)Physics.IgnoreCollision(shape,controller,false);
        }
        public bool DropSelected(int requested=1)
        {
            var selected=inventory.Selected;if(selected==null||selected.Empty)return false;
            if(!selected.item.canDrop){Hint("ของชิ้นนี้ยังจำเป็นต้องเก็บไว้");return false;}
            int amount=Mathf.Clamp(requested,1,selected.amount);var item=selected.item;
            var drop=SpawnDrop(item,amount,DropPosition(),Quaternion.identity);if(drop==null){Hint("ยังไม่มีตัวแทนของสำหรับวางในโลก");return false;}
            inventory.RemoveFromSlot(inventory.selectedSlot,amount);Hint("วาง "+item.displayName+" ×"+amount);return true;
        }
        public void UseSelected()
        {
            var slot=inventory.Selected;if(slot==null||slot.Empty)return;
            switch(slot.item.use)
            {
                case PrototypeItemUse.Flashlight:player.GetComponent<Talingchan.Sandbox.SandboxFlashlight>().ToggleEquipped();break;
                case PrototypeItemUse.Readable:OpenReading(slot.item.displayName,slot.item.description);break;
                case PrototypeItemUse.Throwable:
                    var item=slot.item;if(!item.canThrow)return;
                    var drop=SpawnDrop(item,1,DropPosition(),Quaternion.identity);if(drop==null)return;
                    inventory.RemoveFromSlot(inventory.selectedSlot,1);if(drop.Body!=null)drop.Body.linearVelocity=player.view.forward*settings.throwSpeed;break;
                default:Hint(slot.item.description);break;
            }
        }
        public bool ToggleCarry()
        {
            if(heldItem!=null){ReleaseHeld(false);return true;}
            if(!(focus is PrototypePickup pickup)||pickup.item==null||!pickup.item.canCarry)return false;
            heldItem=pickup;heldItem.held=true;SetHighlight(null);
            heldItem.lastSafePosition=heldItem.transform.position;
            if(heldItem.Body!=null){if(!heldItem.Body.isKinematic){heldItem.Body.linearVelocity=Vector3.zero;heldItem.Body.angularVelocity=Vector3.zero;}heldItem.Body.isKinematic=true;}
            if(heldItem.Shape!=null)heldItem.Shape.enabled=false;
            heldItem.transform.SetParent(player.view,true);return true;
        }
        public bool ReleaseHeld(bool throwing)
        {
            if(heldItem==null)return false;var pickup=heldItem;heldItem=null;
            if(throwing&&pickup.item!=null&&!pickup.item.canThrow)throwing=false;
            pickup.held=false;pickup.transform.SetParent(null,true);pickup.transform.position=DropPosition();pickup.lastSafePosition=pickup.transform.position;pickup.physicsObject=true;
            if(pickup.Shape!=null){pickup.Shape.enabled=true;StartCoroutine(IgnorePlayerBriefly(pickup.Shape));}
            if(pickup.Body!=null){pickup.Body.isKinematic=false;pickup.Body.linearVelocity=throwing?player.view.forward*settings.throwSpeed:Vector3.zero;}
            return true;
        }
    }
}
