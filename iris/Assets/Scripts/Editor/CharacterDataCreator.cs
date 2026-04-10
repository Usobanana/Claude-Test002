using UnityEngine;
using UnityEditor;

/// <summary>
/// 初期キャラクターのCharacterDataアセットを自動生成するエディタツール
/// </summary>
public static class CharacterDataCreator
{
    [MenuItem("Game/Create Initial Character Data")]
    public static void CreateInitialCharacterData()
    {
        CreateWarriorData();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CharacterDataCreator] 初期キャラクターデータ作成完了");
    }

    private static CharacterData CreateWarriorData()
    {
        var data = ScriptableObject.CreateInstance<CharacterData>();
        data.characterName = "戦士";
        data.role          = CharacterRole.Attacker;
        data.element       = ElementType.Fire;
        data.weaponType    = WeaponType.OneHandedSword;
        data.unlockType    = UnlockType.Default;
        data.attackRange   = 2.5f;
        data.attackInterval= 1.0f;

        data.baseStats = new CharacterStats
        {
            maxHp             = 1200f,
            attack            = 120f,
            defense           = 60f,
            maxStamina        = 100f,
            critRate          = 0.05f,
            critDamage        = 1.5f,
            staminaRegenPerSec= 20f,
            level             = 1,
            exp               = 0,
            expToNextLevel    = 100,
        };

        AssetDatabase.CreateAsset(data, "Assets/Data/Warrior_CharacterData.asset");
        Debug.Log("[CharacterDataCreator] 戦士データ作成: Assets/Data/Warrior_CharacterData.asset");
        return data;
    }
}
