using UnityEngine;

public enum MolkkyType
{
    Normal,
    Bomb,
    Rocket,
    Darkness,
    Wind
}

public class MolkkyItemHandler : MonoBehaviour
{
    [Header("各モルックの見た目オブジェクト（子要素）")]
    public GameObject normalModel;
    public GameObject bombModel;
    public GameObject rocketModel;

    public MolkkyType currentType = MolkkyType.Normal;

    // 投げてから一度でも何かに衝突したか（飛行中の誤停止判定を防ぐために使用）
    public bool hasLanded = false;

    // モルックのタイプを変更する関数
    public void SetMolkkyType(MolkkyType type)
    {
        currentType = type;

        // すべて非表示にしてから該当するものだけ表示
        if (normalModel != null) normalModel.SetActive(type == MolkkyType.Normal);
        if (bombModel != null) bombModel.SetActive(type == MolkkyType.Bomb);
        if (rocketModel != null) rocketModel.SetActive(type == MolkkyType.Rocket);
    }

    // このGameObject（Rigidbody本体）が何かに衝突した瞬間に呼ばれる
    private void OnCollisionEnter(Collision collision)
    {
        hasLanded = true;

        if (currentType == MolkkyType.Bomb && bombModel != null)
        {
            BombImpact bomb = bombModel.GetComponent<BombImpact>();
            if (bomb != null) bomb.Explode();
        }
        else if (currentType == MolkkyType.Rocket && rocketModel != null)
        {
            Rocket rocket = rocketModel.GetComponent<Rocket>();
            if (rocket != null) rocket.OnImpact();
        }
    }
}