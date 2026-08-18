using System.Collections;
using UnityEngine;

public class BombImpact : MonoBehaviour
{
    [SerializeField] private float radius = 100f; // 爆発範囲
    [SerializeField] private float force = 15f;
    [SerializeField] private float upwardsModifier = 0.5f; // 爆風で少し宙に浮かせる力
    [SerializeField] private float fuseTime = 1.0f; // 着地から爆発までの時間（秒）

    [Header("爆発エフェクト")]
    [SerializeField] private GameObject explosionEffectPrefab;

    private bool exploded = false;
    private Renderer targetRenderer;
    private Color originalColor;

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            // 元のマテリアルの色を記録しておく
            originalColor = targetRenderer.material.color;
        }
    }

    // モルックを投げた瞬間にGameManagerから呼ばれる
    public void Arm()
    {
        exploded = false;
        if (targetRenderer != null)
        {
            targetRenderer.material.color = originalColor;
        }
    }

    // 衝突を検知した側（MolkkyItemHandler）から呼び出す
    public void Explode()
    {
        if (exploded) return;
        exploded = true;

        // 1秒後の爆発シーケンスを開始
        StartCoroutine(ExplodeSequence());
    }

    private IEnumerator ExplodeSequence()
    {
        Debug.Log("[Bomb] 着地！0.8秒後に爆発します...");

        // 1. 0.8秒間のカウントダウン中に赤色で高速点滅（光る演出）
        float elapsedTime = 0f;
        float blinkInterval = 0.1f; // 点滅の間隔（秒）
        bool isRed = false;

        while (elapsedTime < fuseTime)
        {
            if (targetRenderer != null)
            {
                // 赤と元の色を交互に切り替え
                targetRenderer.material.color = isRed ? originalColor : Color.red;
                isRed = !isRed;
            }

            yield return new WaitForSeconds(blinkInterval);
            elapsedTime += blinkInterval;
        }

        Debug.Log("[Bomb] 爆発！");

        // 2. 爆発エフェクトの生成
        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        // 3. 周囲のピン（モルック以外）を吹き飛ばす
        Collider[] targets = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider target in targets)
        {
            // 自分自身は吹き飛ばし対象から除外
            if (target.gameObject == gameObject)
                continue;

            Rigidbody rb = target.attachedRigidbody;
            if (rb == null)
                continue;

            rb.AddExplosionForce(force, transform.position, radius, upwardsModifier, ForceMode.Impulse);
        }

        // 💡 4. 爆弾自身を消去
        Destroy(gameObject);
    }
}