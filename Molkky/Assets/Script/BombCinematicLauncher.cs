using System.Collections;
using UnityEngine;

// 収録用: 指定したMolkkyのRigidbodyをボム化し、決まった方向へ自動発射する。
// PlayerControllerの照準/ゲージ状態は経由しない（ゲーム未開始でも動作する）。
public class BombCinematicLauncher : MonoBehaviour
{
    [SerializeField] private Rigidbody molkkyRb;
    [SerializeField] private Vector3 launchDirection = Vector3.right;
    [SerializeField] private float launchSpeed = 25f;

    private void Start()
    {
        StartCoroutine(LaunchNextFrame());
    }

    private IEnumerator LaunchNextFrame()
    {
        // PlayerController.Start()のResetMolkky()（親子付け直し等）が終わるのを1フレーム待つ
        yield return null;

        MolkkyItemHandler handler = molkkyRb.GetComponent<MolkkyItemHandler>();
        if (handler != null)
        {
            handler.SetMolkkyType(MolkkyType.Bomb);
        }

        molkkyRb.transform.SetParent(null);
        molkkyRb.isKinematic = false;
        molkkyRb.linearVelocity = launchDirection.normalized * launchSpeed;
    }
}
