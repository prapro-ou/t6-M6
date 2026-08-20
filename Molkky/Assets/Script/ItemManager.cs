using UnityEngine;
using TMPro;
using System.Collections;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [Header("アイテムスポーン設定")]
    [SerializeField] private GameObject itemPrefab; // 生成するPrefab (SpawnedItemが付いたもの)
    [SerializeField] private CircleDrawer itemSpawnArea; // ★追加: 扇形のスポーンエリア（設定されていればこちらを優先）
    [SerializeField] private float itemSpawnHeight = 0.1f; // ★追加: 扇形エリア使用時のスポーン高さ
    [Tooltip("モルックの近くにはスポーンさせないための内側の半径（スキットルの並びより少し手前が目安）")]
    [SerializeField] private float itemSpawnMinRadius = 3f; // ★追加: スポーン範囲の内側の境界（CircleDrawer自体のinnerRadiusより手前を除外する用）
    [SerializeField] private BoxCollider spawnAreaCollider; // 長方形のスポーンエリア（itemSpawnArea未設定時のフォールバック）
    [SerializeField] private ItemData[] availableItems; // 5つのItemData

    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI noticeText;

    private ItemData reservedItem;
    private Coroutine hideTextCoroutine;

    // ★追加: 1ターンに1つのみ取得を制限するフラグ
    private bool isItemAcquiredThisTurn = false;

    // ★追加: OnTurnStart()の呼び出し回数（1巡目はスポーンさせず、2巡目以降からスポーンさせるため）
    private int turnStartCount = 0;

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

        // ★変更: 1巡目はアイテムなしにするため、開始時点のスポーンは行わない
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

        // 2. スポーン位置の計算（扇形エリア優先、なければ従来の長方形エリア）
        Vector3 spawnPosition;
        if (itemSpawnArea != null)
        {
            spawnPosition = itemSpawnArea.GetRandomPointInside(itemSpawnMinRadius);
            spawnPosition.y = itemSpawnHeight;
        }
        else if (spawnAreaCollider != null)
        {
            Bounds bounds = spawnAreaCollider.bounds;
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);
            float surfaceY = bounds.center.y;

            spawnPosition = new Vector3(randomX, surfaceY, randomZ);
        }
        else
        {
            Debug.LogWarning("[ItemManager] ItemSpawnArea / SpawnAreaColliderのどちらも設定されていないため、デフォルト位置にスポーンします。");
            spawnPosition = new Vector3(0, 0.1f, 2f);
        }

        // 3. 生成とデータのセット
        GameObject spawnedObj = Instantiate(itemPrefab, spawnPosition, Quaternion.identity);
        ItemData selectedData = ChooseWeightedRandomItem();

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

    // ★各ItemDataのspawnWeightに応じた重み付き抽選（重みが同じなら完全ランダムと同じ結果になる）
    private ItemData ChooseWeightedRandomItem()
    {
        float totalWeight = 0f;
        foreach (ItemData item in availableItems)
        {
            totalWeight += Mathf.Max(0f, item.spawnWeight);
        }

        if (totalWeight <= 0f)
        {
            return availableItems[Random.Range(0, availableItems.Length)];
        }

        float pick = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (ItemData item in availableItems)
        {
            cumulative += Mathf.Max(0f, item.spawnWeight);
            if (pick <= cumulative)
            {
                return item;
            }
        }

        return availableItems[availableItems.Length - 1];
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
        if (item.effectType == ItemEffectType.Bomb ||
        item.effectType == ItemEffectType.Rocket ||
        item.effectType == ItemEffectType.Wind ||
        item.effectType == ItemEffectType.Darkness ||
        item.effectType == ItemEffectType.MovingWall) // ★ 追加
        {
            MolkkyType molkkyType = item.effectType switch
            {
                ItemEffectType.Bomb => MolkkyType.Bomb,
                ItemEffectType.Rocket => MolkkyType.Rocket,
                ItemEffectType.Wind => MolkkyType.Wind,
                ItemEffectType.Darkness => MolkkyType.Darkness,
                ItemEffectType.MovingWall => MolkkyType.MovingWall, // ★ 追加
                _ => MolkkyType.Normal
            };

            if (GameManager.instance != null) GameManager.instance.GetItem(molkkyType);
        }
        else
        {
            reservedItem = item;
            Debug.Log($"[ItemManager] アイテム予約完了: {item.itemName}");
        }

        return true; // 登録成功 (true)
    }

  

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

        // 3. 2巡目以降のみ、毎ターン新しいアイテムを1つスポーン
        turnStartCount++;
        if (turnStartCount >= 2)
        {
            SpawnItem();
        }
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

    // ★Sceneビューでスポーンエリアを可視化（扇形エリア優先、なければ従来の長方形）
    private void OnDrawGizmosSelected()
    {
        if (itemSpawnArea != null)
        {
            DrawSpawnFanGizmo();
            return;
        }

        if (spawnAreaCollider == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnAreaCollider.bounds.center, spawnAreaCollider.bounds.size);
    }

    // 💡 itemSpawnMinRadiusの内側除外ラインだけを描画する
    //    外周の弧・左右の直線はCircleDrawer本体の境界線と完全に同じ座標のため、
    //    重ねて描画すると重複してちらつく（Z-fighting）ので描かない
    private void DrawSpawnFanGizmo()
    {
        float outerRadius = itemSpawnArea.Radius;
        float innerRadius = Mathf.Clamp(itemSpawnMinRadius, 0f, outerRadius);
        float halfAngleRad = itemSpawnArea.FanAngle * 0.5f * Mathf.Deg2Rad;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(itemSpawnArea.GetPointAtWorld(innerRadius, -halfAngleRad), itemSpawnArea.GetPointAtWorld(innerRadius, halfAngleRad));
    }
}