using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cube : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision) // ぶつかったら
    {
        if (collision.gameObject.CompareTag("Player")) // そのオブジェクトのタグがplayerなら
        {
            Destroy(gameObject); // オブジェクトを消滅
        }
    }
}