using UnityEngine;

public enum MolkkyType
{
    Normal,
    Bomb,
    Rocket
}

public class MolkkyItemHandler : MonoBehaviour
{
    [Header("各モルックの見た目オブジェクト（子要素）")]
    public GameObject normalModel;
    public GameObject bombModel;
    public GameObject rocketModel;

    public MolkkyType currentType = MolkkyType.Normal;

    // モルックのタイプを変更する関数
    public void SetMolkkyType(MolkkyType type)
    {
        currentType = type;

        // すべて非表示にしてから該当するものだけ表示
        if (normalModel != null) normalModel.SetActive(type == MolkkyType.Normal);
        if (bombModel != null) bombModel.SetActive(type == MolkkyType.Bomb);
        if (rocketModel != null) rocketModel.SetActive(type == MolkkyType.Rocket);
    }
}