// タイトル〜ステージ選択〜ゲーム本編など、シーンをまたいで引き継ぎたい設定をまとめて持つ場所。
// staticなので、DontDestroyOnLoad用のGameObjectを用意しなくても値がシーン間で保持される。
public static class GameSettings
{
    // 選択されたステージのシーン名（今はGameScene固定。ステージが増えたら選択画面から書き換える）
    public static string SelectedStageSceneName = "GameScene";

    // ★のちに人数対応を追加する際は、ここにプレイヤー人数などの設定を追加していく
}
