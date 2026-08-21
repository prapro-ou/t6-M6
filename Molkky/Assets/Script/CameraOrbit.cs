using UnityEngine;

// ターゲットを中心に、centerAngleを軸としてarcDegrees分だけ左右に往復しながら周回する。
// sin波で往復するため、periodSeconds経過すると位置・速度とも開始時点と一致し、
// そのままループ動画にしても継ぎ目が出ない。
public class CameraOrbit : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float radius = 2.2f;
    [SerializeField] private float heightMin = -0.4f;
    [SerializeField] private float heightMax = 1.3f;
    [SerializeField] private float lookHeightOffset = 0.4f;
    [SerializeField] private float centerAngle = 90f;
    [SerializeField] private float arcDegrees = 70f;
    [SerializeField] private float periodSeconds = 10f;
    [SerializeField] private bool lookAtTarget = true;

    private float time;

    private void Update()
    {
        time += Time.deltaTime;
        float phase = (time / periodSeconds) * Mathf.PI * 2f;
        float angle = centerAngle + Mathf.Sin(phase) * (arcDegrees * 0.5f);
        float rad = angle * Mathf.Deg2Rad;

        // phase 0 (開始時点) で heightMin になるように -cos を使う。
        float heightT = (1f - Mathf.Cos(phase)) * 0.5f;
        float height = Mathf.Lerp(heightMin, heightMax, heightT);

        Vector3 targetPos = target != null ? target.position : Vector3.zero;
        Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * radius;
        transform.position = targetPos + offset + Vector3.up * height;

        if (lookAtTarget)
        {
            transform.LookAt(targetPos + Vector3.up * lookHeightOffset);
        }
    }
}
