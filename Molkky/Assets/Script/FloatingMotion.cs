using UnityEngine;

// アタッチしたオブジェクトを、開始位置を基準に上下にゆっくり浮遊させる。
// periodSecondsをCameraOrbitのperiodSecondsと合わせるとループの継ぎ目が出ない。
public class FloatingMotion : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.15f;
    [SerializeField] private float periodSeconds = 10f;

    private Vector3 basePosition;
    private float time;

    private void Start()
    {
        basePosition = transform.position;
    }

    private void Update()
    {
        time += Time.deltaTime;
        float phase = (time / periodSeconds) * Mathf.PI * 2f;
        transform.position = basePosition + Vector3.up * (Mathf.Sin(phase) * amplitude);
    }
}
