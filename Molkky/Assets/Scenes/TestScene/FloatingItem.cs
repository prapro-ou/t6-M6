using UnityEngine;

public class FloatingItem : MonoBehaviour
{
    [Header("上下の揺れ設定")]
    public float amplitude = 0.2f;    // 揺れる幅（高さ）
    public float frequency = 2.0f;    // 揺れるスピード

    [Header("回転設定")]
    public float rotateSpeed = 30.0f; // 回転スピード

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // 上下に移動
        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        // Y軸回転
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }
}