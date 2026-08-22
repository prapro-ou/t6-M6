using UnityEngine;

public class FanWind : MonoBehaviour
{
    [Header("回転")]
    public float rotateSpeed = 800f;

    [Header("風")]
    public float windForce = 10f;
    public float windRange = 20f;
    public float windAngle = 30f;

    [Tooltip("演出専用に配置する場合など、風の力を実際には加えたくない時はfalseにする")]
    public bool applyWindForce = true;

    private void Update()
    {
        // 羽を回転
        transform.Rotate(
            0f,
            rotateSpeed * Time.deltaTime,
            0f,
            Space.Self
        );
    }

    private void FixedUpdate()
    {
        if (!applyWindForce) return;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            windRange
        );

        foreach (Collider hit in hits)
        {
            Rigidbody rb = hit.attachedRigidbody;

            if (rb == null)
                continue;

            Vector3 toTarget =
                hit.transform.position -
                transform.position;

            float angle =
                Vector3.Angle(
                    transform.forward,
                    toTarget
                );

            // 扇風機の角度内だけ
            if (angle <= windAngle)
            {
                float distance =
                    toTarget.magnitude;

                // 距離で減衰
                float strength =
                    1f -
                    (distance / windRange);

                rb.AddForce(
                    transform.forward *
                    windForce *
                    strength,
                    ForceMode.Force
                );
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            windRange
        );

        Vector3 left =
            Quaternion.Euler(
                0,
                -windAngle,
                0
            ) *
            transform.forward;

        Vector3 right =
            Quaternion.Euler(
                0,
                windAngle,
                0
            ) *
            transform.forward;

        Gizmos.DrawLine(
            transform.position,
            transform.position +
            left * windRange
        );

        Gizmos.DrawLine(
            transform.position,
            transform.position +
            right * windRange
        );
    }
#endif
}