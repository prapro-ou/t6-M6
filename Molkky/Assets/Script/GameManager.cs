using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement; // ★【追加】シーン遷移（リトライ・タイトル遷移）に使用

public class GameManager : MonoBehaviour
{
    // --- 【追加】どこからでもアクセスできるようにするSingleton設定 ---
    public static GameManager instance;

    [Header("登録設定")]
    public List<Skittle> skittles = new List<Skittle>();
    public PlayerController playerController;
    public Rigidbody molkkyRb;
    
    // --- 【追加】モルックの見た目切り替えハンドラー ---
    public MolkkyItemHandler molkkyItemHandler;

    [Header("UI設定")]
    public TextMeshProUGUI scoreText;
    
    [Header("ターン交代UI")]
    public GameObject nextTurnButtonUI;

    // =========================================================
    // ★【追加】ゲーム終了時（リザルト画面）用UIの設定項目
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

    // --- 【追加】各プレイヤーの次のターンのアイテム保持変数 ---
    private MolkkyType p1NextItem = MolkkyType.Normal;
    private MolkkyType p2NextItem = MolkkyType.Normal;

    // ★【暗闇処理】各プレイヤーが次のターン暗闇になるかどうかのフラグ
    private bool p1IsBlinded = false;
    private bool p2IsBlinded = false;

    public static bool isGameFinished = false;
    private bool isCheckingTurnEnd = false;
    private bool canCheckStop = false;

    public static bool isGameStarted = false;
    public GameObject startMenuUI;

    void Awake()
    {
        // 他のスクリプトから GameManager.instance で呼べるように保存
        instance = this;
    }

    void Start()
    {
        isGameFinished = false;
        isGameStarted = true;
        currentPlayer = 1;
        p1Score = 0; p1Misses = 0;
        p2Score = 0; p2Misses = 0;

        if (nextTurnButtonUI != null)
        {
            nextTurnButtonUI.SetActive(false);
        }

        // =========================================================
        // ★【追加】ゲーム開始時はリザルト関係のUI要素を非表示にしておく
        // =========================================================
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
            winnerText.gameObject.SetActive(false); // 「New Text」などの初期表示隠し
        }

        UpdateScoreUI();
    }

    // --- 【追加】アイテムを獲得した時に呼ばれる関数 ---
    public void GetItem(MolkkyType item)
    {
        // 風アイテム処理 獲得直後に風向き選択画面を開く
        if (item == MolkkyType.Wind)
        {
            if (WindManager.instance != null)
            {
                WindManager.instance.OpenWindSelector();
            }
            Debug.Log($"Player {currentPlayer} が風アイテムを獲得！ 風向きを選択してください。");
            return;
        }

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
        StartCoroutine(EnableCheckDelay());
    }

    void Update()
    {
        if (canCheckStop && !isCheckingTurnEnd)
        {
            if (molkkyRb.linearVelocity.magnitude < 0.05f)
            {
                StartCoroutine(TurnEndRoutine(4f));
            }

            if (molkkyRb.transform.position.y < -10f)
            {
                StartCoroutine(TurnEndRoutine(0f));
            }
        }
    }

    IEnumerator EnableCheckDelay()
    {
        canCheckStop = false;
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
            if (s != null && s.IsDownForScore())
            {
                downedCount++;
                lastDownedNumber = s.skittleNumber;
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

        // =========================================================
        // ★【変更】勝敗判定のメッセージ生成と表示処理
        // =========================================================
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
            // ★【変更】左上のスコアテキストを隠し、中央の winnerText に勝利メッセージを表示
            if (scoreText != null)
            {
                scoreText.gameObject.SetActive(false);
            }

            if (winnerText != null)
            {
                winnerText.gameObject.SetActive(true);
                winnerText.text = winMessage;
            }

            // ★【追加】勝利時は専用の「Next（次へ）」ボタンを表示
            if (nextButtonUI != null)
            {
                nextButtonUI.SetActive(true);
            }
        }
        else
        {
            UpdateScoreUI();

            // 通常ゲームプレイ時は元の「ターン交代」ボタンを表示
            if (nextTurnButtonUI != null)
            {
                nextTurnButtonUI.SetActive(true);
            }
        }

        isCheckingTurnEnd = false;
    }
        
    public void OnNextTurnButtonPressed()
    {
        foreach (Skittle s in skittles)
        {
            if (s != null) s.StandUp();
        }

        // ターン交代
        currentPlayer = (currentPlayer == 1) ? 2 : 1;

        // 風処理 ターン交代に合わせて残りターン数を進める（ここで発動判定）
        if (WindManager.instance != null)
        {
            WindManager.instance.OnTurnAdvance();
        }

        // 暗闇演出の適用チェック
        bool isCurrentPlayerBlinded = (currentPlayer == 1) ? p1IsBlinded : p2IsBlinded;
        if (DarknessEffect.instance != null)
        {
            DarknessEffect.instance.SetDarkness(isCurrentPlayerBlinded);
        }

        // 適用した暗闇フラグのリセット
        if (currentPlayer == 1) p1IsBlinded = false;
        else p2IsBlinded = false;

        // 次のプレイヤーが保持しているアイテムをモルックに適用 
        MolkkyType currentPlayersItem = (currentPlayer == 1) ? p1NextItem : p2NextItem;
        if (molkkyItemHandler != null)
        {
            molkkyItemHandler.SetMolkkyType(currentPlayersItem);
        }

        // 使用したアイテムストックをNormalリセット（1回使い切りにする場合）
        if (currentPlayer == 1) p1NextItem = MolkkyType.Normal;
        else p2NextItem = MolkkyType.Normal;

        UpdateScoreUI();

        if (nextTurnButtonUI != null)
        {
            nextTurnButtonUI.SetActive(false);
        }
    }

    // =========================================================
    // ★【追加】勝利画面用「Next（次へ）」ボタンを押した時の処理
    // =========================================================
    public void OnNextButtonPressed()
    {
        // 1. 勝利画面用の「次へ」ボタンを消す
        if (nextButtonUI != null)
        {
            nextButtonUI.SetActive(false);
        }

        // 2. 「もう一度遊ぶ」「タイトルへ」の2つのボタン（resultUI）を表示する
        if (resultUI != null)
        {
            resultUI.SetActive(true);
        }
    }

    // =========================================================
    // ★【変更】スコア表示テキスト（日本語化・リッチテキスト化）
    // =========================================================
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

    // =========================================================
    // ★【追加】「もう一度遊ぶ」「タイトルへ」ボタン用のイベント関数
    // =========================================================

    /// <summary>
    /// 「もう一度遊ぶ（リトライ）」ボタンが押されたとき
    /// </summary>
    public void OnRematchButtonPressed()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    /// <summary>
    /// 「タイトルに戻る」ボタンが押されたとき
    /// </summary>
    public void OnTitleButtonPressed()
    {
        SceneManager.LoadScene(titleSceneName);
    }
}