using UnityEngine;

/// <summary>
/// トップダウンビュー用カメラ追従スクリプト
/// プレイヤーを中心に一定オフセットを維持しながら追従する
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3   offset     = new Vector3(0f, 15f, -8f);
    [SerializeField] private float     smoothTime = 0.1f;

    private Vector3 velocity;

    void LateUpdate()
    {
        if (target == null)
        {
            // Playerタグから自動取得
            var player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
            return;
        }

        Vector3 targetPos = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position, targetPos, ref velocity, smoothTime
        );
    }
}
