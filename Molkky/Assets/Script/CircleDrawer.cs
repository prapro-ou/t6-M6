using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CircleDrawer : MonoBehaviour
{
    [Header("扇形の設定")]
    [SerializeField] private int segments = 100;    // 弧の滑らかさ（頂点数）
    [SerializeField] private float radius = 5f;       // 半径
    [SerializeField] private float lineWidth = 0.2f;    // 線の太さ
    [SerializeField] private float fanAngle = 90f;    // 扇の開き角度（度数、+Z方向を中心に左右対称）
    [Tooltip("モルック側（頂点付近）を直線にするための内側の半径。0だと従来通り1点に尖る")]
    [SerializeField] private float innerRadius = 1f;  // モルック側の直線部分の長さ（頂点からの距離）

    private LineRenderer lineRenderer;

    // 💡 この扇形を「スキットルの飛びすぎ防止」の境界として他スクリプトから参照できるようにする
    public static CircleDrawer Boundary { get; private set; }

    public Vector3 Center => transform.position;
    public float Radius => radius;
    public float FanAngle => fanAngle;
    public float InnerRadius => innerRadius;

    // 💡 中心からの角度・距離で扇形上の1点を求める（ワールド座標）。ギズモ描画など他スクリプトからも利用する
    public Vector3 GetPointAtWorld(float r, float angleRad)
    {
        return transform.TransformPoint(PointAt(r, angleRad));
    }

    void Awake()
    {
        Boundary = this;
    }

    void Start()
    {
        SetupCircle();
    }

    // 💡 中心からの角度・距離で扇形上の1点を求める（+Z方向が正面、+X方向が右）
    private Vector3 PointAt(float r, float angle)
    {
        return new Vector3(r * Mathf.Sin(angle), 0f, r * Mathf.Cos(angle));
    }

    void SetupCircle()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false; // オブジェクト中心（＝モルックの投げ位置）のローカル座標を使用
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        int arcSegments = Mathf.Max(1, segments);
        float halfAngleRad = fanAngle * 0.5f * Mathf.Deg2Rad;
        float deltaAngle = (halfAngleRad * 2f) / arcSegments;

        var points = new List<Vector3>();

        // 1. モルック側（頂点付近）の直線部分（内側の半径で左右をまっすぐ結ぶ）
        points.Add(PointAt(innerRadius, -halfAngleRad));
        points.Add(PointAt(innerRadius, halfAngleRad));

        // 2. 右側の直線（内側→外側）
        points.Add(PointAt(radius, halfAngleRad));

        // 3. 外周の弧（右→左）
        float angle = halfAngleRad - deltaAngle;
        for (int i = 1; i <= arcSegments; i++)
        {
            points.Add(PointAt(radius, angle));
            angle -= deltaAngle;
        }

        // 4. 左側の直線（外側→内側）で閉じる
        points.Add(PointAt(innerRadius, -halfAngleRad));

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }

    // 💡 指定したワールド座標が、この扇形の範囲内にあるかどうかを判定する
    // （得点判定・再配置の基準の両方で使用。半径の外、または開き角度の外なら範囲外。
    // 　innerRadiusは見た目上の直線カットのみに使うため、範囲判定には影響させない）
    public bool IsInside(Vector3 worldPosition)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);

        float distance = new Vector2(local.x, local.z).magnitude;
        if (distance > radius) return false;

        // +Z方向（正面）を基準にした角度を求める
        float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        return Mathf.Abs(angle) <= fanAngle * 0.5f;
    }

    // 💡 この扇形の範囲内にあるランダムなワールド座標を1点返す（アイテムのスポーン等で使用）
    //    面積が均一になるよう、半径は二乗の範囲からサンプリングする
    public Vector3 GetRandomPointInside()
    {
        return GetRandomPointInside(innerRadius);
    }

    // 💡 内側の半径を呼び出し側から指定できる版（モルックの近くを避けてスポーンさせたい場合などに使用）
    public Vector3 GetRandomPointInside(float minRadius)
    {
        float halfAngleRad = fanAngle * 0.5f * Mathf.Deg2Rad;
        float angle = Random.Range(-halfAngleRad, halfAngleRad);

        float clampedMinRadius = Mathf.Clamp(minRadius, 0f, radius);
        float minR2 = clampedMinRadius * clampedMinRadius;
        float maxR2 = radius * radius;
        float r = Mathf.Sqrt(Random.Range(minR2, maxR2));

        return transform.TransformPoint(PointAt(r, angle));
    }

    // インスペクター上で数値を変更したときにリアルタイム更新
    void OnValidate()
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        SetupCircle();
    }
}