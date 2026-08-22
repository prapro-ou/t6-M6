using UnityEngine;

public enum WindDirection
{
    None,
    Left,   // 左方向 (-X)
    Right   // 右方向 (+X)
}

public class WindManager : MonoBehaviour
{
    public static WindManager instance;

    [Header("UI設定")]
    public GameObject windSelectButtun; // パネル「WindSelectButtun」

    [Header("モルック設定")]
    public Rigidbody molkkyRb;          // モルックのRigidbody

    [Header("パラメータ")]
    public float windForce = 50f;       // 風の強さ

    [Header("風の効果音")]
    [SerializeField] private AudioClip windSound;
    [SerializeField] private AudioSource windAudioSource;

    [Header("演出用ファン配置")]
    [SerializeField] private GameObject fanPrefab; // Fun.prefab（FanWind付き）を割り当てる
    [Tooltip("中心（投げ位置）から左右にどれだけ離して置くか。扇形境界と同じZ位置に置くため、この距離が小さくても角度的に境界の外側になる")]
    [SerializeField] private float fanSideDistance = 6f;
    [Tooltip("中心からZ方向（+Zが奥＝扇形の正面方向）にどれだけずらすか")]
    [SerializeField] private float fanForwardOffset = 0f;
    [SerializeField] private float fanHeight = 2f;
    [Tooltip("ファンプレハブ自体のスケールに掛ける倍率")]
    [SerializeField] private float fanScale = 1f;
    private GameObject activeFanInstance;

    [Header("デバッグ")]
    [Tooltip("配置調整用：ONにするとゲーム開始直後にファンを出しっぱなしにする（確認が終わったらOFFに戻すこと）")]
    [SerializeField] private bool debugSpawnFanOnStart = false;
    [SerializeField] private WindDirection debugFanDirection = WindDirection.Left;

    private WindDirection currentDirection = WindDirection.None;
    // 2: 予約中(自分の番), 1: 風発動中(相手の番), 0: なし
    private int remainingTurns = 0;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        if (windSelectButtun != null)
        {
            windSelectButtun.SetActive(false);
        }
    }

    private void Start()
    {
        if (debugSpawnFanOnStart)
        {
            SpawnFan(debugFanDirection);
        }
    }

    private void FixedUpdate()
    {
        // ★ remainingTurns が「1」の時（＝相手の番）だけ風を吹かせる
        if (remainingTurns == 1 && molkkyRb != null && !molkkyRb.isKinematic)
        {
            Vector3 forceDir = Vector3.zero;

            if (currentDirection == WindDirection.Left) forceDir = Vector3.left;   // 左 (-X)
            if (currentDirection == WindDirection.Right) forceDir = Vector3.right; // 右 (+X)

            molkkyRb.AddForce(forceDir * windForce, ForceMode.Acceleration);
        }
    }

    // 風アイテム獲得時に呼ぶ
    public void OpenWindSelector()
    {
        if (windSelectButtun != null)
        {
            windSelectButtun.SetActive(true);
        }
    }

    // ボタンの OnClick から呼ぶ (0: 左, 1: 右)
    public void SelectWindDirection(int dirIndex)
    {
        if (dirIndex == 0) currentDirection = WindDirection.Left;
        else if (dirIndex == 1) currentDirection = WindDirection.Right;

        // ★選択した時点では「2（予約中）」にしておき、このターンは吹かせない
        remainingTurns = 2;

        if (windSelectButtun != null)
        {
            windSelectButtun.SetActive(false);
        }

        Debug.Log($"[風予約完了] 向き: {currentDirection} | 次の相手のターンに風が吹きます");

        if (GameManager.instance != null)
        {
            GameManager.instance.OnWindDirectionSelected();
        }
    }

    // ターン進捗時に他スクリプトから呼び出される関数
    public void OnTurnAdvance()
    {
        if (remainingTurns > 0)
        {
            remainingTurns--;

            if (remainingTurns == 1)
            {
                Debug.Log($"[風発動！] 相手のターン中、{currentDirection} の風が吹いています");
                if (windAudioSource != null && windSound != null)
                {
                    windAudioSource.clip = windSound;
                    windAudioSource.loop = true;
                    windAudioSource.Play();
                }

                SpawnFan(currentDirection);
            }
            else if (remainingTurns <= 0)
            {
                currentDirection = WindDirection.None;
                Debug.Log("[風停止] 風が止みました");

                if (windAudioSource != null)
                {
                    windAudioSource.Stop();
                }

                DespawnFan();
            }
        }
    }

    // ★風が吹く向きと逆側（再配置ラインの外側）にファンを配置する
    //   左に風を吹かせる時は右側、右に風を吹かせる時は左側に置く
    private void SpawnFan(WindDirection direction)
    {
        DespawnFan();

        if (fanPrefab == null || CircleDrawer.Boundary == null || direction == WindDirection.None)
        {
            return;
        }

        CircleDrawer boundary = CircleDrawer.Boundary;
        Transform boundaryTransform = boundary.transform;

        // ★ワールドのX/Zではなく境界オブジェクトのローカル右方向/前方向を使うことで、
        //   将来ステージによって境界が回転していても「扇形の外側」を保てるようにする
        // 左風なら境界の右側、右風なら左側に配置
        // ★前後オフセット無し（fanForwardOffset=0）にしておけば、扇形の開き角度の外＝境界の外側に必ずなる。
        //   fanForwardOffsetで奥にずらす場合は、境界の外側を保てる範囲でfanSideDistanceとのバランスに注意
        float sideSign = (direction == WindDirection.Left) ? 1f : -1f;
        Vector3 spawnPosition = boundary.Center
            + boundaryTransform.right * (sideSign * fanSideDistance)
            + boundaryTransform.forward * fanForwardOffset;
        spawnPosition.y = fanHeight;

        // ★ファンのメッシュは薄い方（羽根の回転軸）がローカルY軸のため、
        //   Z軸ではなくY軸をwindDir（風が吹いていく向き＝境界のローカル右方向）に向ける。
        //   「上」はワールドYのまま（フィールドが回転してもファン自体は鉛直に立てる）
        Vector3 windDir = (direction == WindDirection.Left) ? -boundaryTransform.right : boundaryTransform.right;
        Quaternion spawnRotation = Quaternion.LookRotation(Vector3.up, windDir);

        activeFanInstance = Instantiate(fanPrefab, spawnPosition, spawnRotation);
        activeFanInstance.transform.localScale *= fanScale;

        // ★実際の風力はWindManager.FixedUpdateで一括して加えているため、
        //   演出用のファン自体には二重に力を加えさせない
        FanWind fanWind = activeFanInstance.GetComponentInChildren<FanWind>();
        if (fanWind != null)
        {
            fanWind.applyWindForce = false;
        }
    }

    private void DespawnFan()
    {
        if (activeFanInstance != null)
        {
            Destroy(activeFanInstance);
            activeFanInstance = null;
        }
    }
}