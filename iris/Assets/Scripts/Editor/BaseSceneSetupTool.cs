using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BaseScene・ResultScene のセットアップツール
/// </summary>
public static class BaseSceneSetupTool
{
    [MenuItem("Game/Setup Base Scene Objects")]
    public static void SetupBaseSceneObjects()
    {
        // Ground
        var ground = GameObject.Find("Ground");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
            Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
        }

        // Guild
        var guild = GameObject.Find("Guild");
        if (guild == null)
        {
            guild = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guild.name = "Guild";
            guild.transform.position = new Vector3(5f, 1f, 0f);
            guild.transform.localScale = new Vector3(2f, 2f, 2f);
            guild.AddComponent<GuildInteraction>();
            Undo.RegisterCreatedObjectUndo(guild, "Create Guild");
        }

        // FieldExit
        var exit = GameObject.Find("FieldExit");
        if (exit == null)
        {
            exit = new GameObject("FieldExit");
            exit.transform.position = new Vector3(-5f, 0f, 0f);
            exit.AddComponent<FieldExitTrigger>();

            // 視覚的なマーカー
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "ExitMarker";
            marker.transform.SetParent(exit.transform);
            marker.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            marker.transform.localScale    = new Vector3(2f, 0.1f, 2f);
            Object.DestroyImmediate(marker.GetComponent<CapsuleCollider>());
            Undo.RegisterCreatedObjectUndo(exit, "Create FieldExit");
        }

        // QuestManager
        var qm = GameObject.Find("QuestManager");
        if (qm == null)
        {
            qm = new GameObject("QuestManager");
            qm.AddComponent<QuestManager>();
            Undo.RegisterCreatedObjectUndo(qm, "Create QuestManager");
        }

        // InteractionPromptUI Canvas
        SetupInteractionPromptUI();

