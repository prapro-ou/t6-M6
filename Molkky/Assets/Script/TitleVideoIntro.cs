using System.Collections;
using UnityEngine;
using UnityEngine.Video;

// ★タイトル背景：静止画（動画の1フレーム目）→動画再生への切り替えを
//   ハードカットではなくフェードにすることで、デコーダー準備中の色/タイミングの
//   わずかなズレが目立たないようにする
public class TitleVideoIntro : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public CanvasGroup posterOverlay;

    [SerializeField] private float fadeOutDuration = 0.4f;
    // ★Play()直後は数フレーム分の再生が安定するまで色/tearingが乱れることがあるため、
    //   フェード開始を少しだけ遅らせて安定してから切り替える
    [SerializeField] private float settleDelayAfterPlay = 0.15f;

    void Start()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnVideoPrepared;
        vp.Play();
        StartCoroutine(FadeOutPosterAfterDelay());
    }

    private IEnumerator FadeOutPosterAfterDelay()
    {
        yield return new WaitForSeconds(settleDelayAfterPlay);

        if (posterOverlay == null)
        {
            yield break;
        }

        float startAlpha = posterOverlay.alpha;
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            posterOverlay.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        posterOverlay.alpha = 0f;
        posterOverlay.gameObject.SetActive(false);
    }
}
