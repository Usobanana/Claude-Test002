using UnityEditor;
using UnityEngine;

/// <summary>
/// HitEventData の Inspector 表示。
/// ・総フレーム数を判定タイミングフィールドの隣に表示
/// ・shapeType に応じて対応する設定項目だけを表示
/// </summary>
[CustomPropertyDrawer(typeof(HitEventData))]
public class HitEventDataDrawer : PropertyDrawer
{
    private const float LineH   = 18f;
    private const float Spacing = 2f;
    private const float Pad     = 4f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return LineH;

        var shapeType = (HitShapeType)property.FindPropertyRelative("shapeType").enumValueIndex;
        int lines = 3            // hitStartFrame, hitEndFrame, shapeType
                  + ShapeLines(shapeType)
                  + 1            // damageRate
                  + 4            // hitStop x2 + shake x2
                  + 2            // knockback
                  + 3            // SE x3
                  + 1;           // header
        return lines * (LineH + Spacing) + Pad * 2;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        int totalFrames = GetTotalFrames(property);

        // ── ヘッダー（折りたたみ） ──
        var headerRect = new Rect(position.x, position.y + Pad, position.width, LineH);
        property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(headerRect, property.isExpanded,
            $"Hit Event  [start:{property.FindPropertyRelative("hitStartFrame").intValue}  end:{property.FindPropertyRelative("hitEndFrame").intValue}  / {totalFrames}f]");
        EditorGUI.EndFoldoutHeaderGroup();

        if (!property.isExpanded) return;

        EditorGUI.indentLevel++;
        float y = headerRect.yMax + Spacing;

        // ── 判定タイミング ──
        y = DrawFrameField(position, y, property.FindPropertyRelative("hitStartFrame"), "判定開始フレーム", totalFrames);
        y = DrawFrameField(position, y, property.FindPropertyRelative("hitEndFrame"),   "判定終了フレーム",   totalFrames);

        // ── 形状 ──
        y = DrawProp(position, y, property.FindPropertyRelative("shapeType"), "判定形状");

        var shapeType = (HitShapeType)property.FindPropertyRelative("shapeType").enumValueIndex;
        y = DrawShapeFields(position, y, property, shapeType);

        // ── ダメージ ──
        y = DrawProp(position, y, property.FindPropertyRelative("damageRate"), "ダメージ倍率 (%)");

        // ── 打感 ──
        y = DrawProp(position, y, property.FindPropertyRelative("hitStopTimeScale"),  "HitStop タイムスケール");
        y = DrawProp(position, y, property.FindPropertyRelative("hitStopDuration"),   "HitStop 時間");
        y = DrawProp(position, y, property.FindPropertyRelative("hitShakeMagnitude"), "カメラシェイク 強度");
        y = DrawProp(position, y, property.FindPropertyRelative("hitShakeDuration"),  "カメラシェイク 時間");

        // ── ノックバック ──
        y = DrawProp(position, y, property.FindPropertyRelative("knockbackForce"),    "ノックバック 距離");
        y = DrawProp(position, y, property.FindPropertyRelative("knockbackDuration"), "ノックバック 時間");

        // ── SE ──
        y = DrawProp(position, y, property.FindPropertyRelative("swingSE"),      "振り始めSE");
        y = DrawFrameField(position, y, property.FindPropertyRelative("swingSEFrame"), "振り始めSE フレーム（-1=判定開始と同じ）", totalFrames);
        y = DrawProp(position, y, property.FindPropertyRelative("hitSE"),        "ヒット SE");

        EditorGUI.indentLevel--;
    }

    private float DrawShapeFields(Rect pos, float y, SerializedProperty prop, HitShapeType shape)
    {
        switch (shape)
        {
            case HitShapeType.Arc:
                y = DrawProp(pos, y, prop.FindPropertyRelative("arcRange"), "Arc 距離");
                y = DrawProp(pos, y, prop.FindPropertyRelative("arcAngle"), "Arc 角度");
                break;
            case HitShapeType.Rectangle:
                y = DrawProp(pos, y, prop.FindPropertyRelative("rectLength"),        "矩形 奥行き");
                y = DrawProp(pos, y, prop.FindPropertyRelative("rectWidth"),         "矩形 幅");
                y = DrawProp(pos, y, prop.FindPropertyRelative("rectForwardOffset"), "矩形 前方オフセット");
                break;
            case HitShapeType.Circle:
                y = DrawProp(pos, y, prop.FindPropertyRelative("circleRadius"), "円 半径");
                y = DrawProp(pos, y, prop.FindPropertyRelative("circleOffset"), "円 オフセット");
                break;
        }
        return y;
    }

    private static float DrawProp(Rect pos, float y, SerializedProperty prop, string label)
    {
        var rect = new Rect(pos.x, y, pos.width, LineH);
        EditorGUI.PropertyField(rect, prop, new GUIContent(label));
        return y + LineH + Spacing;
    }

    private static float DrawFrameField(Rect pos, float y, SerializedProperty prop, string label, int totalFrames)
    {
        var rect      = new Rect(pos.x, y, pos.width - 70f, LineH);
        var suffixRect = new Rect(rect.xMax + 2f, y, 68f, LineH);

        EditorGUI.PropertyField(rect, prop, new GUIContent(label));

        var prevColor = GUI.color;
        GUI.color = new Color(0.7f, 0.9f, 0.7f, 1f);
        EditorGUI.LabelField(suffixRect, totalFrames > 0 ? $"/ {totalFrames} f" : "/ ? f");
        GUI.color = prevColor;

        return y + LineH + Spacing;
    }

    private static int GetTotalFrames(SerializedProperty hitEventProp)
    {
        // "steps.Array.data[N].hitEvents.Array.data[M]" → "steps.Array.data[N].clip"
        string path = hitEventProp.propertyPath;
        int idx = path.LastIndexOf(".hitEvents");
        if (idx < 0) return 0;

        string stepPath = path.Substring(0, idx);
        var clipProp    = hitEventProp.serializedObject.FindProperty(stepPath + ".clip");
        var clip        = clipProp?.objectReferenceValue as AnimationClip;
        return clip != null ? Mathf.RoundToInt(clip.length * clip.frameRate) : 0;
    }

    private static int ShapeLines(HitShapeType shape) => shape switch
    {
        HitShapeType.Arc       => 2,
        HitShapeType.Rectangle => 3,
        HitShapeType.Circle    => 2,
        _                      => 2,
    };
}
