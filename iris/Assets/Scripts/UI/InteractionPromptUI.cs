using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 近接インタラクション用のプロンプトボタンを画面下部に表示するシングルトン。
/// GuildInteraction・FieldExitTrigger などから Show()/Hide() を呼ぶ。
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    [SerializeField] private Button          promptButton;
    [SerializeField] private TextMeshProUGUI promptText;

    private Action currentCallback;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // SerializeField未設定の場合は自動検索
        if (promptButton == null)
            promptButton = GetComponentInChildren<Button>(true);
        if (promptText == null)
            promptText = GetComponentInChildren<TextMeshProUGUI>(true);

        promptButton?.onClick.AddListener(OnButtonClicked);
        SetVisible(false);
    }

    /// <summary>ボタンを表示してラベルとコールバックをセットする。</summary>
    public void Show(string label, Action callback)
    {
        currentCallback = callback;
        if (promptText != null) promptText.text = label;
        SetVisible(true);
    }

    /// <summary>ボタンを非表示にする。</summary>
    public void Hide()
    {
        currentCallback = null;
        SetVisible(false);
    }

    private void OnButtonClicked()
    {
        currentCallback?.Invoke();
    }

    private void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
