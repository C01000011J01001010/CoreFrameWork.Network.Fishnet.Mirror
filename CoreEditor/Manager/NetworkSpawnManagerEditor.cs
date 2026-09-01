using CoreEngine.Network.FishNetExtension.Manager;
using UnityEditor;
using UnityEngine;

namespace CoreEditor.Network.Spawn
{
    // true 인자를 주어 모든 파생 클래스(FarmingSpawnManager 등)에 자동으로 이 에디터가 적용되게 합니다.
    [CustomEditor(typeof(NetworkSpawnManager<>), true)]
    public class NetworkSpawnManagerEditor : Editor
    {
        private int _selectedIndex = -1;

        private void OnSceneGUI()
        {
            serializedObject.Update();

            // 제네릭 타입 캐스팅 에러를 피하기 위해 SerializedProperty로 다이렉트 접근
            SerializedProperty listProp = serializedObject.FindProperty("spawnDataList");
            SerializedProperty showConesProp = serializedObject.FindProperty("showAllCones");

            if (listProp == null) return;

            bool showAllCones = showConesProp.boolValue;

            for (int i = 0; i < listProp.arraySize; i++)
            {
                SerializedProperty elementProp = listProp.GetArrayElementAtIndex(i);
                SerializedProperty posProp = elementProp.FindPropertyRelative("position");
                SerializedProperty rotProp = elementProp.FindPropertyRelative("rotation");
                // 풀 타입 라벨 표시를 위해 enum 속성도 가져옵니다.
                SerializedProperty typeProp = elementProp.FindPropertyRelative("poolType");

                Vector3 pos = posProp.vector3Value;
                Quaternion rot = Quaternion.Euler(rotProp.vector3Value);
                float handleSize = HandleUtility.GetHandleSize(pos);

                // 1. 점(Dot) 핫스팟 렌더링 및 클릭 감지
                Handles.color = (_selectedIndex == i) ? Color.cyan : Color.white;
                if (Handles.Button(pos, rot, handleSize * 0.1f, handleSize * 0.15f, Handles.SphereHandleCap))
                {
                    _selectedIndex = i;
                    Repaint();
                }

                // 2. 전체 스폰 데이터의 회전 방향을 보여주는 원뿔 (글로벌 토글)
                if (showAllCones)
                {
                    Handles.color = new Color(1f, 0.8f, 0f, 0.4f); // 반투명 노란색
                    Handles.ConeHandleCap(0, pos, rot, handleSize * 0.3f, EventType.Repaint);
                }

                // 3. 사용자가 핫스팟을 클릭하여 선택한 항목에만 조작 핸들 노출[cite: 3]
                if (_selectedIndex == i)
                {
                    EditorGUI.BeginChangeCheck();

                    Vector3 newPos = pos;
                    Quaternion newRot = rot;

                    // 유니티 상단 툴(W, E) 상태에 따라 이동/회전 핸들 스위칭[cite: 3]
                    if (Tools.current == Tool.Rotate)
                    {
                        newRot = Handles.RotationHandle(rot, pos);
                    }
                    else
                    {
                        newPos = Handles.PositionHandle(pos, rot);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Modify Spawn Point");
                        posProp.vector3Value = newPos;
                        rotProp.vector3Value = newRot.eulerAngles;
                        serializedObject.ApplyModifiedProperties();
                    }

                    // 어떤 스폰 대상인지 씬 뷰에 직관적인 라벨 표시
                    GUIStyle labelStyle = new GUIStyle() { normal = new GUIStyleState() { textColor = Color.cyan }, fontStyle = FontStyle.Bold };
                    string typeName = typeProp.enumNames[typeProp.enumValueIndex];
                    Handles.Label(pos + Vector3.up * (handleSize * 0.5f), $"[{i}] {typeName}", labelStyle);
                }
            }
        }
    }
}