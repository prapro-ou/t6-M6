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
    Rocket,       // 次に投げるモルックがロケットになる
    Wind,         // 風が起こる
    Darkness,     // 相手の次のターンが暗闇になる（既存の値を維持するため末尾に追加）
    MovingWall,   // 壁を発生
    AllSkittles   // ★レアアイテム: 次に投げたモルックが着地した瞬間、場の全スキットルを倒す（末尾に追加）
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

    [Tooltip("出現しやすさの重み。数値が大きいほど抽選されやすくなる（全アイテム同じ値なら完全ランダムと同じ）")]
    public float spawnWeight = 1f;

    [Header("見た目上書き（任意）")]
    [Tooltip("設定すると、色付きの球の代わりにこのモデルをフィールド上のアイテムとして表示する")]
    public GameObject visualModelPrefab;
    [Tooltip("上記モデルの元のサイズに掛ける倍率（フィールド上の表示サイズ調整用）")]
    public float visualModelScale = 1f;
    [Tooltip("上記モデルの傾き（オイラー角）。Y軸回転はその場回転の演出と別に、この角度を保ったまま回る")]
    public Vector3 visualModelRotationOffset = Vector3.zero;

    [Header("出現エフェクト（任意）")]
    [Tooltip("設定すると、アイテムがフィールドに出現した瞬間にこのパーティクルエフェクトを再生する。パーティクル側のStop ActionをDestroyにしておくと再生後に自動で消える")]
    public GameObject spawnEffectPrefab;
}