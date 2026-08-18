using UnityEngine;

public class Rocket : MonoBehaviour
{
    [Header("直進飛行の設定")]
    [Tooltip("パワーゲージに関わらず一定にする飛行速度")]
    [SerializeField] private float fixedSpeed = 20f; 

    private Rigidbody rb;
    private bool isFlyingStraight = false;
    private Vector3 flightDirection;

    void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
    }

    void OnEnable()
    {
        isFlyingStraight = false;
    }

    void OnDisable()
    {
        if (rb != null) rb.useGravity = true;
        isFlyingStraight = false;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (!isFlyingStraight)
        {
            if (!rb.isKinematic && rb.linearVelocity.magnitude > 0.1f)
            {
                // 💡 1. Y軸（上下）の速度を削り、地面と平行な（水平）ベクトルを作る
                Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

                // 万が一、真上や真下に投げられた場合はプレイヤーの前方を水平方向とする
                if (horizontalVelocity.sqrMagnitude < 0.01f)
                {
                    horizontalVelocity = new Vector3(transform.forward.x, 0f, transform.forward.z);
                }

                flightDirection = horizontalVelocity.normalized;

                // 💡 2. ロケットの向き（グラフィック）も地面と平行な飛行方向に合わせる
                if (flightDirection != Vector3.zero)
                {
                    rb.rotation = Quaternion.LookRotation(flightDirection);
                }

                rb.useGravity = false;
                isFlyingStraight = true;
            }
            return;
        }

        // 💡 3. パワーゲージの値（投擲速度）に影響されず、指定した fixedSpeed で直進させる
        rb.linearVelocity = flightDirection * fixedSpeed;
        rb.angularVelocity = Vector3.zero;
    }

    public void OnImpact()
    {
        if (!isFlyingStraight) return;

        isFlyingStraight = false;
        if (rb != null) rb.useGravity = true;
    }
}