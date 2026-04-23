# 申し送り事項

## CI/CDビルド（GitHub Actions）

### ワークフロー構成
`.github/workflows/unity-build.yml` にて以下を自動実行（mainプッシュ or 手動）：
- **WebGL ビルド** → GitHub Pages 自動デプロイ
- **Windows ビルド** → Artifact として14日間保存
- **Android ビルド** → APK を Artifact として14日間保存

### 必要な GitHub Secrets
| Secret名 | 内容 | 取得場所 |
|---------|------|---------|
| `UNITY_LICENSE` | `.ulf` ファイルの全内容 | `C:\ProgramData\Unity\Unity_lic.ulf` |
| `UNITY_EMAIL` | Unity アカウントのメール | Unity アカウント |
| `UNITY_PASSWORD` | Unity アカウントのパスワード | Unity アカウント |

> `.ulf` ファイルは Unity Hub で Personal ライセンスを有効化すると生成される。

---

## Synty アセットの扱い

### 状況
`iris/Assets/Synty/` は `.gitignore` で除外されており、CI環境に存在しない。

### 対応済みファイル
Syntyの型（`PropBoneBinder`, `PropBoneConfig`）を使うコードは `#if SYNTY_PROP_BONE_TOOL` で条件付きコンパイル済み：
- `Assets/Scripts/Character/PlayerAppearance.cs`
- `Assets/Scripts/Editor/PlayerAppearanceEditor.cs`

### ローカルでSynty機能を有効にする方法
`Edit > Project Settings > Player > Scripting Define Symbols` に `SYNTY_PROP_BONE_TOOL` を追加する。

### 新たにSynty依存コードを書く場合
```csharp
#if SYNTY_PROP_BONE_TOOL
using Synty.Tools.SyntyPropBoneTool;
#endif

// ...

#if SYNTY_PROP_BONE_TOOL
    // Syntyに依存する処理
#endif
```

---

## シーン構成・入力設計

### シーン遷移フロー
`TitleScene → BaseScene → FieldScene → ResultScene → BaseScene`

### 入力対応
各インタラクション（クエスト受理・フィールド出発・拠点へ戻る）は以下に対応：
- キーボード: `E` キー
- コントローラー: `B` ボタン（Xbox）/ `×`（PS）
- モバイル: `InteractionPromptUI` のボタンタッチ

### シングルトン設計の注意
`GameManager` / `SceneLoader` / `InputHandler` / `AudioManager` / `EffectManager` は DontDestroyOnLoad。
重複時は `Destroy(this)`（コンポーネントのみ）で対応済み。`Destroy(gameObject)` にすると同居コンポーネントが消えるため禁止。

---

## フォント
日本語フォント: `Assets/Fonts/NotoSansJP-VariableFont_wght SDF.asset`
TMP Settings のデフォルトフォントに設定済み。
