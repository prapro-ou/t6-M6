using UnityEngine;

public class WallObstacleManager : MonoBehaviour
{
    public static WallObstacleManager Instance { get; private set; }

    [SerializeField] private GameObject movingWallPrefab;
    [SerializeField] private string wallLayerName = "Default";
    [SerializeField] private string skittleLayerName = "Skittle";
    [SerializeField] private LayerMask skittleLayerMask;

    [SerializeField] private float minX = -2.5f, maxX = 2.5f, minZ = 2f, maxZ = 7f;

    private bool isPendingForNextTurn = false;
    private GameObject[] activeWalls;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // スキットルと壁の物理衝突をオフ（押し飛ばし防止）
        int wallLayer = LayerMask.NameToLayer(wallLayerName);
        int skittleLayer = LayerMask.NameToLayer(skittleLayerName);
        if (wallLayer != -1 && skittleLayer != -1)
        {
            Physics.IgnoreLayerCollision(wallLayer, skittleLayer, true);
        }
    }

    public void RegisterObstacle() => isPendingForNextTurn = true;

    public void OnSkittlesResetComplete()
    {
        ClearWalls();
        if (isPendingForNextTurn)
        {
            SpawnWalls();
            isPendingForNextTurn = false;
        }
    }

    private void SpawnWalls()
    {
        int count = 4;
        activeWalls = new GameObject[count];

        // ★BoxCollider前提だと、Bushのように別の形状のColliderを使う壁プレハブでは
        //   スケール(=見た目のサイズ)を無視した既定値(1,1,1)にフォールバックしてしまうため、
        //   BoxColliderが無ければRendererのバウンズから見た目のサイズを求める。
        //   インポートしたモデルなどメッシュが子オブジェクトにある構成にも対応できるよう、
        //   子を含めた全Rendererのバウンズを合算する
        Vector3 checkSize = Vector3.one;
        if (movingWallPrefab != null)
        {
            if (movingWallPrefab.TryGetComponent<BoxCollider>(out var box))
            {
                checkSize = Vector3.Scale(box.size, movingWallPrefab.transform.localScale) * 0.6f;
            }
            else
            {
                Renderer[] renderers = movingWallPrefab.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        bounds.Encapsulate(renderers[i].bounds);
                    }
                    checkSize = bounds.size * 0.6f;
                }
            }
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = Vector3.zero;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                float zStep = (maxZ - minZ) / count;
                float zPos = minZ + (zStep * i) + Random.Range(0f, zStep * 0.5f);
                spawnPos = new Vector3(Random.Range(minX, maxX), 0.5f, zPos);

                if (!Physics.CheckBox(spawnPos, checkSize, Quaternion.identity, skittleLayerMask)) break;
            }

            GameObject wall = Instantiate(movingWallPrefab, spawnPos, Quaternion.identity);
            if (wall.TryGetComponent<MovingWall>(out var wallScript)) wallScript.SetRandomParams(2f, 5f);
            activeWalls[i] = wall;
        }
    }

    public void ClearWalls()
    {
        if (activeWalls == null) return;
        foreach (var w in activeWalls) { if (w != null) Destroy(w); }
        activeWalls = null;
    }
}