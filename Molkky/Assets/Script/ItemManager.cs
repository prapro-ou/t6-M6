using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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

    private Coroutine hideTextCoroutine;

    // ★追加: 1ターンに1つのみ取得を制限するフラグ
    private bool isItemAcquiredThisTurn = false;

    // ★追加: OnTurnStart()の呼び出し回数（1巡目はスポーンさせず、2巡目以降からスポーンさせるため）
    private int turnStartCount = 0;

    // ★追加: isRareなItemDataが1回スポーンしたら、以降は抽選対象から外すためのフラグ
    private bool rareItemSpawned = false;

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
        ItemData selectedData = ChooseWeightedRandomItem();
        if (selectedData.isRare)
        {
            rareItemSpawned = true;
        }

        GameObject spawnedObj = Instantiate(itemPrefab, spawnPosition, Quaternion.identity);

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
    //   isRareなアイテムは、1ゲーム中に既に1回スポーン済みなら抽選対象から除外する
    private ItemData ChooseWeightedRandomItem()
    {
        List<ItemData> eligibleItems = new List<ItemData>();
        foreach (ItemData item in availableItems)
        {
            if (item.isRare && rareItemSpawned) continue;
            eligibleItems.Add(item);
        }

        if (eligibleItems.Count == 0)
        {
            return availableItems[Random.Range(0, availableItems.Length)];
        }

        float totalWeight = 0f;
        foreach (ItemData item in eligibleItems)
        {
            totalWeight += Mathf.Max(0f, item.spawnWeight);
        }

        if (totalWeight <= 0f)
        {
            return eligibleItems[Random.Range(0, eligibleItems.Count)];
        }

        float pick = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (ItemData item in eligibleItems)
        {
            cumulative += Mathf.Max(0f, item.spawnWeight);
            if (pick <= cumulative)
            {
                return item;
            }
        }

        return eligibleItems[eligibleItems.Count - 1];
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

        // ★既存のMolkkyType機構（ItemBoxと同じ仕組み）に乗せて、
        //   自分の次の投球で見た目・能力が変わるよう即座に反映する
        MolkkyType molkkyType = item.effectType switch
        {
            ItemEffectType.Bomb => MolkkyType.Bomb,
            ItemEffectType.Rocket => MolkkyType.Rocket,
            ItemEffectType.Wind => MolkkyType.Wind,
            ItemEffectType.Darkness => MolkkyType.Darkness,
            ItemEffectType.MovingWall => MolkkyType.MovingWall,
            ItemEffectType.AllSkittles => MolkkyType.AllSkittles,
            _ => MolkkyType.Normal
        };

        if (GameManager.instance != null) GameManager.instance.GetItem(molkkyType);

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

        // 2. 2巡目以降のみ、毎ターン新しいアイテムを1つスポーン
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