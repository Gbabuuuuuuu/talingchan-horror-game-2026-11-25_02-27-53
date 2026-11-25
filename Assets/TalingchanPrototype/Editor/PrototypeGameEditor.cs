#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace Talingchan.Prototype.Editor
{
    [CustomEditor(typeof(PrototypeGame))]
    public class PrototypeGameEditor:UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var game=(PrototypeGame)target;
            EditorGUILayout.HelpBox("ชุดทดลอง: Settings = กระเป๋า / AFK / ปุ่ม / เซฟ / UI\nQuest และ Item เป็น assets แยกแก้ไขได้\nPursuer = ความเร็ว / เวลาเตือน / การมองเห็น / เสียง\nค่าปุ่มมีผลเมื่อเริ่ม Play รอบถัดไป",MessageType.Info);
            DrawDefaultInspector();
            if(game.settings!=null&&GUILayout.Button("Select Gameplay Settings"))Selection.activeObject=game.settings;
            if(game.chaser!=null&&GUILayout.Button("Select Pursuer Settings"))Selection.activeGameObject=game.chaser.gameObject;
            using(new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                EditorGUILayout.Space();EditorGUILayout.LabelField("PLAY MODE TESTS",EditorStyles.boldLabel);
                if(GUILayout.Button("New run / เริ่มรอบใหม่"))game.DebugNewRun();
                if(GUILayout.Button("Save checkpoint / บันทึก"))game.DebugSave();
                if(GUILayout.Button("Load checkpoint / โหลด"))game.DebugLoad();
                if(GUILayout.Button("Spawn pursuer / เรียกคนไล่"))game.DebugSpawnChaser();
                if(GUILayout.Button("Death test / ทดสอบตาย"))game.DebugDie();
                if(GUILayout.Button("Trigger harmless shadow / ทดลองเงาผี"))foreach(var ghost in Object.FindObjectsByType<PrototypeHaunt>())ghost.Trigger();
                if(GUILayout.Button("Close UI / ปิดหน้าจออ่านหรือกระเป๋า"))game.CloseModal();
            }
        }
    }
}
#endif
