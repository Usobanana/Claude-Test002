using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// 日本語フォント（Noto Sans JP）の TMP Font Asset を生成し
/// TMP Settings のデフォルトフォントに設定する
/// Game/Setup Japanese Font を実行するだけでOK
/// </summary>
public static class JapaneseFontSetupTool
{
    private const string FontPath      = "Assets/Fonts/NotoSansJP-VariableFont_wght.ttf";
    private const string FontAssetPath = "Assets/Fonts/NotoSansJP SDF.asset";
    private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

    [MenuItem("Game/Setup Japanese Font")]
    public static void SetupJapaneseFont()
    {
        // 1. フォントファイル読み込み
        var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (font == null)
        {
            Debug.LogError($"[JapaneseFontSetup] フォントが見つかりません: {FontPath}");
            return;
        }

        // 2. 既存の Font Asset があれば再利用、なければ生成
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset == null)
        {
            fontAsset = TMP_FontAsset.CreateFontAsset(font);
            fontAsset.name = "NotoSansJP SDF";
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[JapaneseFontSetup] Font Asset 生成: {FontAssetPath}");
        }
        else
        {
            Debug.Log($"[JapaneseFontSetup] 既存の Font Asset を使用: {FontAssetPath}");
        }

        // 3. TMP Settings のデフォルトフォントを設定
        var settingsAsset = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        if (settingsAsset == null)
        {
            Debug.LogError($"[JapaneseFontSetup] TMP Settings が見つかりません: {TmpSettingsPath}");
            Debug.Log("[JapaneseFontSetup] Window > TextMeshPro > Settings を開いて確認してください");
            return;
        }

        var so = new SerializedObject(settingsAsset);
        var defaultFontProp = so.FindProperty("m_defaultFontAsset");
        if (defaultFontProp != null)
        {
            defaultFontProp.objectReferenceValue = fontAsset;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settingsAsset);
            AssetDatabase.SaveAssets();
            Debug.Log("[JapaneseFontSetup] TMP Settings のデフォルトフォントを NotoSansJP SDF に設定しました");
        }
        else
        {
            Debug.LogWarning("[JapaneseFontSetup] m_defaultFontAsset プロパティが見つかりません。手動で設定してください。");
            Debug.Log($"  1. Window > TextMeshPro > Settings を開く");
            Debug.Log($"  2. Default Font Asset に '{FontAssetPath}' を設定する");
        }

        Debug.Log("[JapaneseFontSetup] 完了！既存のTMPテキストには手動でフォントを再設定してください。");
        Debug.Log("  ヒント: 各TextMeshProUGUIのFont Assetをクリアすると自動でデフォルトが適用されます。");
    }
}
