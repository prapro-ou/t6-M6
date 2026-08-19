using UnityEngine;
using UnityEngine.SceneManagement;

public class RuleManager : MonoBehaviour
{
    [Header("遷移先シーン名")]
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string gameSceneName = "GameScene";

    /// <summary>
    /// 「タイトルへ」ボタンの OnClick に割り当て
    /// </summary>
    public void OnTitleButtonPressed()
    {
        SceneManager.LoadScene(titleSceneName);
    }

    /// <summary>
    /// 「ゲーム開始（または戻る）」ボタンの OnClick に割り当て
    /// </summary>
    public void OnGameButtonPressed()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}