using UnityEngine;
using System.Collections;

public class Skittle : MonoBehaviour
{
    // 💡 インスペクターで何番のピンかを設定できるようにします
    [Header("ピンの番号（1〜12）")]
    public int skittleNumber;

    [Header("再配置時の正面向き（角度調整）")]
    [Tooltip("立ち上がったときに数字が正面を向くよう、必要に応じて90, 180, 270などに変更してください")]
    public float targetYRotation = 0f;

    [Header("飛びすぎ防止の設定")]
    [Tooltip("モルックを投げる場所からこの距離（メートル）以上離れていたら、再配置時に初期位置へ戻します")]
    public float maxDistanceFromThrowPoint = 40f;

    // 💡 モルックを投げる場所（発射台）の基準点。GameManagerから設定されます
    private static Transform throwPoint;

    public static void SetThrowPoint(Transform point)
    {
        throwPoint = point;
    }

    [Header("特殊ピン設定（左右に揺れる）")]
    [Tooltip("ONにすると、立て直された後モルックが投げられるまで左右に揺れ続けます")]
    public bool isMovingPin = false;
    public float moveSpeed = 2f;
    public float moveRange = 0.5f;

    private Rigidbody rb;
    private Vector3 initialPosition;
    private Vector3 swayCenterPosition;
    private bool isSwaying = false;
    private Coroutine swayCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 💡 最初（ゲーム開始時）の位置を覚えておく
        initialPosition = transform.position;
    }

    // 💡 ① 得点にするかどうかの判定（90度に倒れたか）
    public bool IsDownForScore()
    {
        // 垂直からほぼ横たわっている（85度〜95度付近）状態を得点対象とする
        return Vector3.Angle(transform.up, Vector3.up) >= 89f;
    }

    // 💡 ② 再配置（リセット）対象にするかどうかの判定（1度でも傾いているか）
    // ※将来的に「傾いたピンだけ再配置する」ルールにする場合に活用できます
    public bool IsDownForReset()
    {
        // わずかでも（1度以上）傾いていたら、再配置対象とする
        return Vector3.Angle(transform.up, Vector3.up) > 1f;
    }

    // 💡 ピンをその場で（または元の位置に）無条件で立て直す関数
    public void StandUp()
    {
        // 現在は無条件で全ピンを再配置（もし条件付きに戻したい場合は if (IsDownForReset()) で囲んでください）
        Debug.Log($"ピン {skittleNumber} 番を再配置します。");

        // 1. まず完全に動きを止めて、物理を一時停止
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // 2. 角度を「ワールド空間の真上」かつ「指定した正面の向き」に向け直す
        transform.rotation = Quaternion.Euler(0f, targetYRotation, 0f);

        // 3. 地面へのめり込みを防ぐため、先に少しだけ上に浮かせる
        Vector3 targetPosition = transform.position;

        // モルックを投げる場所を中心とした円の外に出ていたら、再配置の基準位置を初期位置に戻す
        Vector3 center = (throwPoint != null) ? throwPoint.position : initialPosition;
        float distanceFromCenter = Vector3.Distance(
            new Vector3(transform.position.x, center.y, transform.position.z),
            center);
        if (distanceFromCenter > maxDistanceFromThrowPoint)
        {
            Debug.Log($"ピン {skittleNumber} 番は投げる場所から{distanceFromCenter:F1}m離れているため、初期位置に戻します。");
            targetPosition = initialPosition;
        }

        targetPosition.y += 0.1f;

        // 4. 重なりを防止した安全な位置（X, Z）を計算する
        targetPosition = GetOverlapResolvedPosition(targetPosition);
        transform.position = targetPosition;

        // 5. 物理エンジンに新しい位置・角度を即座に反映させる
        Physics.SyncTransforms();

        // 6. 物理演算を安全に再開
        StartCoroutine(SafeActivatePhysics());
    }

    // 💡 重なりをチェックして少しずつずらす関数
    private Vector3 GetOverlapResolvedPosition(Vector3 startPos)
    {
        float minDistance = 0.25f; // スキットル同士の最低限界距離
        Vector3 newPos = startPos;

        // シーン内のすべてのスキットルを取得して距離を調べる
        Skittle[] allSkittles = Object.FindObjectsByType<Skittle>(FindObjectsInactive.Exclude);

        foreach (Skittle other in allSkittles)
        {
            // 自分自身はスキップ
            if (other == this) continue;

            // 高さ（Y）を無視して平面（X, Z）の距離を計算
            Vector3 diff = newPos - other.transform.position;
            diff.y = 0;

            // もし他のスキットルとの距離が近すぎたら
            if (diff.magnitude < minDistance)
            {
                // もし完全に同じ座標で重なっている場合はランダムな方向に逃がす
                if (diff.magnitude == 0)
                {
                    diff = new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));
                }

                // 重ならないギリギリの距離まで少し外側に押し出す
                newPos = other.transform.position + diff.normalized * minDistance;
            }
        }

        return newPos;
    }

    // 💡 物理挙動を安全に再開するコルーチン
    IEnumerator SafeActivatePhysics()
    {
        // Unityが位置と角度の上書きを完全に完了するのを待つ
        yield return new WaitForFixedUpdate();

        if (isMovingPin)
        {
            // 特殊ピンは物理を再開せず、投げられるまで左右に揺れ続ける
            StartSwaying();
            yield break;
        }

        // 安全が確保されたあとに物理演算を再開！
        rb.isKinematic = false;

        // 念のため、再開時の速度も完全にゼロに固定
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // 💡 左右に揺れる動きを開始する（StandUp完了後に呼ばれる）
    private void StartSwaying()
    {
        if (swayCoroutine != null) StopCoroutine(swayCoroutine);

        swayCenterPosition = transform.position;
        isSwaying = true;
        swayCoroutine = StartCoroutine(SwayRoutine());
    }

    IEnumerator SwayRoutine()
    {
        float t = 0f;
        while (isSwaying)
        {
            t += Time.deltaTime;
            Vector3 offset = transform.right * Mathf.Sin(t * moveSpeed) * moveRange;
            rb.MovePosition(swayCenterPosition + offset);
            yield return new WaitForFixedUpdate();
        }
    }

    // 💡 モルックが投げられた瞬間にGameManagerから呼ばれ、揺れを止めて通常の物理判定に戻す
    public void StopSwaying()
    {
        if (!isSwaying) return;

        isSwaying = false;
        if (swayCoroutine != null)
        {
            StopCoroutine(swayCoroutine);
            swayCoroutine = null;
        }

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}