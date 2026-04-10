using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// シーン毎のBGMを制御する軽量コンポーネント
/// 各シーンの SceneManagers（またはGameManager）にアタッチして使う
/// Start でアクティブシーン名を AudioManager に渡す
/// </summary>
public class SceneBGM : MonoBehaviour
{
    void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM(SceneManager.GetActiveScene().name);
    }
}
