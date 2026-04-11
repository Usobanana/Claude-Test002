using UnityEngine;
using UnityEditor;

/// <summary>
/// AudioManager と SceneBGM を各シーンにセットアップするEditorツール
/// AudioManager は BaseScene の SceneManagers に追加（DontDestroyOnLoad で全シーン共有）
/// SceneBGM は各シーンの SceneManagers に追加してシーンごとのBGMを制御する
/// </summary>
public static class AudioManagerSetupTool
{
    [MenuItem("Game/Setup AudioManager (BaseScene)")]
    public static void SetupAudioManager()
    {
        // SceneManagers オブジェクトを探す（なければ新規作成）
        var managers = GameObject.Find("SceneManagers");
        if (managers == null)
        {
            managers = new GameObject("SceneManagers");
            Undo.RegisterCreatedObjectUndo(managers, "Create SceneManagers");
        }

        // AudioManager を追加（既にある場合はスキップ）
        if (managers.GetComponent<AudioManager>() == null)
        {
            managers.AddComponent<AudioManager>();
            Debug.Log("[AudioSetup] AudioManager を SceneManagers に追加しました");
        }
        else
        {
            Debug.Log("[AudioSetup] AudioManager は既に存在します");
        }

        // SceneBGM を追加（BGMの自動切り替え用）
        if (managers.GetComponent<SceneBGM>() == null)
        {
            managers.AddComponent<SceneBGM>();
            Debug.Log("[AudioSetup] SceneBGM を追加しました");
        }

        AssignAudioClips(managers.GetComponent<AudioManager>());
        Debug.Log("[AudioSetup] 完了");
    }

    [MenuItem("Game/Assign AudioClips to AudioManager")]
    public static void AssignAudioClips()
    {
        var managers = GameObject.Find("SceneManagers");
        if (managers == null) { Debug.LogError("[AudioSetup] SceneManagers が見つかりません"); return; }
        var am = managers.GetComponent<AudioManager>();
        if (am == null) { Debug.LogError("[AudioSetup] AudioManager が見つかりません"); return; }
        AssignAudioClips(am);
        EditorUtility.SetDirty(am);
    }

    private static void AssignAudioClips(AudioManager am)
    {
        if (am == null) return;
        var so = new SerializedObject(am);

        AssignClip(so, "fieldBGM",              "Assets/Audio/BGM/BGM_Battle_2.mp3");
        AssignClip(so, "bossBGM",               "Assets/Audio/BGM/BGM_Battle_2.mp3");
        AssignClip(so, "playerAttackSE",        "Assets/Audio/SE/Battle/Slash/SE_swing1.mp3");
        AssignClip(so, "playerDodgeSE",         "Assets/Audio/SE/Battle/Slash/SE_swing2.mp3");
        AssignClip(so, "playerHurtSE",          "Assets/Audio/SE/Battle/Hit/SE_Hit_1.mp3");
        AssignClip(so, "playerDeathSE",         "Assets/Audio/SE/Battle/Hit/SE_Hit_3.mp3");
        AssignClip(so, "enemyHurtSE",           "Assets/Audio/SE/Battle/Hit/SE_Hit_2.mp3");
        AssignClip(so, "enemyDeathSE",          "Assets/Audio/SE/Battle/Hit/SE_Hit_3.mp3");
        AssignClip(so, "bossPhaseTransitionSE", "Assets/Audio/SE/Battle/Hit/SE_Hit_3.mp3");
        AssignClip(so, "bossDeathSE",           "Assets/Audio/SE/Battle/Hit/SE_Hit_3.mp3");
        AssignClip(so, "questClearSE",          "Assets/Audio/SE/Battle/Hit/SE_Hit_1.mp3");

        so.ApplyModifiedProperties();
        Debug.Log("[AudioSetup] AudioClip のアサイン完了");
    }

    private static void AssignClip(SerializedObject so, string fieldName, string assetPath)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (clip == null) { Debug.LogWarning($"[AudioSetup] クリップが見つかりません: {assetPath}"); return; }
        so.FindProperty(fieldName).objectReferenceValue = clip;
    }

    [MenuItem("Game/Add SceneBGM to Current Scene")]
    public static void AddSceneBGM()
    {
        var managers = GameObject.Find("SceneManagers");
        if (managers == null)
        {
            Debug.LogWarning("[AudioSetup] SceneManagers が見つかりません");
            return;
        }

        if (managers.GetComponent<SceneBGM>() == null)
        {
            managers.AddComponent<SceneBGM>();
            Debug.Log("[AudioSetup] SceneBGM を追加しました");
        }
        else
        {
            Debug.Log("[AudioSetup] SceneBGM は既に存在します");
        }
    }
}
