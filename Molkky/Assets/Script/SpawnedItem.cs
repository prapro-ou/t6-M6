using UnityEngine;

public class SpawnedItem : MonoBehaviour
{
    [SerializeField] private int remainingTurns = 2;
    [SerializeField] private MeshRenderer meshRenderer;

    [Header("回転演出")]
    [SerializeField] private float spinSpeed = 90f; // その場で回転する速さ（度/秒、Y軸）

    public ItemData CurrentItemData { get; private set; }
    private bool isCollected = false;

    public void SetItemData(ItemData data)
    {
        CurrentItemData = data;

        if (meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        if (data != null && data.visualModelPrefab != null)
        {
            // ★専用モデルが指定されている場合：デフォルトの色付き球は隠し、代わりにモデルを表示する
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }

            // 元の土台はコイン状に潰すため(1, 0.05, 1)のスケールになっており、
            // そのままモデルを子にすると平べったく潰れてしまうため均一スケールに戻す
            transform.localScale = Vector3.one;

            GameObject visual = Instantiate(data.visualModelPrefab, transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(data.visualModelRotationOffset);
            visual.transform.localScale *= data.visualModelScale;

            // フィールド上の表示物には不要な、当たり判定や専用の挙動スクリプトは取り除く
            foreach (Collider col in visual.GetComponentsInChildren<Collider>())
            {
                Destroy(col);
            }
            foreach (BombImpact bomb in visual.GetComponentsInChildren<BombImpact>())
            {
                Destroy(bomb);
            }

            // ロケットの噴射エフェクトなど、モデルに付属するパーティクルはPlay On Awakeで
            // 自動再生されてしまうため、フィールドに置いてあるだけの間は出ないよう止めておく
            foreach (ParticleSystem ps in visual.GetComponentsInChildren<ParticleSystem>())
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            }

            // モデルごとに半径（大きさ）が異なり、中心をそのまま地面の高さに置くと
            // 大きいモデルほど半分近く埋まってしまうため、見た目の底面がスポーン高さに
            // 接するようY方向を補正する
            AlignVisualBottomToSpawnHeight(visual);
        }
        else if (meshRenderer != null && data != null)
        {
            Material mat = meshRenderer.material;
            mat.color = data.itemColor;

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", data.itemColor);
            }
        }
    }

    // 💡 描画結果のバウンズ（見た目上の底面）を調べて、地面に埋まらないよう持ち上げる
    private void AlignVisualBottomToSpawnHeight(GameObject visual)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float liftAmount = transform.position.y - bounds.min.y;
        if (liftAmount > 0f)
        {
            visual.transform.position += new Vector3(0f, liftAmount, 0f);
        }
    }

    private void Update()
    {
        // ★カメラ方向への追従（ビルボード）はやめて、その場でY軸回転させる
        //   3Dモデル使用時にビルボードだと横から見た薄い面しか映らないため
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Trigger判定入] 接触相手: {other.gameObject.name} / Tag: '{other.tag}'");

        if (isCollected)
        {
            Debug.LogWarning("[Trigger中断] すでに取得済み(isCollected = true)です");
            return;
        }

        if (other.CompareTag("Molkky"))
        {
            if (ItemManager.Instance != null && CurrentItemData != null)
            {
                // ★1. RegisterItem を呼び出して、獲得できたかどうか(bool)を受け取る
                bool success = ItemManager.Instance.RegisterItem(CurrentItemData);

                // ★2. 登録に成功した場合（このターンで最初の1つ目だった場合）
                if (success)
                {
                    isCollected = true;
                    Debug.Log($"[獲得成功] {CurrentItemData.itemName} をゲット！消去処理を実行します。");

                    // アイテム消滅
                    Destroy(gameObject);
                }
                else
                {
                    // ★3. すでにこのターンで他のアイテムを獲得していた場合
                    Debug.Log($"[獲得失敗] 既にこのターンでアイテムを獲得しているため、{CurrentItemData.itemName} は無視されました。");

                    // ※もし「取れなくても当たったら消滅させたい」場合は、ここに Destroy(gameObject); を入れてください
                }
            }
            else
            {
                Debug.LogWarning($"[注意] RegisterItemが呼ばれませんでした (ItemManager存在: {ItemManager.Instance != null}, ItemData存在: {CurrentItemData != null})");
            }
        }
        else
        {
            Debug.LogWarning($"[タグ不一致] 接触相手のタグは '{other.tag}' でした。'Molkky' と一致しません。");
        }
    }

    public void OnTurnPassed()
    {
        remainingTurns--;
        if (remainingTurns <= 0)
        {
            Destroy(gameObject);
        }
    }
}