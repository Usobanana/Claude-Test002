using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// リザルト画面のUI制御。
/// QuestManagerの結果を表示し、E / コントローラーB / ボタンタッチで拠点へ戻る。
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
        if (returnButton == null)
            returnButton = GetComponentInChildren<Button>(true);

        SetupUI();
        returnButton?.onClick.AddListener(ReturnToBase);

        // InteractionPromptUI でキー操作アナウンスを表示
        InteractionPromptUI.Instance?.Show("拠点へ戻る", ReturnToBase);

        if (QuestManager.Instance != null && QuestManager.Instance.IsSuccess)
            AudioManager.Instance?.PlaySE(SFX.QuestClear);
    }

    void OnDestroy()
    {
        InteractionPromptUI.Instance?.Hide();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            ReturnToBase();
        else if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
            ReturnToBase();
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
