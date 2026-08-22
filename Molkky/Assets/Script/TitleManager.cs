using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    /// <summary>
    /// 効果音
    /// </summary>
    public AudioSource audioSource;
    public AudioClip bottonSound;

    // 「ゲーム開始」ボタンを押したとき
    public void OnStartButton()
    {
        // ★ステージ選択画面を経由するように変更（選択後はStageSelectManagerがGameSceneをロードする）
        SceneTransitionAudio.PlayThenLoad(bottonSound, () => SceneManager.LoadScene("StageSelectScene"));
    }

    // ★【追加】「ルール説明」ボタンを押したとき
    public void OnRuleButton()
    {
        SceneTransitionAudio.PlayThenLoad(bottonSound, () => SceneManager.LoadScene("RuleScene")); // "RuleScene" の部分はルール説明シーンの名前に変更
    }
}