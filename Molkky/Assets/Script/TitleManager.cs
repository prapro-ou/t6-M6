using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    // 「ゲーム開始」ボタンを押したとき
    public void OnStartButton()
    {
        SceneManager.LoadScene("GameScene"); // "GameScene" の部分はゲームシーンの名前に変更
    }

    // ★【追加】「ルール説明」ボタンを押したとき
    public void OnRuleButton()
    {
        SceneManager.LoadScene("RuleScene"); // "RuleScene" の部分はルール説明シーンの名前に変更
    }
}