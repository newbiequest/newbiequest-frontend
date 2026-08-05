// 이 파일을 Assets/Editor/RemoveMisplacedNameTags.cs 로 저장
using UnityEngine;
using UnityEditor;

public class RemoveMisplacedNameTags : Editor
{
    [MenuItem("Tools/NPCNameTag 잘못된거 제거")]
    static void RemoveAll()
    {
        NPCNameTag[] all = FindObjectsByType<NPCNameTag>(FindObjectsSortMode.None);

        foreach (var tag in all)
        {
            // 이름에 bind_, mesh_, NameTag, NameText 포함된 오브젝트에서 제거
            string name = tag.gameObject.name;
            if (name.StartsWith("bind_") || name.StartsWith("mesh_") ||
                name == "NameTagCanvas" || name == "NameText")
            {
                DestroyImmediate(tag);
                Debug.Log($"제거됨: {name}");
            }
        }

        Debug.Log("완료!");
    }
}