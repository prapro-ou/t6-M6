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
            }
            else if (remainingTurns <= 0)
            {
                currentDirection = WindDirection.None;
                Debug.Log("[風停止] 風が止みました");

                if (windAudioSource != null)
                {
                    windAudioSource.Stop();
                }
            }
        }
    }
}