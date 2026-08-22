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
        SceneTransitionAudio.PlayThenLoad(buttonSound, () => SceneManager.LoadScene(titleSceneName));
    }

    /// <summary>
    /// 「ゲーム開始（または戻る）」ボタンの OnClick に割り当て
    /// </summary>
    public void OnGameButtonPressed()
    {
        SceneTransitionAudio.PlayThenLoad(buttonSound, () => SceneManager.LoadScene(gameSceneName));
    }
}
