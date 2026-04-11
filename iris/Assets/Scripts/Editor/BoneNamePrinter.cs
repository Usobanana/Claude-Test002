using UnityEngine;
using UnityEditor;

public static class BoneNamePrinter
{
    [MenuItem("Game/Debug/Print Player Bones")]
    public static void PrintBones()
    {
        var rig = GameObject.Find("POLYGONRig_01");
        if (rig == null) { Debug.LogError("POLYGONRig_01 が見つかりません（シーンを開いてください）"); return; }

        var bones = rig.GetComponentsInChildren<Transform>();
        foreach (var b in bones)
            Debug.Log($"[Bone] {b.name}  (path: {GetPath(b, rig.transform)})");
    }

    private static string GetPath(Transform t, Transform root)
    {
        if (t == root) return root.name;
        return GetPath(t.parent, root) + "/" + t.name;
    }
}
