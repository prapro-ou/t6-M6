using System.Collections;
using UnityEngine;

// 収録用: 指定したMolkkyのRigidbodyを爆弾化し、決まった方向へ自動発射する。
// PlayerControllerの照準/ゲージ状態は経由しない（ゲーム未開始でも動作する）。
public class BombCinematicLauncher : MonoBehaviour
{
    [SerializeField] private Rigidbody molkkyRb;

    [Header("発射方向")]
    [SerializeField] private Vector3 launchDirection = Vector3.right;

    [Header("発射速度")]
    [SerializeField] private float launchSpeed = 25f;

    private void Start()
    {
        StartCoroutine(LaunchNextFrame());
    }

    private IEnumerator LaunchNextFrame()
    {
        // PlayerController.Start()のResetMolkky()が終わるのを1フレーム待つ
        yield return null;

        // モルックを爆弾タイプに変更
        MolkkyItemHandler handler = molkkyRb.GetComponent<MolkkyItemHandler>();

        if (handler != null)
        {
            handler.SetMolkkyType(MolkkyType.Bomb);
        }

        // 親から外す
        molkkyRb.transform.SetParent(null);

        // 物理を有効化
        molkkyRb.isKinematic = false;

        // 指定方向へ自動発射
        molkkyRb.linearVelocity =
            launchDirection.normalized * launchSpeed;
    }
}