using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// タイトル画面のUI制御。
/// 「ゲームを始める」ボタンでBaseSceneへ遷移する。
/// </summary>
public class TitleScreenUI : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private Button          startButton;
    [SerializeField] private TextMeshProUGUI titleText;

    void Start()
    {
        // SerializeField未設定の場合は自動検索
        if (startButton == null)
            startButton = GetComponentInChildren<Button>(true);
        if (titleText == null)
        {
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
                if (t.gameObject.name == "TitleText") { titleText = t; break; }
        }

        startButton?.onClick.AddListener(OnStartClicked);
    }

    private void OnStartClicked()
    {
        GameManager.Instance?.GoToBase();
    }
}
