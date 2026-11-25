using System;
using System.Collections.Generic;
using UnityEngine;

namespace Talingchan.Prototype
{
    [Serializable]public class PrototypeSlot
    {
        public PrototypeItemDefinition item;
        public int amount;
        public bool Empty=>item==null||amount<=0;
        public void Clear(){item=null;amount=0;}
    }
    public class PrototypeInventory : MonoBehaviour
    {
        [SerializeField]List<PrototypeSlot> slots=new List<PrototypeSlot>();
        public int selectedSlot;
        public IReadOnlyList<PrototypeSlot> Slots=>slots;
        public event Action Changed;
        public void Resize(int capacity,Action<PrototypeItemDefinition,int> overflow=null)
        {
            capacity=Mathf.Clamp(capacity,1,40);
            while(slots.Count>capacity){var last=slots[slots.Count-1];if(!last.Empty){if(overflow==null)return;overflow(last.item,last.amount);}slots.RemoveAt(slots.Count-1);}
            while(slots.Count<capacity)slots.Add(new PrototypeSlot());
            selectedSlot=Mathf.Clamp(selectedSlot,0,slots.Count-1);
        }
        public int Count(PrototypeItemDefinition item){int count=0;foreach(var s in slots)if(!s.Empty&&s.item==item)count+=s.amount;return count;}
        public int Add(PrototypeItemDefinition item,int amount)
        {
            if(item==null||amount<=0)return 0;int left=amount,max=Mathf.Max(1,item.maxStack);
            foreach(var s in slots){if(s.Empty||s.item!=item)continue;int n=Mathf.Min(left,Mathf.Max(0,max-s.amount));s.amount+=n;left-=n;if(left==0)break;}
            foreach(var s in slots){if(left==0)break;if(!s.Empty)continue;int n=Mathf.Min(left,max);s.item=item;s.amount=n;left-=n;}
            if(left!=amount)Changed?.Invoke();return amount-left;
        }
        public bool Remove(PrototypeItemDefinition item,int amount)
        {
            if(amount<0||Count(item)<amount)return false;int left=amount;
            for(int i=slots.Count-1;i>=0&&left>0;i--){var s=slots[i];if(s.Empty||s.item!=item)continue;int n=Mathf.Min(left,s.amount);s.amount-=n;left-=n;if(s.amount==0)s.Clear();}
            Changed?.Invoke();return true;
        }
        public bool RemoveFromSlot(int index,int amount)
        {
            if(index<0||index>=slots.Count||amount<=0||slots[index].Empty||slots[index].amount<amount)return false;
            slots[index].amount-=amount;if(slots[index].amount==0)slots[index].Clear();Changed?.Invoke();return true;
        }
        public PrototypeSlot Selected=>slots.Count==0?null:slots[Mathf.Clamp(selectedSlot,0,slots.Count-1)];
        public void Clear(){foreach(var s in slots)s.Clear();selectedSlot=0;Changed?.Invoke();}
        public void NotifyChanged(){Changed?.Invoke();}
    }
}
