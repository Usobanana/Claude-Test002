using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.InputSystem;

/// <summary>
/// 近接インタラクション用のプロンプトボタンを画面下部に表示するシングルトン。
/// GuildInteraction・FieldExitTrigger・ResultScreenUI などから Show()/Hide() を呼ぶ。
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    [SerializeField] private Button          promptButton;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI hintText;   // "[E]" / "[B]" 表示用（任意）

    private Action currentCallback;
    private bool   isVisible;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        if (promptButton == null)
            promptButton = GetComponentInChildren<Button>(true);

        // promptText / hintText は子から順に割り当て
        if (promptText == null || hintText == null)
        {
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                if (promptText == null && t.gameObject.name != "HintText") promptText = t;
                else if (hintText == null) hintText = t;
            }
        }

        promptButton?.onClick.AddListener(OnButtonClicked);
        SetVisible(false);
    }

    void Update()
    {
        if (!isVisible || currentCallback == null) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            currentCallback.Invoke();

        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
            currentCallback.Invoke();

        UpdateHintText();
    }

    /// <summary>ボタンを表示してラベルとコールバックをセットする。</summary>
    public void Show(string label, Action callback)
    {
        currentCallback = callback;
        if (promptText != null) promptText.text = label;
        UpdateHintText();
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

    private void UpdateHintText()
    {
        if (hintText == null) return;
        hintText.text = Gamepad.current != null ? "[B]" : "[E]";
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;
        gameObject.SetActive(visible);
    }
}
