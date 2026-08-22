using UnityEngine;

// シーン遷移用の効果音を、遷移をまたいでも最後まで鳴らし切るためのヘルパー。
// 通常のAudioSourceで鳴らすと、遷移先シーンのロードでAudioSourceごと破棄され、
// タイミングによっては音が途中で途切れることがあるため、専用オブジェクトに逃がして鳴らす。
public static class SceneTransitionAudio
{
    public static void PlayThenLoad(AudioClip clip, System.Action loadScene)
    {
        if (clip != null)
        {
            GameObject player = new GameObject("SceneTransitionSfx");
            Object.DontDestroyOnLoad(player);
            AudioSource source = player.AddComponent<AudioSource>();
            source.PlayOneShot(clip);
            Object.Destroy(player, clip.length);
        }

        loadScene?.Invoke();
    }
}
