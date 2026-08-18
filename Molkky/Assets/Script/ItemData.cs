using UnityEngine;

// アイテムの効果タイプ一覧
public enum ItemEffectType
{
    ScoreDouble,  // 得点2倍
    BigMolkky,    // モルック巨大化
    SmallMolkky,  // モルック小型化
    SkittleGroup, // スキットル密集
    SkittleSpread,// スキットル分散
    Bomb,         // 次に投げるモルックがボムになる
    Rocket        // 次に投げるモルックがロケットになる
}

// Unityの右クリックメニューからこのデータを作成できるようにする属性
[CreateAssetMenu(fileName = "NewItemData", menuName = "Mölkky/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemId;                 // 例: "ScoreDouble", "BigMolkky" など
    public string itemName;               // 例: "得点2倍"
    public Color itemColor = Color.white; // 5色のカラー
    [TextArea] public string description; // 説明文

    public ItemEffectType effectType;     // ★ 追加: ItemManagerとの連携に必要な効果識別子
}