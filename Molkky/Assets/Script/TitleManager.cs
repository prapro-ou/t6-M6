using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 追加

public class TitleManager : MonoBehaviour
{
    public void OnStartButton()
    {
        SceneManager.LoadScene("GameScene"); // "GameScene" の部分はシーンの名前に変更
    }

    public void OnScoreAttackButton()
    {
        SceneManager.LoadScene("ScoreAttackScene");
    }
}
