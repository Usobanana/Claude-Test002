# Codex-Test002

## プロジェクト概要
Unity製アクションRPG「iris」の開発リポジトリ。
Unityプロジェクトは `iris/` フォルダ以下。

## コミュニケーション
- 日本語で対話する

## 開発ルール
- コードの変更前に必ずファイルを読む
- 不要なファイルは作成しない
- セキュリティに配慮したコードを書く
- mainブランチで直接作業する（個人開発のためワークツリー不使用）

## シーン構成
`TitleScene → BaseScene → FieldScene → ResultScene → BaseScene`

## 入力設計
各インタラクションはキーボード `E` / コントローラー `B` / モバイルタッチに対応。
`InteractionPromptUI`（シングルトン）が各シーンに配置されている。

## シングルトン設計の注意
`GameManager` / `SceneLoader` / `InputHandler` / `AudioManager` / `EffectManager` は DontDestroyOnLoad。
重複時は必ず `Destroy(this)`（コンポーネントのみ）を使うこと。
`Destroy(gameObject)` は同居コンポーネントも消えるため禁止。

## Syntyアセットについて
`iris/Assets/Synty/` は `.gitignore` で除外されており CI 環境に存在しない。
Syntyの型（`PropBoneBinder`, `PropBoneConfig` 等）を使うコードは必ず `#if SYNTY_PROP_BONE_TOOL` で囲むこと。
ローカルで有効化: `Edit > Project Settings > Player > Scripting Define Symbols` に `SYNTY_PROP_BONE_TOOL` を追加。

## CI/CD（GitHub Actions）
- Windows / Android ビルド → CI自動実行（game-ci）
- WebGL → ローカルビルドして `docs/webgl/` にコミット → GitHub Pages 自動デプロイ
- 詳細は [docs/handover.md](docs/handover.md) を参照

## フォント
日本語フォント: `Assets/Fonts/NotoSansJP-VariableFont_wght SDF.asset`
TMP Settings のデフォルトフォントに設定済み。
