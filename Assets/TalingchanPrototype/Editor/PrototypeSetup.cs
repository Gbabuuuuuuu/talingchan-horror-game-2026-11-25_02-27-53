#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Talingchan.Sandbox;

namespace Talingchan.Prototype.Editor
{
    public static class PrototypeSetup
    {
        const string Root="Assets/TalingchanPrototype";
        static Transform systems,content;
        static PrototypeGame game;
        static PrototypeSettings settings;
        static Material wood,cloth,gold,metal,dark,skin,robe,paper;
        [MenuItem("Talingchan/Build first gameplay prototype")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before building.");
            foreach(string folder in new[]{"Data/Items","Data/Quests","Prefabs","Materials","Scenes","Navigation"})Directory.CreateDirectory(Root+"/"+folder);
            AssetDatabase.Refresh();
            string scenePath=Root+"/Scenes/GameplayPrototype.unity";
            // Always derive the prototype from the existing village; the source scene stays intact.
            var original=EditorSceneManager.OpenScene("Assets/TalingchanVillage/Scenes/BanRimKhlong.unity",OpenSceneMode.Single);
            EditorSceneManager.SaveScene(original,scenePath,true);
            EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
            foreach(var old in UnityEngine.Object.FindObjectsByType<PrototypeGame>())UnityEngine.Object.DestroyImmediate(old.gameObject);
            systems=new GameObject("PROTOTYPE SYSTEMS - Inspector settings here").transform;
            content=new GameObject("PROTOTYPE CONTENT - editable quest objects").transform;
            game=systems.gameObject.AddComponent<PrototypeGame>();game.inventory=systems.gameObject.AddComponent<PrototypeInventory>();game.audioSystem=systems.gameObject.AddComponent<PrototypeAudio>();
            settings=Asset<PrototypeSettings>(Root+"/Data/GameplaySettings.asset");game.settings=settings;
            wood=Mat("Wood scraps",new Color(.28f,.16f,.075f));cloth=Mat("Cloth",new Color(.65f,.55f,.38f));gold=Mat("Amulet fragments",new Color(.48f,.35f,.12f));metal=Mat("Iron",new Color(.19f,.21f,.23f),.6f);dark=Mat("Apparition",new Color(.018f,.022f,.024f));skin=Mat("Skin",new Color(.5f,.37f,.3f));robe=Mat("Monk robe",new Color(.52f,.20f,.045f));paper=Mat("Note paper",new Color(.68f,.61f,.42f));
            var woodItem=Item("wood","เศษไม้",PrototypeItemUse.Material,10,"วัสดุทดลองสำหรับเควสบ้านร้าง",wood,new Vector3(.34f,.07f,.13f));
            var clothItem=Item("cloth","ผ้าเก่า",PrototypeItemUse.Material,10,"ผ้าที่พบในบ้านร้าง เก็บไว้ส่งให้พระ",cloth,new Vector3(.24f,.045f,.2f));
            var amuletItem=Item("amulet-fragment","เศษพระ",PrototypeItemUse.Material,5,"ชิ้นส่วนพระแตก ใช้ทดสอบการรวบรวมของ",gold,new Vector3(.12f,.12f,.09f));
            var stone=Item("stone","ก้อนหิน",PrototypeItemUse.Throwable,10,"ปาให้เกิดเสียงเพื่อล่อคนไล่ไปตรวจสอบ",metal,new Vector3(.13f,.1f,.12f));
            var bottle=Item("bottle","ขวดเก่า",PrototypeItemUse.Throwable,5,"ของทดลองสำหรับถือ วาง และปาล่อเสียง",Mat("Bottle",new Color(.12f,.23f,.13f)),new Vector3(.08f,.25f,.08f));
            var flashlight=Item("flashlight","ไฟฉาย",PrototypeItemUse.Flashlight,1,"X หรือ F หยิบ/เก็บ • G เปิด/ปิด",metal,new Vector3(.07f,.07f,.25f));flashlight.canDrop=false;flashlight.canCarry=false;flashlight.canThrow=false;
            settings.items=new[]{flashlight,woodItem,clothItem,amuletItem,stone,bottle};settings.flashlightItem=flashlight;settings.inventorySlots=10;settings.afkSeconds=120;settings.uiFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/TalingchanVillage/Sources/Fonts/NotoSansThai.ttf");
            settings.introText="คืนนี้ทางกลับบ้านดูไม่เหมือนเดิม...\n\nรอบทดลอง: ไปสำรวจบ้านร้างหลังที่สองริมถนน อ่านบันทึก และเก็บเศษไม้ 3 ผ้าเก่า 2 เศษพระ 1 ชิ้น จากนั้นนำไปให้พระที่วัดทางเหนือ\n\nบ้านหลังแรกเป็นจุดพักและจุดบันทึกชั่วคราว กด E ที่สมุดบันทึกเกม\n\nหากถูกจับ จะกลับสู่จุดบันทึกล่าสุด พร้อมสถานะของและเควส ณ ตอนที่บันทึก";
            settings.questRect=new Rect(910,55,350,300);
            var quest=Asset<PrototypeQuestDefinition>(Root+"/Data/Quests/AbandonedHouse.asset");quest.id="abandoned-house";quest.title="รวบรวมของจากบ้านร้าง";quest.description="อ่านบันทึกและเก็บของจากบ้านร้าง ก่อนนำไปให้พระที่วัดทางเหนือ";quest.hint="บ้านร้างหลังที่สองริมถนน: ประตูไม้ หน้าบ้านมีแสงสีเหลือง";quest.requiredItems=new[]{Amount(woodItem,3),Amount(clothItem,2),Amount(amuletItem,1)};quest.requiredFlags=new[]{"abandoned-note-read"};quest.prerequisites=Array.Empty<PrototypeQuestDefinition>();quest.rewards=Array.Empty<PrototypeItemAmount>();quest.completionText="ส่งของครบแล้ว — จบรอบทดลองระบบ เควสและสูตรจริงจะเพิ่มภายหลัง";quest.requiredFlagLabels=new[]{"อ่านบันทึกในบ้านร้าง"};settings.quests=new[]{quest};
            game.player=UnityEngine.Object.FindAnyObjectByType<SandboxPlayer>();var player=game.player;
            player.thirdPerson=false;player.allowViewSwitch=false;player.useBuiltInRespawn=false;player.showLegacyHUD=false;player.externalCursorManaged=true;player.GetComponent<SandboxFlashlight>().showHUD=false;
            var phone=player.GetComponent<SandboxPhone>();if(phone!=null)phone.enabled=false;
            var house1=GameObject.Find("บ้าน 1").transform;var house2=GameObject.Find("บ้าน 2").transform;
            game.initialCheckpoint=Marker("Checkpoint spawn - change this later",house1.TransformPoint(new Vector3(0,.3f,-3.2f)));game.initialCheckpoint.rotation=Quaternion.Euler(0,90,0);
            player.transform.SetPositionAndRotation(game.initialCheckpoint.position,game.initialCheckpoint.rotation);
            var save=Station("Checkpoint ledger",house1.TransformPoint(new Vector3(2.3f,1.09f,-.3f)),PrototypeStationKind.Checkpoint,"สมุดบันทึกเกม");save.checkpointSpawn=game.initialCheckpoint;save.transform.rotation=house1.rotation;
            var safe=new GameObject("Safe zone - temporary house 1");safe.transform.SetParent(content);safe.transform.SetPositionAndRotation(house1.TransformPoint(new Vector3(0,1.5f,0)),house1.rotation);var safeBox=safe.AddComponent<BoxCollider>();safeBox.isTrigger=true;safeBox.size=new Vector3(9,3,10);safe.AddComponent<PrototypeSafeZone>();
            var note=Station("Abandoned house note",house2.TransformPoint(new Vector3(2.3f,1.09f,-.3f)),PrototypeStationKind.Note,"บันทึกที่ทิ้งไว้");note.grantFlag="abandoned-note-read";note.text="ถ้าทางออกหายไป อย่าเดินวนอยู่กับที่...\n\nเก็บเศษไม้ 3 ผ้าเก่า 2 และเศษพระ 1 ชิ้น นำไปให้หลวงพ่อที่วัดทางเหนือ\n\nของที่ยังหาไม่ครบกลับมาเก็บทีหลังได้\n\n[ข้อความและสูตรชั่วคราวสำหรับทดสอบระบบ]";
            Source(woodItem,2,"house2-wood-a",house2.TransformPoint(new Vector3(1,.3f,-2)));Source(woodItem,1,"house2-wood-b",house2.TransformPoint(new Vector3(-1,.3f,.2f)));
            Source(clothItem,1,"house2-cloth-a",house2.TransformPoint(new Vector3(2,.29f,1)));Source(clothItem,1,"house2-cloth-b",house2.TransformPoint(new Vector3(-3.2f,.81f,2.5f)));Source(amuletItem,1,"house2-fragment",house2.TransformPoint(new Vector3(1.5f,.3f,3.2f)));
            Source(stone,4,"road-stones",new Vector3(-5,.16f,-23));Source(bottle,2,"house1-bottles",house1.TransformPoint(new Vector3(-2,.4f,-2)));
            var monk=Station("Monk - temporary quest NPC",new Vector3(5,.8f,157),PrototypeStationKind.QuestGiver,"หลวงพ่อ");UnityEngine.Object.DestroyImmediate(monk.GetComponent<MeshRenderer>());UnityEngine.Object.DestroyImmediate(monk.GetComponent<MeshFilter>());var monkBox=monk.GetComponent<BoxCollider>();monkBox.center=Vector3.up*.8f;monkBox.size=new Vector3(.6f,1.6f,.55f);Figure(monk.transform,robe,skin,false);monk.gameObject.layer=0;monk.quest=quest;monk.text="รวบรวมของจากบ้านร้างแล้วนำมาให้อาตมา\n\nตอนนี้เป็นการทดสอบส่งของให้ครบ ยังไม่ใช่สูตรคราฟหรือพิธีปลุกเสกฉบับจริง";
            foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>().Where(t=>t.name=="Open front door"))
            {
                foreach(var child in t.GetComponentsInChildren<Transform>())child.gameObject.isStatic=false;
                var door=t.gameObject.AddComponent<PrototypeDoor>();door.id="door-"+t.parent.name+"-"+t.position.ToString();door.startOpen=!t.IsChildOf(house2);door.SetOpen(door.startOpen,true);
            }
            var hauntObject=new GameObject("Atmospheric shadow - harmless");hauntObject.transform.SetParent(content);hauntObject.transform.SetPositionAndRotation(house2.TransformPoint(new Vector3(0,1.5f,-.5f)),house2.rotation);var trigger=hauntObject.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(8,3,7);var haunt=hauntObject.AddComponent<PrototypeHaunt>();var apparition=new GameObject("Shadow placeholder - replace visual here");apparition.transform.SetParent(content);apparition.transform.position=house2.TransformPoint(new Vector3(-3,.22f,2.2f));Figure(apparition.transform,dark,dark,false);haunt.apparition=apparition;apparition.SetActive(false);
            var forbidden=new GameObject("Forbidden-zone TEST - water tower");forbidden.transform.SetParent(content);forbidden.transform.position=new Vector3(-49,1.5f,64);var fb=forbidden.AddComponent<BoxCollider>();fb.isTrigger=true;fb.size=new Vector3(8,3,8);forbidden.AddComponent<PrototypeForbiddenZone>();
            WireAudio();
            var enemy=new GameObject("PURSUER - tune chase in Inspector");enemy.transform.SetParent(systems);enemy.AddComponent<NavMeshAgent>();var chaser=enemy.AddComponent<PrototypeChaser>();game.chaser=chaser;chaser.game=game;
            var visual=new GameObject("Pursuer placeholder visual").transform;visual.SetParent(enemy.transform,false);var limbs=Figure(visual,Mat("Pursuer shirt",new Color(.23f,.17f,.13f)),skin,true);chaser.visual=visual;chaser.leftLeg=limbs[0];chaser.rightLeg=limbs[1];chaser.leftArm=limbs[2];chaser.rightArm=limbs[3];
            chaser.spawnPoints=new[]{Marker("Spawn road south",new Vector3(0,0,-55)),Marker("Spawn road house 2",new Vector3(0,0,-15)),Marker("Spawn road center",new Vector3(0,0,12)),Marker("Spawn west lane",new Vector3(-35,0,-10)),Marker("Spawn east lane",new Vector3(35,0,4)),Marker("Spawn north",new Vector3(0,0,90)),Marker("Spawn temple",new Vector3(0,0,138))};
            systems.gameObject.AddComponent<PrototypeHud>().game=game;
            BakeNavigation(chaser,player);
            visual.gameObject.SetActive(false);EditorUtility.SetDirty(settings);foreach(var item in settings.items)EditorUtility.SetDirty(item);EditorUtility.SetDirty(quest);AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());Selection.activeGameObject=systems.gameObject;
            File.WriteAllText(Root+"/BUILD_STATUS.json","{\"ok\":true,\"scene\":\""+scenePath+"\",\"pickups\":7,\"quests\":1}");AssetDatabase.Refresh();Debug.Log("GameplayPrototype ready: collection, inventory, quests, save/load, AFK pursuer.");
        }
        static T Asset<T>(string path) where T:ScriptableObject {var data=AssetDatabase.LoadAssetAtPath<T>(path);if(data==null){data=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(data,path);}return data;}
        static Material Mat(string name,Color color,float metallic=0)
        {
            string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",.2f);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);EditorUtility.SetDirty(m);return m;
        }
        static GameObject Cube(string name,Transform parent,Vector3 position,Vector3 size,Material material,bool collider=false)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        static PrototypeItemDefinition Item(string id,string name,PrototypeItemUse use,int stack,string description,Material material,Vector3 size)
        {
            var item=Asset<PrototypeItemDefinition>(Root+"/Data/Items/"+id+".asset");item.id=id;item.displayName=name;item.use=use;item.maxStack=stack;item.description=description;item.color=material.color;
            var root=new GameObject(name);var shape=root.AddComponent<BoxCollider>();shape.size=size;var body=root.AddComponent<Rigidbody>();body.isKinematic=true;body.mass=item.worldMass;root.AddComponent<PrototypePickup>().item=item;Cube("Visual - replace with final model",root.transform,Vector3.zero,size,material);
            item.worldPrefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/"+id+".prefab");UnityEngine.Object.DestroyImmediate(root);return item;
        }
        static void Source(PrototypeItemDefinition item,int count,string id,Vector3 position)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(item.worldPrefab);go.name="LOOT "+item.displayName+" x"+count;go.transform.SetParent(content);go.transform.position=position;var p=go.GetComponent<PrototypePickup>();p.amount=count;p.persistentId=id;p.sceneSpawn=true;
        }
        static Transform Marker(string name,Vector3 position){var go=new GameObject(name);go.transform.SetParent(content);go.transform.position=position;return go.transform;}
        static PrototypeStation Station(string id,Vector3 position,PrototypeStationKind kind,string name)
        {
            var go=Cube(id,content,Vector3.zero,new Vector3(.36f,.13f,.28f),paper,true);go.transform.position=position;
            // Root scale one keeps authored colliders and NPC visuals predictable.
            Vector3 size=go.transform.localScale;go.transform.localScale=Vector3.one;go.GetComponent<BoxCollider>().size=size;UnityEngine.Object.DestroyImmediate(go.GetComponent<MeshFilter>());UnityEngine.Object.DestroyImmediate(go.GetComponent<MeshRenderer>());Cube("Station visual",go.transform,Vector3.zero,size,paper);
            var s=go.AddComponent<PrototypeStation>();s.id=id;s.kind=kind;s.displayName=name;return s;
        }
        static PrototypeItemAmount Amount(PrototypeItemDefinition item,int count)=>new PrototypeItemAmount{item=item,amount=count};
        static Transform[] Figure(Transform parent,Material clothes,Material face,bool knife)
        {
            Cube("Torso",parent,new Vector3(0,1.1f,0),new Vector3(.48f,.65f,.25f),clothes);var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.name="Head";head.transform.SetParent(parent,false);head.transform.localPosition=new Vector3(0,1.65f,0);head.transform.localScale=Vector3.one*.3f;head.GetComponent<Renderer>().sharedMaterial=face;UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
            var result=new Transform[4];for(int i=0;i<4;i++){var pivot=new GameObject(i<2?"Leg pivot":"Arm pivot").transform;pivot.SetParent(parent,false);pivot.localPosition=new Vector3((i%2==0?-1:1)*(i<2?.13f:.32f),i<2?.82f:1.4f,0);Cube("Limb",pivot,new Vector3(0,-.32f,0),new Vector3(.15f,.65f,.17f),clothes);result[i]=pivot;}
            if(knife)Cube("Knife placeholder",result[3],new Vector3(0,-.6f,.17f),new Vector3(.04f,.05f,.35f),metal);
            foreach(var t in parent.GetComponentsInChildren<Transform>())t.gameObject.layer=2;return result;
        }
        static void WireAudio()
        {
            const string path="Assets/TalingchanVillage/Audio/Temporary/";var audio=game.audioSystem;audio.ambience=AssetDatabase.LoadAssetAtPath<AudioClip>(path+"night_crickets.mp3");audio.footsteps=Enumerable.Range(1,6).Select(i=>AssetDatabase.LoadAssetAtPath<AudioClip>(path+"footstep_"+i.ToString("00")+".ogg")).ToArray();audio.pickup=AssetDatabase.LoadAssetAtPath<AudioClip>(path+"item_pickup_temp.ogg");audio.door=AssetDatabase.LoadAssetAtPath<AudioClip>(path+"door_temp.ogg");audio.impact=audio.door;audio.ghostCue=audio.door;
        }
        [MenuItem("Talingchan/Rebake prototype navigation only")]
        public static void RebakeNavigation()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before baking.");
            var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGame>();if(game==null||game.chaser==null)throw new InvalidOperationException("Open GameplayPrototype first.");
            BakeNavigation(game.chaser,game.player);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        static void BakeNavigation(PrototypeChaser chaser,SandboxPlayer player)
        {
            var markups=new List<NavMeshBuildMarkup>();
            var ignored=new List<Transform>{player.transform,chaser.transform};ignored.AddRange(UnityEngine.Object.FindObjectsByType<PrototypePickup>().Select(p=>p.transform));ignored.AddRange(UnityEngine.Object.FindObjectsByType<PrototypeDoor>().Select(p=>p.transform));ignored.AddRange(UnityEngine.Object.FindObjectsByType<Collider>().Where(c=>c.isTrigger).Select(c=>c.transform));
            foreach(var t in ignored)markups.Add(new NavMeshBuildMarkup{root=t,overrideIgnore=true,ignoreFromBuild=true,applyToChildren=true});
            var bounds=new Bounds(new Vector3(0,10,65),new Vector3(220,35,330));var sources=new List<NavMeshBuildSource>();NavMeshBuilder.CollectSources(bounds,~(1<<2),NavMeshCollectGeometry.PhysicsColliders,0,false,markups,false,sources);
            var build=NavMesh.GetSettingsByIndex(0);build.agentRadius=.28f;build.agentHeight=1.75f;build.agentClimb=.35f;build.agentSlope=45;build.overrideVoxelSize=true;build.voxelSize=.14f;build.overrideTileSize=true;build.tileSize=256;
            var data=NavMeshBuilder.BuildNavMeshData(build,sources,bounds,Vector3.zero,Quaternion.identity);if(data==null)throw new Exception("Navigation bake failed");string path=Root+"/Navigation/VillagePrototypeNavMesh.asset";var old=AssetDatabase.LoadAssetAtPath<NavMeshData>(path);if(old!=null){EditorUtility.CopySerialized(data,old);UnityEngine.Object.DestroyImmediate(data);data=old;EditorUtility.SetDirty(old);}else AssetDatabase.CreateAsset(data,path);chaser.navigation=data;chaser.GetComponent<NavMeshAgent>().agentTypeID=build.agentTypeID;
        }
    }
}
#endif
