using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// リザルト画面のUI制御
/// QuestManagerの結果を表示し、拠点に戻るボタンを提供する
/// </summary>
public class ResultScreenUI : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button          returnButton;

    void Start()
    {
        SetupUI();
        returnButton?.onClick.AddListener(ReturnToBase);

        // クエスト成功時にSEを再生
        if (QuestManager.Instance != null && QuestManager.Instance.IsSuccess)
            AudioManager.Instance?.PlaySE(SFX.QuestClear);
    }

    private void SetupUI()
    {
        var qm = QuestManager.Instance;
        if (qm == null) return;

        bool success = qm.IsSuccess;

        if (resultTitleText != null)
            resultTitleText.text = success ? "QUEST CLEAR" : "QUEST FAILED";

        if (questNameText != null)
            questNameText.text = qm.ActiveQuest != null ? qm.ActiveQuest.questName : "";

        if (expText != null)
            expText.text = $"EXP +{qm.EarnedExp}";

        if (goldText != null)
            goldText.text = $"Gold +{qm.EarnedGold}";
    }

    private void ReturnToBase()
    {
        QuestManager.Instance?.ResetQuest();
        GameManager.Instance?.GoToBase();
    }
}
