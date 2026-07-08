using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int power;

    Rigidbody playerRb;
    // Rigidbodyを入れるための箱「playerRb」を作成

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerRb = GetComponent<Rigidbody>();
        // playerRbという箱の中にコンポーネントを捕まえてくる．そのときのコンポーネントはRigidbody

        // playerRb.AddForce(Vector3.forward * power);
        // playerRbの機能を使って力を加える
    }

    // Update is called once per frame
    void Update()
    {
        // 十字キーで操作
        if(Input.GetKey(KeyCode.RightArrow)) // 右矢印キーが押されたら（長押し可）
        {
            playerRb.AddForce(Vector3.right); // 右向きの力を加える
        }

        if(Input.GetKey(KeyCode.LeftArrow))
        {
            playerRb.AddForce(Vector3.left);
        }

        if(Input.GetKey(KeyCode.UpArrow))
        {
            playerRb.AddForce(Vector3.forward);
        }

        if(Input.GetKey(KeyCode.DownArrow))
        {
            playerRb.AddForce(Vector3.back);
        }

        if(Input.GetKeyDown(KeyCode.Space)) // spaceキーが押されたら
        {
            playerRb.AddForce(Vector3.up * power); // ジャンプ
        }
    }
}
