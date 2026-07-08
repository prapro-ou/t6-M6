using UnityEngine;
using UnityEngine.UIElements;
using System.Collections; // 💡 スクリプトの一番上にこれが無い場合は追加してください

public class Skittle : MonoBehaviour
{
    // 💡 インスペクターで何番のピンかを設定できるようにします
    [Header("ピンの番号（1〜12）")]
    public int skittleNumber;

    private Rigidbody rb;
    private Vector3 initialPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 💡 最初（ゲーム開始時）の位置を覚えておく
        initialPosition = transform.position;
    }

    // 💡 倒れているか判定する関数
    public bool IsDown()
    {
        // ピンの頭（上方向）が、世界の「上」から一定以上傾いていたら倒れたとみなす
        return Vector3.Angle(transform.up, Vector3.up) > 30f;
    }

    // 💡 ピンをその場で（または元の位置に）立て直す関数
    

    public void StandUp()
{
    if (IsDown())
    {
        Debug.Log($"ピン {skittleNumber} 番が倒れていました。");

        // 1. まず完全に動きを止めて、物理を一時停止（置物化）
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // 2. 角度を「完全に真上（傾きゼロ）」に向け直す
        transform.localRotation = Quaternion.identity;

        // 3. 地面へのめり込みを防ぐため、少しだけ上に浮かす
        Vector3 safePosition = transform.position;
        safePosition.y += 0.3f;
        transform.position = safePosition;

        // 💡 修正ポイント：警告の原因だった rb.Sleep() を消し、
        // 「一瞬だけ待ってから物理を安全に再開する」処理（コルーチン）を呼び出します
        StartCoroutine(SafeActivatePhysics());
    }
}

// 💡 新しくこの関数を下に追加してください
IEnumerator SafeActivatePhysics()
{
    // 0.01秒だけ待つことで、Unityが位置と角度の上書きを完全に完了するのを待ちます
    yield return new WaitForFixedUpdate();

    // 安全が確保されたあとに物理演算を再開！
    rb.isKinematic = false;

    // 念のため、再開時の速度も完全にゼロに固定します
    rb.linearVelocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;
}
}