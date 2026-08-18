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

    // 💡 まだ動いている（倒れている途中・転がっている途中）かどうかの判定
    public bool IsMoving(float threshold = 0.05f)
    {
        if (rb == null) return false;
        return rb.linearVelocity.magnitude > threshold || rb.angularVelocity.magnitude > threshold;
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

    // 💡 ③ CircleDrawerが描く境界（扇形）の外に出ているかどうかの判定（得点判定・再配置の両方で使用）
    public bool IsOutsideBoundary()
    {
        CircleDrawer boundary = CircleDrawer.Boundary;
        if (boundary == null) return false;

        return !boundary.IsInside(transform.position);
    }

    // 💡 ピンをその場で（または元の位置に）立て直す関数
    public void StandUp()
    {
        // 💡 既に立っていて範囲内にあるピンは何もしない。
        //    ここで無条件に全ピン再配置していると、元々問題なく立っているピン同士まで
        //    GetOverlapResolvedPositionで押し出し合い、密集地帯では逆に隣のピンにぶつけて
        //    倒してしまうことがあるため、本当に動かす必要があるピンだけに限定する。
        if (!IsDownForReset() && !IsOutsideBoundary())
        {
            return;
        }

        Debug.Log($"ピン {skittleNumber} 番を再配置します。");

        // 1. まず完全に動きを止めて、物理を一時停止
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // 2. 角度を「ワールド空間の真上」かつ「指定した正面の向き」に向け直す
        transform.rotation = Quaternion.Euler(0f, targetYRotation, 0f);

        // 3. 高さは「倒れた状態のY」ではなく「元々立っていたときのY」を使う
        //    （ピンごとに立ったときの高さは異なるため、倒れた高さに固定オフセットを足すだけだと
        //    　背の高いピンはめり込んだままになったり、低いピンは浮きすぎて着地時に弾んで倒れたりする）
        Vector3 targetPosition = transform.position;
        targetPosition.y = initialPosition.y;

        // CircleDrawerが描いている境界（扇形）の外に出ていたら、再配置の基準位置を初期位置に戻す
        bool returningToInitialPosition = IsOutsideBoundary();
        if (returningToInitialPosition)
        {
            Debug.Log($"ピン {skittleNumber} 番は境界の外に出ているため、初期位置に戻します。");
            targetPosition = initialPosition;
        }

        // 地面へのめり込みを防ぐため、ごくわずかだけ上に浮かせてから着地させる
        targetPosition.y += 0.01f;

        // 4. 重なりを防止した安全な位置（X, Z）を計算する
        //    ただし初期位置に戻す場合は、そもそもデザイン段階で重ならないよう配置されているため
        //    ここでの押し出しは不要。全ピンが同時に初期位置へ戻る場面（境界外への吹き飛ばし等）で、
        //    お互いがまだ「初期位置に戻る途中」であることを知らずに押し出し合い、
        //    正しいフォーメーションが崩れてしまうのを防ぐ。
        if (!returningToInitialPosition)
        {
            targetPosition = GetOverlapResolvedPosition(targetPosition);
        }
        transform.position = targetPosition;

        // 5. 物理エンジンに新しい位置・角度を即座に反映させる
        Physics.SyncTransforms();

        // 6. 物理演算を安全に再開
        StartCoroutine(SafeActivatePhysics());
    }

    // 💡 このピンの立った状態でのXZ方向の半径（見た目上のコライダーの太さ）を取得する
    //    ピンごとに背の高さ（＝メッシュ）が異なり、太さも一律ではないため固定値では判定できない
    private float GetXZRadius()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return 0.15f; // コライダーが無い場合の保険値

        Vector3 extents = col.bounds.extents;
        return Mathf.Max(extents.x, extents.z);
    }

    // 💡 重なりをチェックして少しずつずらす関数
    private Vector3 GetOverlapResolvedPosition(Vector3 startPos)
    {
        const int maxIterations = 12;   // 密集していても収まるまで押し出しを繰り返す回数
        const float safetyMargin = 0.05f; // 実際のコライダー同士がぴったり接触しないための余白

        float myRadius = GetXZRadius();
        Vector3 newPos = startPos;

        // シーン内のすべてのスキットルを取得して距離を調べる
        Skittle[] allSkittles = Object.FindObjectsByType<Skittle>(FindObjectsInactive.Exclude);

        // 💡 1回の走査だけだと「Aから逃げた先でCと重なる」といったケースを取りこぼすため、
        //    全員との重なりが解消されるまで（または上限回数まで）繰り返し押し出す
        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            bool hasOverlap = false;

            foreach (Skittle other in allSkittles)
            {
                // 自分自身はスキップ
                if (other == this) continue;

                // お互いの実際の太さから、重ならないために必要な距離を求める
                float requiredDistance = myRadius + other.GetXZRadius() + safetyMargin;

                // 高さ（Y）を無視して平面（X, Z）の距離を計算
                Vector3 diff = newPos - other.transform.position;
                diff.y = 0;

                // もし他のスキットルとの距離が近すぎたら
                if (diff.magnitude < requiredDistance)
                {
                    hasOverlap = true;

                    // ほぼ完全に同じ座標で重なっている場合はランダムな方向に逃がす
                    if (diff.magnitude < 0.0001f)
                    {
                        diff = new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));
                    }

                    // 重ならないギリギリの距離まで少し外側に押し出す
                    newPos = other.transform.position + diff.normalized * requiredDistance;
                }
            }

            // どのピンとも重ならなくなったら収束したとみなして打ち切る
            if (!hasOverlap) break;
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