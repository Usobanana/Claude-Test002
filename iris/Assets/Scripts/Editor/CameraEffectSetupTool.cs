using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP Post Processing（Global Volume + Camera 設定）を各シーンにセットアップする
/// Game/Setup Post Processing (Current Scene) を実行するだけでOK
/// </summary>
public static class CameraEffectSetupTool
{
    private const string ProfilePath = "Assets/Settings/GlobalVolumeProfile.asset";

    [MenuItem("Game/Setup Post Processing (Current Scene)")]
    public static void SetupPostProcessing()
    {
        // カメラに Post Processing を有効化
        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("[CameraEffect] Main Camera が見つかりません");
            return;
        }

        var urpData = camera.GetComponent<UniversalAdditionalCameraData>();
        if (urpData == null)
        {
            urpData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            Debug.Log("[CameraEffect] UniversalAdditionalCameraData を追加しました");
        }

        urpData.renderPostProcessing = true;
        EditorUtility.SetDirty(urpData);

        // Global Volume を作成（既にあればスキップ）
        var existing = GameObject.Find("GlobalVolume");
        if (existing != null)
        {
            Debug.Log("[CameraEffect] GlobalVolume は既に存在します");
            MarkSceneDirty();
            return;
        }

        var volumeGO = new GameObject("GlobalVolume");
        Undo.RegisterCreatedObjectUndo(volumeGO, "Create GlobalVolume");

        var volume = volumeGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;

        // VolumeProfile を作成（共有アセット）
        var profile = GetOrCreateProfile();
        volume.sharedProfile = profile;

        EditorUtility.SetDirty(volume);
        MarkSceneDirty();

        Debug.Log("[CameraEffect] Post Processing セットアップ完了（Bloom / ColorAdjustments）");
    }

    private static VolumeProfile GetOrCreateProfile()
    {
        // 既存のプロファイルを再利用
        var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (existing != null) return existing;

        // Settings フォルダが無ければ作成
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        // Bloom：控えめに（ポリゴンスタイルなので派手にしない）
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(0.4f);
        bloom.scatter.Override(0.65f);

        // Color Adjustments：彩度・コントラストを少し上げてポリゴン感を強調
        var colorAdj = profile.Add<ColorAdjustments>(true);
        colorAdj.contrast.Override(8f);
        colorAdj.saturation.Override(12f);

        AssetDatabase.CreateAsset(profile, ProfilePath);
        AssetDatabase.SaveAssets();

        return profile;
    }

    private static void MarkSceneDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene()
        );
    }
}
