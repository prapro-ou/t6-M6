using UnityEngine;
// 新しい入力システムを使うための宣言
using UnityEngine.InputSystem;

public class MolkkyThrower : MonoBehaviour
{
    
    private Rigidbody rb;
    public float throwForce = 40f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Unity 6の新しいキーボード監視の書き方（スペースキー）
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
           
        {
            Vector3 throwDirection = new Vector3(0, 0.3f, 1f).normalized;
            rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);

            Debug.Log("新システムでスペースキーの入力を検知し、発射しました！");
        }
    }
}