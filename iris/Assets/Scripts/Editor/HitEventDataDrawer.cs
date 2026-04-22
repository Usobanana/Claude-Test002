using UnityEditor;
using UnityEngine;

/// <summary>
/// HitEventData の Inspector 表示。
/// ・総フレーム数を判定タイミングフィールドの右側に表示
/// ・shapeType に応じて対応する設定項目だけを表示
/// </summary>
[CustomPropertyDrawer(typeof(HitEventData))]
public class HitEventDataDrawer : PropertyDrawer
{
    private const float LineH   = 18f;
    private const float Spacing = 2f;
    private const float SectionGap = 6f;

    // ─── 高さ計算 ───────────────────────────────

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return LineH + Spacing;

        var shapeType = (HitShapeType)property.FindPropertyRelative("shapeType").enumValueIndex;

        int dataRows      = 2 + 1 + ShapeRows(shapeType) + 1 + 4 + 2 + 3;
        int sectionLabels = 5;  // タイミング／形状／ダメージ／打感・ノックバック／SE
        int sectionGaps   = 5;

        // circleOffset は Vector3 なので2行分
        float extra = shapeType == HitShapeType.Circle ? (LineH + Spacing) : 0f;

        return (LineH + Spacing)                                    // ヘッダー
             + (dataRows + sectionLabels) * (LineH + Spacing)
             + sectionGaps * SectionGap
             + extra;
    }

    // ─── 描画 ────────────────────────────────────

    public override void OnGUI(Rect pos, SerializedProperty property, GUIContent label)
    {
        int totalFrames = GetTotalFrames(property);

        EditorGUI.BeginProperty(pos, label, property);

        // ヘッダー
        var headerRect = new Rect(pos.x, pos.y, pos.width, LineH);
        int startF = property.FindPropertyRelative("hitStartFrame").intValue;
        int endF   = property.FindPropertyRelative("hitEndFrame").intValue;
        string frameInfo = totalFrames > 0 ? $"  [{startF}〜{endF} / {totalFrames}f]" : $"  [{startF}〜{endF}]";
        property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(headerRect, property.isExpanded,
            label.text + frameInfo);
        EditorGUI.EndFoldoutHeaderGroup();

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        float y = headerRect.yMax + Spacing;

        // ── 判定タイミング ──────────────────────
        y = SectionLabel(pos, y, "判定タイミング（フレーム）");
        y = FrameRow(pos, y, property.FindPropertyRelative("hitStartFrame"), "開始フレーム", totalFrames);
        y = FrameRow(pos, y, property.FindPropertyRelative("hitEndFrame"),   "終了フレーム", totalFrames);

        // ── 判定形状 ────────────────────────────
        y += SectionGap;
        y = SectionLabel(pos, y, "判定形状");
        y = Row(pos, y, property.FindPropertyRelative("shapeType"), "形状");

        var shapeType = (HitShapeType)property.FindPropertyRelative("shapeType").enumValueIndex;
        EditorGUI.indentLevel++;
        y = DrawShapeFields(pos, y, property, shapeType);
        EditorGUI.indentLevel--;

        // ── ダメージ ────────────────────────────
        y += SectionGap;
        y = SectionLabel(pos, y, "ダメージ");
        y = Row(pos, y, property.FindPropertyRelative("damageRate"), "倍率 (%)");

        // ── 打感 ────────────────────────────────
        y += SectionGap;
        y = SectionLabel(pos, y, "打感 / ノックバック");
        EditorGUI.indentLevel++;
        y = Row(pos, y, property.FindPropertyRelative("hitStopTimeScale"),  "HitStop スケール");
        y = Row(pos, y, property.FindPropertyRelative("hitStopDuration"),   "HitStop 時間");
        y = Row(pos, y, property.FindPropertyRelative("hitShakeMagnitude"), "シェイク 強度");
        y = Row(pos, y, property.FindPropertyRelative("hitShakeDuration"),  "シェイク 時間");
        y = Row(pos, y, property.FindPropertyRelative("knockbackForce"),    "ノックバック 距離");
        y = Row(pos, y, property.FindPropertyRelative("knockbackDuration"), "ノックバック 時間");
        EditorGUI.indentLevel--;

        // ── SE ──────────────────────────────────
        y += SectionGap;
        y = SectionLabel(pos, y, "SE");
        EditorGUI.indentLevel++;
        y = Row(pos, y, property.FindPropertyRelative("swingSE"),      "振り始めSE");
        y = FrameRow(pos, y, property.FindPropertyRelative("swingSEFrame"), "振り始めSE フレーム", totalFrames, "-1=開始と同じ");
        y = Row(pos, y, property.FindPropertyRelative("hitSE"),        "ヒットSE");
        EditorGUI.indentLevel--;

        EditorGUI.EndProperty();
    }

    // ─── ヘルパー ────────────────────────────────

    private static float SectionLabel(Rect pos, float y, string text)
    {
        var rect = new Rect(pos.x, y, pos.width, LineH);
        var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 10 };
        var prevColor = GUI.color;
        GUI.color = new Color(0.7f, 0.85f, 1f);
        EditorGUI.LabelField(rect, text, style);
        GUI.color = prevColor;
        return y + LineH + Spacing;
    }

    private static float Row(Rect pos, float y, SerializedProperty prop, string label)
    {
        var rect = new Rect(pos.x, y, pos.width, LineH);
        EditorGUI.PropertyField(rect, prop, new GUIContent(label));
        return y + LineH + Spacing;
    }

    private static float FrameRow(Rect pos, float y, SerializedProperty prop, string label,
                                  int totalFrames, string hint = null)
    {
        // ラベル幅を確保しつつ右端に "/ Nf" を表示
        float suffixW = 52f;
        var fieldRect  = new Rect(pos.x, y, pos.width - suffixW - 2f, LineH);
        var suffixRect = new Rect(pos.xMax - suffixW, y, suffixW, LineH);

        string fullLabel = hint != null ? $"{label} ({hint})" : label;
        EditorGUI.PropertyField(fieldRect, prop, new GUIContent(fullLabel));

        var prevColor = GUI.color;
        GUI.color = totalFrames > 0 ? new Color(0.6f, 0.9f, 0.6f) : new Color(0.6f, 0.6f, 0.6f);
        EditorGUI.LabelField(suffixRect, totalFrames > 0 ? $"/ {totalFrames}f" : "/ ?f",
            new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight });
        GUI.color = prevColor;

        return y + LineH + Spacing;
    }

    private static float DrawShapeFields(Rect pos, float y, SerializedProperty prop, HitShapeType shape)
    {
        switch (shape)
        {
            case HitShapeType.Arc:
                y = Row(pos, y, prop.FindPropertyRelative("arcRange"), "距離");
                y = Row(pos, y, prop.FindPropertyRelative("arcAngle"), "角度");
                break;
            case HitShapeType.Rectangle:
                y = Row(pos, y, prop.FindPropertyRelative("rectLength"),        "奥行き");
                y = Row(pos, y, prop.FindPropertyRelative("rectWidth"),         "幅");
                y = Row(pos, y, prop.FindPropertyRelative("rectForwardOffset"), "前方オフセット");
                break;
            case HitShapeType.Circle:
                y = Row(pos, y, prop.FindPropertyRelative("circleRadius"), "半径");
                y = Row(pos, y, prop.FindPropertyRelative("circleOffset"), "オフセット");
                break;
        }
        return y;
    }

    private static int GetTotalFrames(SerializedProperty hitEventProp)
    {
        string path = hitEventProp.propertyPath;
        int idx = path.LastIndexOf(".hitEvents");
        if (idx < 0) return 0;
        string stepPath = path.Substring(0, idx);
        var clipProp    = hitEventProp.serializedObject.FindProperty(stepPath + ".clip");
        var clip        = clipProp?.objectReferenceValue as AnimationClip;
        return clip != null ? Mathf.RoundToInt(clip.length * clip.frameRate) : 0;
    }

    private static int ShapeRows(HitShapeType shape) => shape switch
    {
        HitShapeType.Rectangle => 3,
        _                      => 2,
    };
}
