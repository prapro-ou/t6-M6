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


    [Header("爆発音")]
    [SerializeField] private AudioSource explosionAudioSource;
    [SerializeField] private AudioClip explosionSound;


    private bool exploded = false;
    private Renderer targetRenderer;
    private Color originalColor;

    // 爆発した瞬間に通知するイベント（GameManagerが戻りタイマーの起点として使う）
    public event System.Action OnExploded;

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

        // 0.8秒後の爆発シーケンスを開始
        StartCoroutine(ExplodeSequence());
    }

    private IEnumerator ExplodeSequence()
    {
        Debug.Log("[Bomb] 着地！爆発カウントダウン...");

        // 0.8秒間のカウントダウン中に赤色で高速点滅（光る演出）
        float elapsedTime = 0f;
        float blinkInterval = 0.1f; // 点滅の間隔（秒）
        bool isRed = false;

        while (elapsedTime < fuseTime)
        {
            if (targetRenderer != null)
            {
                targetRenderer.material.color = isRed ? originalColor : Color.red;
                isRed = !isRed;
            }

            yield return new WaitForSeconds(blinkInterval);
            elapsedTime += blinkInterval;
        }

        Debug.Log("[Bomb] 爆発！");
        if (explosionSound != null)
        {
            Debug.Log("[Bomb] 爆発音を再生します！");
            AudioSource.PlayClipAtPoint(
                explosionSound,
                transform.position,
                5f
            );
        }
        
        OnExploded?.Invoke();

        // ★1. 爆発の瞬間にカメラシェイクを呼び出す
        if (Camera.main != null)
        {
            StartCoroutine(ShakeCamera(0.3f, 0.4f)); // (揺らす時間秒, 揺れの強さ)
        }

        // ★2. 爆発エフェクトの生成
        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // ★3. 周囲のピン（モルック以外）を吹き飛ばす
        Collider[] targets = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider target in targets)
        {
            if (target.gameObject == gameObject)
                continue;

            Rigidbody rb = target.attachedRigidbody;
            if (rb == null)
                continue;

            rb.AddExplosionForce(force, transform.position, radius, upwardsModifier, ForceMode.Impulse);
        }

        // 💡 4. 爆弾自体の見た目と当たり判定を非表示（即座に消えたように見せる）
        if (targetRenderer != null) targetRenderer.enabled = false;
        Collider myCollider = GetComponent<Collider>();
        if (myCollider != null) myCollider.enabled = false;

        // 💡 5. カメラシェイク（0.3秒）が終わるまで待つ！
        yield return new WaitForSeconds(0.3f);

        // 💡 6. 揺れが完了してから安全に自分自身を消去
        Destroy(gameObject);
    }

    // ★★★ エラーの原因になっていたカメラシェイク処理本体 ★★★
    private IEnumerator ShakeCamera(float duration, float magnitude)
    {
        Vector3 originalPos = Camera.main.transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            Camera.main.transform.localPosition = originalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Camera.main.transform.localPosition = originalPos;
    }
}