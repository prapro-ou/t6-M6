using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RuleManager : MonoBehaviour
{
    [Header("遷移先シーン名")]
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("効果音")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonSound;

    /// <summary>
    /// 「タイトルへ」ボタンの OnClick に割り当て
    /// </summary>
    public void OnTitleButtonPressed()
    {
        StartCoroutine(PlaySoundThenLoadScene(titleSceneName));
    }

    /// <summary>
    /// 「ゲーム開始（または戻る）」ボタンの OnClick に割り当て
    /// </summary>
    public void OnGameButtonPressed()
    {
        StartCoroutine(PlaySoundThenLoadScene(gameSceneName));
    }

    // 効果音を鳴らし、鳴り切ってからシーン遷移する
    // （即座にLoadSceneすると、AudioSourceごと破棄されて音が聞こえないことがあるため）
    private IEnumerator PlaySoundThenLoadScene(string sceneName)
    {
        if (audioSource != null && buttonSound != null)
        {
            audioSource.PlayOneShot(buttonSound);
            yield return new WaitForSeconds(buttonSound.length);
        }

        SceneManager.LoadScene(sceneName);
    }
}