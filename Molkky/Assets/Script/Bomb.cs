using UnityEngine;

public class BombImpact : MonoBehaviour
{
    [SerializeField] private float radius = 3f;
    [SerializeField] private float force = 30f;

    // この距離以上ワープしたら再使用とみなす
    [SerializeField] private float resetDistance = 1f;

    private bool exploded = false;
    private Vector3 lastPosition;

    private void Awake()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (Vector3.Distance(transform.position, lastPosition) > resetDistance)
        {
            exploded = false;
        }

        lastPosition = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (exploded) return;
        exploded = true;

        Collider[] targets = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider target in targets)
        {
            Rigidbody rb = target.attachedRigidbody;

            if (rb == null)
                continue;

            Vector3 direction =
                (rb.worldCenterOfMass - transform.position).normalized;

            rb.AddForce(direction * force, ForceMode.Impulse);
        }
    }
}
