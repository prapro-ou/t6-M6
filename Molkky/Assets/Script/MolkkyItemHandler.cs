using UnityEngine;

public enum MolkkyType
{
    Normal,
    Bomb,
    Rocket,
    Darkness,
    Wind,
    MovingWall
}

public class MolkkyItemHandler : MonoBehaviour
{
    [Header("各モルックの見た目オブジェクト（子要素）")]
    public GameObject normalModel;
    public GameObject bombModel;
    public GameObject rocketModel;

    public MolkkyType currentType = MolkkyType.Normal;

    // ★現在、地面やスキットルなど何かに接触しているかどうか
    //   （接触数をカウントするのは、複数のコライダーに同時接触していても
    //     どれか1つが離れただけで「非接地」にならないようにするため）
    //   さらに、物理エンジンの都合で接触が1フレームだけ途切れることがあるため、
    //   groundedDebounce秒以内の再接触は「接地し続けていた」ものとして扱う
    [SerializeField] private float groundedDebounce = 0.1f;
    private int contactCount = 0;
    private float lastContactTime = -1f;
    public bool IsGrounded => contactCount > 0 || (Time.time - lastContactTime) <= groundedDebounce;

    // ★IsGroundedが（デバウンスを挟んで）連続してtrueになっている秒数。0なら接地していない
    public float ContinuousGroundedDuration => IsGrounded ? Time.time - continuousGroundedStartTime : 0f;
    private float continuousGroundedStartTime = -1f;
    private bool wasGroundedLastCheck = false;

    // 投げた直後（まだ何にも触れていない飛行中）の状態にリセットする
    public void ResetGroundState()
    {
        contactCount = 0;
        lastContactTime = -1f;
        continuousGroundedStartTime = -1f;
        wasGroundedLastCheck = false;
    }

    private void Update()
    {
        bool grounded = IsGrounded;
        if (grounded && !wasGroundedLastCheck)
        {
            continuousGroundedStartTime = Time.time;
        }
        wasGroundedLastCheck = grounded;
    }

    // モルックのタイプを変更する関数
    public void SetMolkkyType(MolkkyType type)
    {
        currentType = type;
        bool isNormal = (type == MolkkyType.Normal || type == MolkkyType.MovingWall || type == MolkkyType.Darkness || type == MolkkyType.Wind);

        if (normalModel != null) normalModel.SetActive(isNormal);
        if (bombModel != null) bombModel.SetActive(type == MolkkyType.Bomb);
        if (rocketModel != null) rocketModel.SetActive(type == MolkkyType.Rocket);
    }

    // このGameObject（Rigidbody本体）が何かに衝突した瞬間に呼ばれる
    private void OnCollisionEnter(Collision collision)
    {
        contactCount++;
        lastContactTime = Time.time;

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

    // 接触が続いている間、毎物理フレーム呼ばれる（接地デバウンスの基準時刻を更新し続ける）
    private void OnCollisionStay(Collision collision)
    {
        lastContactTime = Time.time;
    }

    // 接触していたコライダーから離れた瞬間に呼ばれる（跳ねて再び空中に浮いた場合など）
    private void OnCollisionExit(Collision collision)
    {
        contactCount = Mathf.Max(0, contactCount - 1);
    }
}