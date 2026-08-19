using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // --- 【追加】どこからでもアクセスできるようにするSingleton設定 ---
    public static GameManager instance;

    [Header("登録設定")]
    public List<Skittle> skittles = new List<Skittle>();
    public PlayerController playerController;
    public Rigidbody molkkyRb;

    // --- モルックの見た目切り替えハンドラー ---
    public MolkkyItemHandler molkkyItemHandler;

    [Header("UI設定")]
    public TextMeshProUGUI scoreText;

    [Header("ターン交代UI")]
    public GameObject nextTurnButtonUI;

    // =========================================================
    // ゲーム終了時（リザルト画面）用UIの設定項目
    // =========================================================
    [Header("ゲーム終了UI")]
    public GameObject nextButtonUI;       // 勝利時に出る「次へ」ボタン
    public GameObject resultUI;           // 2つのボタン（再戦・タイトル）が入った親UIパネル
    public TextMeshProUGUI winnerText;    // 画面中央に勝者名を表示するテキスト
    [SerializeField] private string titleSceneName = "TitleScene"; // 遷移先のタイトルシーン名


    private int currentPlayer = 1;
    private int p1Score = 0;
    private int p1Misses = 0;
    private int p2Score = 0;
    private int p2Misses = 0;

    // ★ 現在のターン（Player 1 または Player 2）のスコアを自動で返すプロパティ
    public int currentScore
    {
        get
        {
            return (currentPlayer == 1) ? p1Score : p2Score;
        }
    }

    // --- 各プレイヤーの次のターンのアイテム保持変数 ---
    private MolkkyType p1NextItem = MolkkyType.Normal;
    private MolkkyType p2NextItem = MolkkyType.Normal;

    // ★【暗闇処理】各プレイヤーが次のターン暗闇になるかどうかのフラグ
    private bool p1IsBlinded = false;
    private bool p2IsBlinded = false;

    // ★【1. 追加】各プレイヤーが次のターン風を起こす権利を持っているかのフラグ
    private bool p1HasWindItem = false;
    private bool p2HasWindItem = false;

    public static bool isGameFinished = false;
    private bool isCheckingTurnEnd = false;
    private bool canCheckStop = false;

    // 💡 「停止した」と判定するために低速状態を継続させる必要がある秒数
    [SerializeField] private float lowSpeedRequiredDuration = 0.3f;
    private float lowSpeedTimer = 0f;

    public GameObject startMenuUI;
    public static bool isGameStarted = false;


    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        isGameFinished = false;
        isGameStarted = true;
        currentPlayer = 1;
        p1Score = 0; p1Misses = 0;
        p2Score = 0; p2Misses = 0;

        p1HasWindItem = false;
        p2HasWindItem = false;

        if (nextTurnButtonUI != null)
        {
            nextTurnButtonUI.SetActive(false);
        }

        if (nextButtonUI != null)
        {
            nextButtonUI.SetActive(false);
        }

        if (resultUI != null)
        {
            resultUI.SetActive(false);
        }

        if (winnerText != null)
        {
            winnerText.gameObject.SetActive(false);
        }

        UpdateScoreUI();
    }

    // --- 【2. 変更】アイテムを獲得した時に呼ばれる関数 ---
    public void GetItem(MolkkyType item)
    {
        //========================================
        // ここから変更した！！菊地
        //========================================
        // ★風アイテム処理：獲得した瞬間に風向き選択画面を開く
        if (item == MolkkyType.Wind)
        {
            if (WindManager.instance != null)
            {
                WindManager.instance.OpenWindSelector();
            }
            Debug.Log($"Player {currentPlayer} が風アイテム(Yellow)を獲得！ 風向きを選択してください。");
            return;
        }

        //========================================
        // ここから変更した！！菊地
        //========================================

        // 暗闇アイテムの場合は相手にデバフを付与
        if (item == MolkkyType.Darkness)
        {
            if (currentPlayer == 1) p2IsBlinded = true;
            else p1IsBlinded = true;

            Debug.Log($"Player {currentPlayer} が暗闇アイテムを獲得！ 相手の次のターンが暗闇になります。");
            return;
        }

        // 通常アイテム（Bomb/Rocket）は自分のストックへ
        if (currentPlayer == 1)
        {
            p1NextItem = item;
        }
        else
        {
            p2NextItem = item;
        }
        Debug.Log($"Player {currentPlayer} が {item} を獲得！ 次のターンで発動します。");
    }

    public void OnMolkkyLaunched()
    {
        if (molkkyItemHandler != null && molkkyItemHandler.currentType == MolkkyType.Bomb && molkkyItemHandler.bombModel != null)
        {
            BombImpact bomb = molkkyItemHandler.bombModel.GetComponent<BombImpact>();
            if (bomb != null) bomb.Arm();
        }

        foreach (Skittle s in skittles)
        {
            if (s != null) s.StopSwaying();
        }

        StartCoroutine(EnableCheckDelay());
    }

    void Update()
    {
        if (canCheckStop && !isCheckingTurnEnd)
        {
            if (molkkyRb.transform.position.y < -10f)
            {
                StartCoroutine(TurnEndRoutine(0f));
                return;
            }

            if (molkkyRb.linearVelocity.magnitude < 0.1f && !AreSkittlesMoving())
            {
                lowSpeedTimer += Time.deltaTime;
                if (lowSpeedTimer >= lowSpeedRequiredDuration)
                {
                    StartCoroutine(TurnEndRoutine(1.5f));
                }
            }
            else
            {
                lowSpeedTimer = 0f;
            }
        }
    }

    private bool AreSkittlesMoving()
    {
        foreach (Skittle s in skittles)
        {
            if (s != null && s.IsMoving()) return true;
        }
        return false;
    }

    IEnumerator EnableCheckDelay()
    {
        canCheckStop = false;
        lowSpeedTimer = 0f;
        yield return new WaitForSeconds(0.5f);
        canCheckStop = true;
        StartCoroutine(SafetyTimeoutRoutine());
    }

    IEnumerator SafetyTimeoutRoutine()
    {
        yield return new WaitForSeconds(10.0f);
        if (canCheckStop && !isCheckingTurnEnd)
        {
            StartCoroutine(TurnEndRoutine(0f));
        }
    }

    IEnumerator TurnEndRoutine(float delayTime)
    {
        isCheckingTurnEnd = true;
        canCheckStop = false;

        if (delayTime > 0f)
        {
            yield return new WaitForSeconds(delayTime);
        }

        playerController.ResetMolkky();

        int downedCount = 0;
        int lastDownedNumber = 0;

        foreach (Skittle s in skittles)
        {
            if (s != null)
            {
                bool isDown = s.IsDownForScore() || s.IsOutsideBoundary();
                float angle = Vector3.Angle(s.transform.up, Vector3.up);

                Debug.Log($"【{s.gameObject.name}】 角度: {angle}度 -> 倒れ判定: {isDown}");

                if (isDown)
                {
                    downedCount++;
                    lastDownedNumber = s.skittleNumber;
                }
            }
        }

        int turnScore = 0;
        bool isMiss = false;

        if (downedCount == 1)
        {
            turnScore = lastDownedNumber;
        }
        else if (downedCount > 1)
        {
            turnScore = downedCount;
        }
        else
        {
            isMiss = true;
        }

        if (currentPlayer == 1)
        {
            if (isMiss) p1Misses++; else p1Misses = 0;
            p1Score += turnScore;
            if (p1Score > 50) p1Score = 25;
        }
        else
        {
            if (isMiss) p2Misses++; else p2Misses = 0;
            p2Score += turnScore;
            if (p2Score > 50) p2Score = 25;
        }

        string winMessage = "";
        if (p1Misses >= 3)
        {
            winMessage = "<color=red><size=100>プレイヤー 2 WIN!</size></color>";
            isGameFinished = true;
        }
        else if (p2Misses >= 3)
        {
            winMessage = "<color=red><size=100>プレイヤー 1 WIN!</size></color>";
            isGameFinished = true;
        }
        else if (p1Score == 50)
        {
            winMessage = "<color=red><size=100>プレイヤー 1 WIN!!</size></color>";
            isGameFinished = true;
        }
        else if (p2Score == 50)
        {
            winMessage = "<color=red><size=100>プレイヤー 2 WIN!!</size></color>";
            isGameFinished = true;
        }

        if (isGameFinished)
        {
            if (scoreText != null)
            {
                scoreText.gameObject.SetActive(false);
            }

            if (winnerText != null)
            {
                winnerText.gameObject.SetActive(true);
                winnerText.text = winMessage;
            }

            if (nextButtonUI != null)
            {
                nextButtonUI.SetActive(true);
            }
        }
        else
        {
            UpdateScoreUI();

            if (nextTurnButtonUI != null)
            {
                nextTurnButtonUI.SetActive(true);
            }
        }

        isCheckingTurnEnd = false;
    }

    // --- 【3. 変更】ターン交代時の処理 ---
    public void OnNextTurnButtonPressed()
    {
        foreach (Skittle s in skittles)
        {
            if (s != null) s.StandUp();
        }

        // 1. ターン交代（P1 ⇆ P2）
        currentPlayer = (currentPlayer == 1) ? 2 : 1;

        // 2. 予約アイテムの発動＋新アイテムのスポーン
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.OnTurnStart();
        }

        // 3. 風処理（残りターンのカウントダウン等）
        if (WindManager.instance != null)
        {
            WindManager.instance.OnTurnAdvance();
        }

        // 4. 暗闇演出の適用チェック（暗闇は相手へのデバフなので交代後プレイヤーに適用）
        bool isCurrentPlayerBlinded = (currentPlayer == 1) ? p1IsBlinded : p2IsBlinded;
        if (DarknessEffect.instance != null)
        {
            DarknessEffect.instance.SetDarkness(isCurrentPlayerBlinded);
        }

        // 適用した暗闇フラグのリセット
        if (currentPlayer == 1) p1IsBlinded = false;
        else p2IsBlinded = false;

        // ★ 5. 【修正箇所】交代後のプレイヤー（currentPlayer）が保持しているアイテムを適用
        MolkkyType currentPlayersItem = (currentPlayer == 1) ? p1NextItem : p2NextItem;

        if (molkkyItemHandler != null)
        {
            molkkyItemHandler.SetMolkkyType(currentPlayersItem);
        }

        // ★ 6. 【修正箇所】適用した「自分のストック」のみをリセットする
        if (currentPlayer == 1)
        {
            p1NextItem = MolkkyType.Normal;
        }
        else
        {
            p2NextItem = MolkkyType.Normal;
        }

        // 7. UI表示の更新
        if (!isGameFinished)
        {
            UpdateScoreUI();

            if (nextTurnButtonUI != null)
            {
                nextTurnButtonUI.SetActive(false);
            }
        }
        else
        {
            if (nextTurnButtonUI != null)
            {
                nextTurnButtonUI.SetActive(false);
            }
        }
    }

    public void OnNextButtonPressed()
    {
        if (nextButtonUI != null)
        {
            nextButtonUI.SetActive(false);
        }

        if (resultUI != null)
        {
            resultUI.SetActive(true);
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            if (isGameFinished) return;

            scoreText.text = $"<color=blue>ターン: プレイヤー {currentPlayer}</color>\n" +
                             $"<color=white>プレイヤー 1: {p1Score} / 50</color>\n" +
                             $"<color=white>(ミス: {p1Misses}/3)</color>\n" +
                             $"<color=white>プレイヤー 2: {p2Score} / 50</color>\n" +
                             $"<color=white>(ミス: {p2Misses}/3)</color>";
        }
    }

    public void StartGame()
    {
        isGameStarted = true;
        if (startMenuUI != null) startMenuUI.SetActive(false);
    }

    public void OnRematchButtonPressed()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    public void OnTitleButtonPressed()
    {
        SceneManager.LoadScene(titleSceneName);
    }
}