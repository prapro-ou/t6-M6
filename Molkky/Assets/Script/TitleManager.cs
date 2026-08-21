using System.Collections;
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
        StartCoroutine(PlaySoundThenLoadScene("StageSelectScene"));
    }

    // ★【追加】「ルール説明」ボタンを押したとき
    public void OnRuleButton()
    {
        StartCoroutine(PlaySoundThenLoadScene("RuleScene")); // "RuleScene" の部分はルール説明シーンの名前に変更
    }

    // 効果音を鳴らし、鳴り切ってからシーン遷移する
    // （即座にLoadSceneすると、AudioSourceごと破棄されて音が聞こえないことがあるため）
    private IEnumerator PlaySoundThenLoadScene(string sceneName)
    {
        if (audioSource != null && bottonSound != null)
        {
            audioSource.PlayOneShot(bottonSound);
            yield return new WaitForSeconds(bottonSound.length);
        }

        SceneManager.LoadScene(sceneName);
    }
}