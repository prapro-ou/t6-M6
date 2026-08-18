using UnityEngine;

public class SpawnedItem : MonoBehaviour
{
    [SerializeField] private int remainingTurns = 2;
    [SerializeField] private MeshRenderer meshRenderer;

    public ItemData CurrentItemData { get; private set; }
    private bool isCollected = false;

    public void SetItemData(ItemData data)
    {
        CurrentItemData = data;

        if (meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        if (meshRenderer != null && data != null)
        {
            Material mat = meshRenderer.material;
            mat.color = data.itemColor;

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", data.itemColor);
            }
        }
    }

    private void Update()
    {
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                             Camera.main.transform.rotation * Vector3.up);
            transform.Rotate(90f, 0f, 0f, Space.Self);
        }
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