using System.Collections;
using UnityEngine;
using UnityEngine.Video;

// ★タイトル背景：動画のデコード準備が終わるまで静止画（動画の1フレーム目）を表示し、
//   静止画が完全に消えてから動画を再生する（静止画と動画が重なる期間を作らない）
public class TitleVideoIntro : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public CanvasGroup posterOverlay;

    [SerializeField] private float fadeOutDuration = 0.4f;

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
        StartCoroutine(FadeOutPosterThenPlay(vp));
    }

    private IEnumerator FadeOutPosterThenPlay(VideoPlayer vp)
    {
        if (posterOverlay != null)
        {
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

        // ★この時点で静止画は完全に消えている。動画はすでにPrepare済みなので、
        //   ここでPlay()してもデコード待ちの止まりは発生しない
        vp.Play();
    }
}
