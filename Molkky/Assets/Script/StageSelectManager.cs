using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// ステージ選択画面用。ボタンのOnClickから SelectStage(シーン名) を呼び出す想定。
// 今はステージが1つしかないが、増えたらボタンを増やしてそれぞれ違うシーン名を渡すだけでよい。
public class StageSelectManager : MonoBehaviour
{
    [Header("効果音")]
    public AudioSource audioSource;
    public AudioClip buttonSound;

    public void SelectStage(string stageSceneName)
    {
        GameSettings.SelectedStageSceneName = stageSceneName;
        StartCoroutine(PlaySoundThenLoadScene(stageSceneName));
    }

    // ★人数選択（今は「2人対戦」か「ひとりでスコアアタック」の2モードしかないため、
    //   実質どのシーンを読み込むかの選択になる。ボタンのOnClickから直接呼べるよう用意）
    public void SelectTwoPlayerMode()
    {
        SelectStage("GameScene");
    }

    public void SelectSoloScoreAttack()
    {
        SelectStage("ScoreAttackScene");
    }

    // タイトルに戻るボタン用
    public void OnBackButton()
    {
        StartCoroutine(PlaySoundThenLoadScene("TitleScene"));
    }

    // 効果音を鳴らし、遷移先シーンが軽くても音が鳴り切ってからシーン遷移する
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
