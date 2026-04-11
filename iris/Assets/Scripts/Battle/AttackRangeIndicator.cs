using UnityEngine;

/// <summary>
/// プレイヤー攻撃範囲を LineRenderer で円描画するコンポーネント。
/// AUTO ON 時のみ表示。AutoAttackSystem.SetAutoMode() から SetVisible() で制御される。
/// </summary>
[RequireComponent(typeof(CharacterEntity))]
public class AttackRangeIndicator : MonoBehaviour
{
    [Header("円描画設定")]
    [SerializeField] private int   segments  = 48;
    [SerializeField] private float yOffset   = 0.05f;
    [SerializeField] private Color lineColor = new Color(1f, 0.9f, 0.2f, 0.7f);
    [SerializeField] private float lineWidth = 0.04f;

    private LineRenderer lr;
    private CharacterEntity entity;

    void Awake()
    {
        entity = GetComponent<CharacterEntity>();

        lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace    = true;
        lr.loop             = true;
        lr.positionCount    = segments;
        lr.startWidth       = lineWidth;
        lr.endWidth         = lineWidth;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows   = false;

        // マテリアル（デフォルトの Sprites-Default で代用）
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lineColor;
        lr.endColor   = lineColor;

        lr.enabled = false;
    }

    void LateUpdate()
    {
        if (!lr.enabled || entity?.Data == null) return;
        DrawCircle(entity.Data.attackRange);
    }

    /// <summary>表示 ON/OFF を切り替える</summary>
    public void SetVisible(bool visible)
    {
        if (lr != null) lr.enabled = visible;
    }

    private void DrawCircle(float radius)
    {
        float angleStep = 360f / segments;
        Vector3 center  = transform.position;
        center.y       += yOffset;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x     = center.x + Mathf.Cos(angle) * radius;
            float z     = center.z + Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, center.y, z));
        }
    }
}
