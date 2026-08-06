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

    private Rigidbody rb;
    private Vector3 initialPosition;

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

        // 安全が確保されたあとに物理演算を再開！
        rb.isKinematic = false;

        // 念のため、再開時の速度も完全にゼロに固定
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}