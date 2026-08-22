using UnityEngine;

// アイテムの効果タイプ一覧
// ★数値を明示しているのは、既存のItemData(.asset)がこの数値でシリアライズされているため。
//   歯抜けのまま維持しないと、既存アイテムの効果が別の効果にズレてしまう。
public enum ItemEffectType
{
    Bomb = 5,         // 次に投げるモルックがボムになる
    Rocket = 6,       // 次に投げるモルックがロケットになる
    Wind = 7,         // 風が起こる
    Darkness = 8,     // 相手の次のターンが暗闇になる
    MovingWall = 9,   // 壁を発生
    AllSkittles = 10  // ★レアアイテム: 取得した人がもう1ターン連続でプレイできる
}

// Unityの右クリックメニューからこのデータを作成できるようにする属性
[CreateAssetMenu(fileName = "NewItemData", menuName = "Mölkky/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemId;                 // 例: "Bomb", "AllSkittles" など
    public string itemName;               // 例: "得点2倍"
    public Color itemColor = Color.white; // 5色のカラー
    [TextArea] public string description; // 説明文

    public ItemEffectType effectType;     // ★ 追加: ItemManagerとの連携に必要な効果識別子

    [Tooltip("出現しやすさの重み。数値が大きいほど抽選されやすくなる（全アイテム同じ値なら完全ランダムと同じ）")]
    public float spawnWeight = 1f;

    [Tooltip("チェックを入れると、1ゲーム中に1回スポーンしたら以降は抽選対象から外れる（レアアイテム用）")]
    public bool isRare = false;

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