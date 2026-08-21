using UnityEngine;

// targetからのオフセット位置を保ちながら追従し、常にtargetを見続けるカメラ。
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(4f, 1.5f, 0f);
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private bool lookAtTarget = true;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        if (lookAtTarget)
        {
            transform.LookAt(target.position);
        }
    }
}
