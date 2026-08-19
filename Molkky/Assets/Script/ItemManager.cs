using UnityEngine;
using TMPro;
using System.Collections;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [Header("アイテムスポーン設定")]
    [SerializeField] private GameObject itemPrefab; // 生成するPrefab (SpawnedItemが付いたもの)
    [SerializeField] private BoxCollider spawnAreaCollider; // 長方形のスポーンエリア
    [SerializeField] private ItemData[] availableItems; // 5つのItemData

    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI noticeText;

    private ItemData reservedItem;
    private Coroutine hideTextCoroutine;

    // ★追加: 1ターンに1つのみ取得を制限するフラグ
    private bool isItemAcquiredThisTurn = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (noticeText != null)
        {
            noticeText.text = "";
        }

        SpawnItem();
    }

    // ★ItemSpawnerから移植・改良した長方形用スポーン処理
    public void SpawnItem()
    {
        // 1. 安全チェック
        if (itemPrefab == null)
        {
            Debug.LogWarning("[ItemManager] ItemPrefabが設定されていません！");
            return;
        }

        if (availableItems == null || availableItems.Length == 0)
        {
            Debug.LogWarning("[ItemManager] AvailableItemsにItemDataが設定されていません！");
            return;
        }

        // 2. スポーン位置の計算（BoxColliderの長方形エリア内）
        Vector3 spawnPosition;
        if (spawnAreaCollider != null)
        {
            Bounds bounds = spawnAreaCollider.bounds;
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);
            float surfaceY = bounds.center.y;

            spawnPosition = new Vector3(randomX, surfaceY, randomZ);
        }
        else
        {
            Debug.LogWarning("[ItemManager] SpawnAreaColliderが設定されていないため、デフォルト位置にスポーンします。");
            spawnPosition = new Vector3(0, 0.1f, 2f);
        }

        // 3. 生成とデータのセット
        GameObject spawnedObj = Instantiate(itemPrefab, spawnPosition, Quaternion.identity);
        ItemData selectedData = availableItems[Random.Range(0, availableItems.Length)];

        SpawnedItem spawnedItemScript = spawnedObj.GetComponent<SpawnedItem>();
        if (spawnedItemScript != null)
        {
            spawnedItemScript.SetItemData(selectedData);
        }
        else
        {
            Debug.LogError("[ItemManager] 生成されたPrefabに SpawnedItem スクリプトが付いていません！");
        }

        Debug.Log($"[ItemManager] アイテム ({selectedData.itemName}) をスポーンしました: {spawnPosition}");
    }

    // ★ bool を返し、すでに取得済みなら false を返す
    public bool RegisterItem(ItemData item)
    {
        // 既にこのターンで取得していれば失敗 (false)
        if (isItemAcquiredThisTurn)
        {
            Debug.Log($"[ItemManager] 取得失敗: 既にこのターンでアイテムを獲得済みです ({item.itemName})");
            return false;
        }

        isItemAcquiredThisTurn = true; // 獲得済みに更新
        ShowNotice($"{item.itemName} Get!");

        // ★Bomb/Rocketは既存のMolkkyType機構（ItemBoxと同じ仕組み）に乗せて、
        //   自分の次の投球で見た目・能力が変わるよう即座に反映する（ターン開始時の予約効果とは別扱い）
        //========================================
        // ここから変更した！！菊地
        //========================================
        if (item.effectType == ItemEffectType.Bomb ||
        item.effectType == ItemEffectType.Rocket ||
        item.effectType == ItemEffectType.Wind)
        {
            MolkkyType molkkyType = MolkkyType.Normal;

            if (item.effectType == ItemEffectType.Bomb)
            {
                molkkyType = MolkkyType.Bomb;
            }
            else if (item.effectType == ItemEffectType.Rocket)
            {
                molkkyType = MolkkyType.Rocket;
            }
            else if (item.effectType == ItemEffectType.Wind)
            {
                molkkyType = MolkkyType.Wind; // ★Windを設定
            }
            if (GameManager.instance != null)
            {
                GameManager.instance.GetItem(molkkyType);
            }
        }
        else
        {
            reservedItem = item;
            Debug.Log($"[ItemManager] アイテム予約完了: {item.itemName}");
        }

        return true; // 登録成功 (true)
    }

    //========================================
    // ここまで変更した！！菊地
    //========================================

    public void ShowNotice(string message)
    {
        if (noticeText == null) return;
        noticeText.text = message;

        if (hideTextCoroutine != null)
        {
            StopCoroutine(hideTextCoroutine);
        }
        hideTextCoroutine = StartCoroutine(HideNoticeAfterDelay(2.0f));
    }

    private IEnumerator HideNoticeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (noticeText != null)
        {
            noticeText.text = "";
        }
    }

    public void OnTurnStart()
    {
        // ★追加: ターン開始時に取得制限フラグをリセット
        isItemAcquiredThisTurn = false;

        // 1. フィールド上の既存アイテムに1ターン経過したことを通知
        UpdateFieldItems();

        // 2. 予約されていたアイテム効果の発動処理
        if (reservedItem != null)
        {
            ExecuteEffect(reservedItem.effectType);
            ShowNotice($"効果発動: {reservedItem.itemName}！");
            reservedItem = null;
        }

        // 3. 毎ターン新しいアイテムを1つスポーン
        SpawnItem();
    }

    private void UpdateFieldItems()
    {
        SpawnedItem[] activeItems = FindObjectsByType<SpawnedItem>(FindObjectsInactive.Exclude);
        foreach (SpawnedItem item in activeItems)
        {
            item.OnTurnPassed();
        }
    }

    private void ExecuteEffect(ItemEffectType effectType)
    {
        switch (effectType)
        {
            case ItemEffectType.ScoreDouble:
                Debug.Log("【効果発動】得点2倍！");
                break;
            case ItemEffectType.BigMolkky:
                Debug.Log("【効果発動】モルック巨大化！");
                break;
            case ItemEffectType.SmallMolkky:
                Debug.Log("【効果発動】モルック小型化！");
                break;
            case ItemEffectType.SkittleGroup:
                Debug.Log("【効果発動】スキットル密集！");
                break;
            case ItemEffectType.SkittleSpread:
                Debug.Log("【効果発動】スキットル分散！");
                break;
        }
    }

    // ★Sceneビューで長方形スポーンエリアを緑色の枠で可視化
    private void OnDrawGizmosSelected()
    {
        if (spawnAreaCollider == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnAreaCollider.bounds.center, spawnAreaCollider.bounds.size);
    }
}