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

        Debug.Log("[AudioSetup] 完了 → AudioClip を Inspector でアサインしてください");
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
