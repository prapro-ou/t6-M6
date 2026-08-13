using UnityEngine;
using System.Collections;

public class DarknessEffect : MonoBehaviour
{
    public static DarknessEffect instance;

    [Header("暗闇用のUIパネル（CanvasGroupをアタッチした黒パネル）")]
    [SerializeField] private CanvasGroup darkOverlayGroup;

    [Header("暗闇時の画面の暗さ（0.0〜1.0）")]
    [Range(0f, 1f)]
    [SerializeField] private float darkAlpha = 0.85f;

    [SerializeField] private float fadeSpeed = 3f;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        if (darkOverlayGroup != null) darkOverlayGroup.alpha = 0f;
    }

    public void SetDarkness(bool enable)
    {
        if (darkOverlayGroup == null) return;

        StopAllCoroutines();
        float targetAlpha = enable ? darkAlpha : 0f;
        StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        while (!Mathf.Approximately(darkOverlayGroup.alpha, targetAlpha))
        {
            darkOverlayGroup.alpha = Mathf.MoveTowards(darkOverlayGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
            yield return null;
        }
        darkOverlayGroup.alpha = targetAlpha;
    }
}