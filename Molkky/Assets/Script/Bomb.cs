using UnityEngine;

public class BombImpact : MonoBehaviour
{
    [SerializeField] private float radius = 3f;
    [SerializeField] private float force = 30f;

    [Header("爆発エフェクト")]
    [Tooltip("爆発時に再生するパーティクルのプレハブ（Particle Systemを含むオブジェクト）")]
    [SerializeField] private GameObject explosionEffectPrefab;

    // 次のExplode()呼び出しを有効にする（一度爆発すると再度Arm()するまで爆発しない）
    private bool exploded = false;

    // モルックを投げた瞬間にGameManagerから呼ばれる
    public void Arm()
    {
        exploded = false;
    }

    // 衝突を検知した側（MolkkyItemHandler）から呼び出す
    public void Explode()
    {
        if (exploded) return;
        exploded = true;

        Debug.Log("[Bomb] 爆発します！");

        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

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
