using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// アニメーションデバッグメニュー
/// F12 キーで表示/非表示を切り替え。
/// 全レイヤーの再生中クリップ名・現在フレーム/総フレームを Game ビューに表示する。
/// </summary>
public class DebugAnimationMenu : MonoBehaviour
{
    // ─────────────────────────────────────────
    // 設定
    // ─────────────────────────────────────────

    [Header("表示設定")]
    [SerializeField] private int   fontSize    = 14;
    [SerializeField] private float panelWidth  = 420f;
    [SerializeField] private float panelX      = 10f;
    [SerializeField] private float panelY      = 10f;

    // ─────────────────────────────────────────
    // 内部状態
    // ─────────────────────────────────────────

    private bool     isVisible = false;
    private Animator playerAnimator;
    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private GUIStyle headerStyle;

    // ─────────────────────────────────────────
    // Unity ライフサイクル
    // ─────────────────────────────────────────

    void Update()
    {
        // F12 でトグル（New Input System 経由）
        if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
            isVisible = !isVisible;

        // Animator を毎回探さず、nullになったときだけ再取得
        if (isVisible && playerAnimator == null)
            FindPlayerAnimator();
    }

    void OnGUI()
    {
        if (!isVisible || playerAnimator == null) return;

        InitStyles();

        float lineH  = fontSize + 6f;
        int   layers = playerAnimator.layerCount;
        float height = lineH * (2 + layers * 4) + 16f;  // ヘッダー + レイヤー分

        var rect = new Rect(panelX, panelY, panelWidth, height);
        GUI.Box(rect, GUIContent.none, boxStyle);

        float y = panelY + 8f;
        float x = panelX + 8f;
        float w = panelWidth - 16f;

        // タイトル
        GUI.Label(new Rect(x, y, w, lineH), "▶ Animation Debug  [F12 で閉じる]", headerStyle);
        y += lineH + 2f;

        // 区切り線代わりに空行
        GUI.Label(new Rect(x, y, w, 2f), "─────────────────────────────────────────", labelStyle);
        y += lineH * 0.6f;

        for (int i = 0; i < layers; i++)
        {
            var stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(i);
            var clipInfos = playerAnimator.GetCurrentAnimatorClipInfo(i);
            float layerWeight = playerAnimator.GetLayerWeight(i);

            // レイヤー名
            string layerName = playerAnimator.GetLayerName(i);
            GUI.Label(new Rect(x, y, w, lineH),
                $"<b>Layer {i}: {layerName}</b>  (weight: {layerWeight:F2})", headerStyle);
            y += lineH;

            if (clipInfos.Length == 0)
            {
                GUI.Label(new Rect(x + 8f, y, w - 8f, lineH), "  ─ クリップなし", labelStyle);
                y += lineH;
            }
            else
            {
                foreach (var ci in clipInfos)
                {
                    var clip = ci.clip;
                    if (clip == null) continue;

                    float fps        = clip.frameRate;
                    float totalSec   = clip.length;
                    int   totalFrames = Mathf.RoundToInt(totalSec * fps);

                    // normalizedTime は 1 を超えることがあるのでループ内位置に変換
                    float normalizedLoop = stateInfo.normalizedTime % 1.0f;
                    int   currentFrame   = Mathf.FloorToInt(normalizedLoop * totalFrames);

                    GUI.Label(new Rect(x + 8f, y, w - 8f, lineH),
                        $"  Clip : {clip.name}", labelStyle);
                    y += lineH;
                    GUI.Label(new Rect(x + 8f, y, w - 8f, lineH),
                        $"  Frame: {currentFrame} / {totalFrames}  ({fps} fps)  norm: {normalizedLoop:F3}", labelStyle);
                    y += lineH;
                }
            }

            // レイヤー間の余白
            y += 2f;
        }
    }

    // ─────────────────────────────────────────
    // 内部ヘルパー
    // ─────────────────────────────────────────

    private void FindPlayerAnimator()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null)
            playerAnimator = player.GetComponentInChildren<Animator>();
    }

    private void InitStyles()
    {
        if (boxStyle != null) return;

        boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.75f)) }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = fontSize,
            richText  = true,
            normal    = { textColor = Color.white }
        };

        headerStyle = new GUIStyle(labelStyle)
        {
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.4f, 1f, 0.6f) }
        };
    }

    private static Texture2D MakeTex(int w, int h, Color col)
    {
        var tex    = new Texture2D(w, h);
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
