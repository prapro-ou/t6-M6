using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CircleDrawer : MonoBehaviour
{
    [Header("円の設定")]
    [SerializeField] private int segments = 100;    // 円の滑らかさ（頂点数）
    [SerializeField] private float radius = 5f;       // 半径
    [SerializeField] private float lineWidth = 0.2f;    // 線の太さ

    private LineRenderer lineRenderer;

    // 💡 この円を「スキットルの飛びすぎ防止」の境界として他スクリプトから参照できるようにする
    public static CircleDrawer Boundary { get; private set; }

    public Vector3 Center => transform.position;
    public float Radius => radius;

    void Awake()
    {
        Boundary = this;
    }

    void Start()
    {
        SetupCircle();
    }

    void SetupCircle()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false; // オブジェクト中心のローカル座標を使用
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = segments + 1; // 円を閉じるため+1

        float deltaTheta = (2f * Mathf.PI) / segments;
        float theta = 0f;

        for (int i = 0; i < segments + 1; i++)
        {
            // X-Z平面（地面）の上に円を描く計算
            float x = radius * Mathf.Cos(theta);
            float z = radius * Mathf.Sin(theta);

            lineRenderer.SetPosition(i, new Vector3(x, 0f, z));
            theta += deltaTheta;
        }
    }

    // インスペクター上で数値を変更したときにリアルタイム更新
    void OnValidate()
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        SetupCircle();
    }
}