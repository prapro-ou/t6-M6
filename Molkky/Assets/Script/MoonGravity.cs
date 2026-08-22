using UnityEngine;

public class MoonGravity : MonoBehaviour
{
    void Start()
    {
        // 地球の重力 (-9.81) の1/6 である -1.635 に設定
        Physics.gravity = new Vector3(0f, -1.635f, 0f);
    }

    void OnDestroy()
    {
        // 別のシーンに移動・離脱した際に地球の重力に戻す
        Physics.gravity = new Vector3(0f, -9.81f, 0f);
    }
}