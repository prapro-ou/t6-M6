using UnityEngine;

public class Rocket : MonoBehaviour
{
    [Header("直進飛行の設定")]
    [Tooltip("投げた瞬間の速度に対する倍率（1で速度そのまま維持）")]
    [SerializeField] private float speedMultiplier = 1f;

    private Rigidbody rb;
    private bool isFlyingStraight = false;
    private Vector3 flightDirection;
    private float flightSpeed;

    void Start()
    {
        // Rocketモデルの親（本体）についているRigidbodyを取得
        rb = GetComponentInParent<Rigidbody>();
    }

    void OnEnable()
    {
        isFlyingStraight = false;
    }

    void OnDisable()
    {
        // 他のタイプに切り替わったら重力設定を元に戻す
        if (rb != null) rb.useGravity = true;
        isFlyingStraight = false;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (!isFlyingStraight)
        {
            // 投げられて速度が発生した瞬間（＝発射の瞬間）を検知
            if (!rb.isKinematic && rb.linearVelocity.magnitude > 0.1f)
            {
                flightDirection = rb.linearVelocity.normalized;
                flightSpeed = rb.linearVelocity.magnitude * speedMultiplier;
                rb.useGravity = false;
                isFlyingStraight = true;
            }
            return;
        }

        // 重力を無視し、発射方向へ等速でまっすぐ飛ばす
        rb.linearVelocity = flightDirection * flightSpeed;
        rb.angularVelocity = Vector3.zero;
    }

    // 何かに衝突した瞬間にMolkkyItemHandlerから呼ばれる（直進飛行を終了し通常の物理挙動に戻す）
    public void OnImpact()
    {
        if (!isFlyingStraight) return;

        isFlyingStraight = false;
        if (rb != null) rb.useGravity = true;
    }
}
