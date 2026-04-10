using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// FieldScene の HUD をセットアップするEditorツール
/// </summary>
public static class FieldHUDSetupTool
{
    [MenuItem("Game/Setup Field HUD")]
    public static void SetupFieldHUD()
    {
        // --- Canvas ---
        var canvasGo = new GameObject("HUDCanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create HUD Canvas");

        // ========== HP / スタミナ（左上） ==========
        var statusPanel = CreatePanel(canvasGo.transform, "StatusPanel",
            new Vector2(0f, 1f), new Vector2(0f, 1f),   // anchor 左上
            new Vector2(10f, -10f),                       // pivot 左上
            new Vector2(300f, 100f));

        // HPバー
        var hpBar = CreateSlider(statusPanel.transform, "HPBar",
            new Vector2(0f, -10f), new Vector2(280f, 30f),
            new Color(0.8f, 0.15f, 0.15f));

        // HPテキスト
        var hpTextGo = new GameObject("HPText");
        hpTextGo.transform.SetParent(hpBar.transform, false);
        var hpRect = hpTextGo.AddComponent<RectTransform>();
        hpRect.anchorMin = Vector2.zero;
        hpRect.anchorMax = Vector2.one;
        hpRect.sizeDelta = Vector2.zero;
        var hpTmp = hpTextGo.AddComponent<TextMeshProUGUI>();
        hpTmp.text      = "1200 / 1200";
        hpTmp.fontSize  = 14f;
        hpTmp.alignment = TextAlignmentOptions.Center;
        hpTmp.color     = Color.white;

        // スタミナバー
        var stBar = CreateSlider(statusPanel.transform, "StaminaBar",
            new Vector2(0f, -50f), new Vector2(280f, 20f),
            new Color(0.15f, 0.7f, 0.9f));

        // ========== スキルボタン（下部中央） ==========
        var skillPanel = CreatePanel(canvasGo.transform, "SkillPanel",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 10f),
            new Vector2(400f, 80f));

        string[] labels   = { "Q", "E", "R", "F", "V" };
        string[] names    = { "Skill1", "Skill2", "Skill3", "Ult", "Chain" };
        Color[]  colors   = {
            new Color(0.3f, 0.5f, 0.9f),
            new Color(0.3f, 0.8f, 0.4f),
            new Color(0.9f, 0.6f, 0.2f),
            new Color(0.8f, 0.2f, 0.8f),
            new Color(0.9f, 0.8f, 0.1f),
        };

        var overlayImages = new Image[5];
        float startX = -160f;

        for (int i = 0; i < 5; i++)
        {
            // ボタン背景
            var btnGo = new GameObject(names[i] + "Button");
            btnGo.transform.SetParent(skillPanel.transform, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(startX + i * 80f, 0f);
            btnRect.sizeDelta        = new Vector2(70f, 70f);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = colors[i];

            // クールダウンオーバーレイ（Filled Image）
            var overlayGo = new GameObject("Overlay");
            overlayGo.transform.SetParent(btnGo.transform, false);
            var overlayRect = overlayGo.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            var overlay = overlayGo.AddComponent<Image>();
            overlay.color     = new Color(0f, 0f, 0f, 0.65f);
            overlay.type      = Image.Type.Filled;
            overlay.fillMethod = Image.FillMethod.Radial360;
            overlay.fillOrigin = (int)Image.Origin360.Top;
            overlay.fillClockwise = true;
            overlay.fillAmount = 0f;
            overlayImages[i]  = overlay;

            // キーラベル
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;
            var label       = labelGo.AddComponent<TextMeshProUGUI>();
            label.text      = labels[i];
            label.fontSize  = 20f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color     = Color.white;
        }

        // ========== PlayerHUD コンポーネントをCanvasに追加 ==========
        var hud = canvasGo.AddComponent<PlayerHUD>();

        // Reflection で SerializeField を設定
        SetPrivateField(hud, "hpSlider",      hpBar);
        SetPrivateField(hud, "staminaSlider", stBar);
        SetPrivateField(hud, "hpText",        hpTextGo.GetComponent<TextMeshProUGUI>());
        SetPrivateField(hud, "skill1Overlay", overlayImages[0]);
        SetPrivateField(hud, "skill2Overlay", overlayImages[1]);
        SetPrivateField(hud, "skill3Overlay", overlayImages[2]);
        SetPrivateField(hud, "ultOverlay",    overlayImages[3]);
        SetPrivateField(hud, "chainOverlay",  overlayImages[4]);

        Debug.Log("[FieldHUDSetupTool] HUD セットアップ完了");
    }

    // --- ヘルパー ---

    private static RectTransform CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect         = go.AddComponent<RectTransform>();
        rect.anchorMin   = anchorMin;
        rect.anchorMax   = anchorMax;
        rect.pivot       = anchorMin; // pivot = anchorMin で左上/中央下などに揃える
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta   = size;
        return rect;
    }

    private static Slider CreateSlider(Transform parent, string name,
        Vector2 anchoredPos, Vector2 size, Color fillColor)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0f, 1f);
        rect.anchorMax        = new Vector2(0f, 1f);
        rect.pivot            = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta        = size;

        // 背景
        var bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        var bgRect       = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

        // Fill Area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var faRect       = fillArea.AddComponent<RectTransform>();
        faRect.anchorMin = new Vector2(0f, 0f);
        faRect.anchorMax = new Vector2(1f, 1f);
        faRect.offsetMin = new Vector2(2f, 2f);
        faRect.offsetMax = new Vector2(-2f, -2f);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRect       = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.sizeDelta = Vector2.zero;
        fill.AddComponent<Image>().color = fillColor;

        var slider            = go.AddComponent<Slider>();
        slider.fillRect       = fillRect;
        slider.minValue       = 0f;
        slider.maxValue       = 1f;
        slider.value          = 1f;
        slider.interactable   = false;
        slider.transition     = Selectable.Transition.None;

        return slider;
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(obj, value);
    }
}
