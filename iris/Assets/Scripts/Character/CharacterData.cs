using UnityEngine;

/// <summary>
/// キャラクターの固有データを定義するScriptableObject
/// 各キャラクター（戦士・騎士・射手）ごとにアセットを作成する
/// </summary>
[CreateAssetMenu(fileName = "CharacterData", menuName = "Game/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("基本情報")]
    public string characterName;
    public CharacterRole role;
    public ElementType element;
    public Sprite portrait;

    [Header("初期ステータス")]
    public CharacterStats baseStats;

    [Header("攻撃設定")]
    public float attackRange = 3f;       // オートアタック範囲（半径）
    public float attackInterval = 1f;    // 攻撃間隔（秒）

    [Header("武器種")]
    public WeaponType weaponType;

    [Header("ビジュアル")]
    [Tooltip("このキャラクターの3Dモデル Prefab（未設定なら差し替えなし）")]
    public GameObject modelPrefab;
    [Tooltip("このキャラクターのデフォルト武器モデル Prefab（未設定なら PlayerAppearance の weaponPrefab を使用）")]
    public GameObject weaponModelPrefab;

    [Header("解放設定")]
    public UnlockType unlockType;
}

public enum CharacterRole
{
    Attacker,   // 近接・中距離アタッカー
    Tank,       // タンク
}

public enum ElementType
{
    Fire,
    Water,
    Wind,
    Light,
    Dark,
}

public enum WeaponType
{
    OneHandedSword,
    OneHandedSwordAndShield,
    Bow,
}

public enum UnlockType
{
    Default,    // 最初から選択可能
    Gacha,      // ガチャで入手（将来実装）
}