        Debug.Log("[BaseSceneSetupTool] BaseScene セットアップ完了");
    }

    private static void SetupInteractionPromptUI()
    {
        if (GameObject.Find("InteractionPromptCanvas") != null) return;

        // Canvas
        var canvasGo = new GameObject("InteractionPromptCanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // InteractionPromptUI コンポーネント
        var promptUI = canvasGo.AddComponent<InteractionPromptUI>();

        // ボタン（画面下中央）
        var btnGo   = new GameObject("PromptButton");
        btnGo.transform.SetParent(canvasGo.transform, false);
        var btnRect = btnGo.AddComponent<RectTransform>();
        btnRect.anchorMin        = new Vector2(0.5f, 0f);
        btnRect.anchorMax        = new Vector2(0.5f, 0f);
        btnRect.pivot            = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = new Vector2(0f, 40f);
        btnRect.sizeDelta        = new Vector2(320f, 70f);

        var btnImg  = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        var btn = btnGo.AddComponent<Button>();

        var txtGo   = new GameObject("Label");
        txtGo.transform.SetParent(btnGo.transform, false);
        var txtRect = txtGo.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
        var txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text      = "インタラクション";
        txt.fontSize  = 26f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = Color.white;

        Undo.RegisterCreatedObjectUndo(canvasGo, "Create InteractionPromptUI");
    }

    [MenuItem("Game/Setup Title Scene Objects")]
    public static void SetupTitleSceneObjects()
    {
        if (GameObject.Find("TitleCanvas") != null)
        {
            Debug.Log("[BaseSceneSetupTool] TitleCanvas は既に存在します");
            return;
        }

        // Canvas
        var canvasGo = new GameObject("TitleCanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // 背景パネル
        var panel     = new GameObject("Panel");
        panel.transform.SetParent(canvasGo.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        // タイトルテキスト
        var titleGo   = new GameObject("TitleText");
        titleGo.transform.SetParent(panel.transform, false);
        var titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0f, 80f);
        titleRect.sizeDelta        = new Vector2(700f, 120f);
        var titleTxt  = titleGo.AddComponent<TextMeshProUGUI>();
        titleTxt.text      = "IRIS";
        titleTxt.fontSize  = 72f;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.color     = Color.white;

        // スタートボタン
        var btnGo   = new GameObject("StartButton");
        btnGo.transform.SetParent(panel.transform, false);
        var btnRect = btnGo.AddComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0f, -80f);
        btnRect.sizeDelta        = new Vector2(300f, 70f);
        var btnImg  = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.5f, 0.8f);
        btnGo.AddComponent<Button>();

        var btnTxtGo   = new GameObject("Text");
        btnTxtGo.transform.SetParent(btnGo.transform, false);
        var btnTxtRect = btnTxtGo.AddComponent<RectTransform>();
        btnTxtRect.anchorMin = Vector2.zero;
        btnTxtRect.anchorMax = Vector2.one;
        btnTxtRect.sizeDelta = Vector2.zero;
        var btnTxt = btnTxtGo.AddComponent<TextMeshProUGUI>();
        btnTxt.text      = "ゲームを始める";
        btnTxt.fontSize  = 28f;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.color     = Color.white;

        // TitleScreenUI をCanvasに追加
        canvasGo.AddComponent<TitleScreenUI>();

        Undo.RegisterCreatedObjectUndo(canvasGo, "Setup Title Canvas");
        Debug.Log("[BaseSceneSetupTool] TitleScene セットアップ完了");
    }

    [MenuItem("Game/Setup Result Scene Objects")]
    public static void SetupResultSceneObjects()
    {
        // Canvas
        var canvas = new GameObject("ResultCanvas");
        var c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Panel
        var panel = new GameObject("Panel");
        panel.transform.SetParent(canvas.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panel.AddComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, 0.7f);

        // ResultTitle
        var titleGo = new GameObject("ResultTitle");
        titleGo.transform.SetParent(panel.transform, false);
        var titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0f, 150f);
        titleRect.sizeDelta = new Vector2(600f, 100f);
        var titleText = titleGo.AddComponent<TMPro.TextMeshProUGUI>();
        titleText.text = "QUEST CLEAR";
        titleText.fontSize = 60f;
        titleText.alignment = TMPro.TextAlignmentOptions.Center;

        // EXP Text
        var expGo = new GameObject("ExpText");
        expGo.transform.SetParent(panel.transform, false);
        var expRect = expGo.AddComponent<RectTransform>();
        expRect.anchoredPosition = new Vector2(0f, 30f);
        expRect.sizeDelta = new Vector2(400f, 60f);
        var expText = expGo.AddComponent<TMPro.TextMeshProUGUI>();
        expText.text = "EXP +0";
        expText.fontSize = 36f;
        expText.alignment = TMPro.TextAlignmentOptions.Center;

        // Gold Text
        var goldGo = new GameObject("GoldText");
        goldGo.transform.SetParent(panel.transform, false);
        var goldRect = goldGo.AddComponent<RectTransform>();
        goldRect.anchoredPosition = new Vector2(0f, -30f);
        goldRect.sizeDelta = new Vector2(400f, 60f);
        var goldText = goldGo.AddComponent<TMPro.TextMeshProUGUI>();
        goldText.text = "Gold +0";
        goldText.fontSize = 36f;
        goldText.alignment = TMPro.TextAlignmentOptions.Center;

        // Return Button
        var btnGo = new GameObject("ReturnButton");
        btnGo.transform.SetParent(panel.transform, false);
        var btnRect = btnGo.AddComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0f, -140f);
        btnRect.sizeDelta = new Vector2(300f, 70f);
        var btnImg = btnGo.AddComponent<UnityEngine.UI.Image>();
        btnImg.color = new Color(0.2f, 0.6f, 0.2f);
        var btn = btnGo.AddComponent<UnityEngine.UI.Button>();

        var btnTextGo = new GameObject("Text");
        btnTextGo.transform.SetParent(btnGo.transform, false);
        var btnTRect = btnTextGo.AddComponent<RectTransform>();
        btnTRect.anchorMin = Vector2.zero;
        btnTRect.anchorMax = Vector2.one;
        btnTRect.sizeDelta = Vector2.zero;
        var btnText = btnTextGo.AddComponent<TMPro.TextMeshProUGUI>();
        btnText.text = "拠点に戻る";
        btnText.fontSize = 28f;
        btnText.alignment = TMPro.TextAlignmentOptions.Center;

        // ResultScreenUI をCanvasに追加
        var ui = canvas.AddComponent<ResultScreenUI>();

        Undo.RegisterCreatedObjectUndo(canvas, "Setup Result Canvas");
        Debug.Log("[BaseSceneSetupTool] ResultScene セットアップ完了（UIをResultScreenUIにアサインしてください）");
    }

    [MenuItem("Game/Create Quest Data Asset")]
    public static void CreateQuestDataAsset()
    {
        var quest = ScriptableObject.CreateInstance<QuestData>();
        quest.questId       = 1;
        quest.questName     = "炎の試練";
        quest.description   = "フィールドに潜む炎のボスを討伐せよ。";
        quest.rewardExp     = 500;
        quest.rewardGold    = 300;
        quest.fieldSceneName = "FieldScene";

        AssetDatabase.CreateAsset(quest, "Assets/Data/Quest_001_FireTrial.asset");
        AssetDatabase.SaveAssets();
        Debug.Log("[BaseSceneSetupTool] QuestData 作成: Assets/Data/Quest_001_FireTrial.asset");
    }
}
