using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("登録設定")]
    public List<Skittle> skittles = new List<Skittle>();
    public PlayerController playerController;
    public Rigidbody molkkyRb;

    [Header("UI設定")]
    public TextMeshProUGUI scoreText;
    
    [Header("ターン交代UI")]
    public GameObject nextTurnButtonUI;

    // 二人プレイ用のデータ管理
    private int currentPlayer = 1; // 1 = Player1, 2 = Player2

    private int p1Score = 0;
    private int p1Misses = 0;

    private int p2Score = 0;
    private int p2Misses = 0;

    public static bool isGameFinished = false;
    private bool isCheckingTurnEnd = false;
    private bool canCheckStop = false;

    // ゲームが開始されたかを判定するフラグ
    public static bool isGameStarted = false;

    // スタートメニュー（Panel）を登録するための箱
    public GameObject startMenuUI;

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

        UpdateScoreUI();
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
                Debug.Log("モルックが奈落に落ちたため、即時回収します。");
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

        Debug.Log($"--- Player {currentPlayer} の投擲終了：得点計算開始 ---");

        // 90度倒れた本数とピン番号の集計
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

        // 得点の計算
        int turnScore = 0;
        bool isMiss = false;

        if (downedCount == 1)
        {
            turnScore = lastDownedNumber;
            Debug.Log($"Player {currentPlayer}: ピン {lastDownedNumber} 番を倒して {turnScore} 点獲得！");
        }
        else if (downedCount > 1)
        {
            turnScore = downedCount;
            Debug.Log($"Player {currentPlayer}: {downedCount} 本倒して {turnScore} 点獲得！");
        }
        else
        {
            isMiss = true;
            Debug.Log($"Player {currentPlayer}: ミス！1本も倒せませんでした。");
        }

        // スコアの反映と減点処理
        if (currentPlayer == 1)
        {
            if (isMiss) p1Misses++; else p1Misses = 0;
            p1Score += turnScore;
            if (p1Score > 50) { p1Score = 25; Debug.Log("Player 1が50点を超えたため25点に減点！"); }
        }
        else
        {
            if (isMiss) p2Misses++; else p2Misses = 0;
            p2Score += turnScore;
            if (p2Score > 50) { p2Score = 25; Debug.Log("Player 2が50点を超えたため25点に減点！"); }
        }

        // 勝敗・失格のチェック
        if (p1Misses >= 3)
        {
            scoreText.text = "<size=120%><color=red>Player 2 WIN!</color></size>\n(Player 1 Disqualified)";
            isGameFinished = true;
        }
        else if (p2Misses >= 3)
        {
            scoreText.text = "<size=120%><color=red>Player 1 WIN!</color></size>\n(Player 2 Disqualified)";
            isGameFinished = true;
        }
        else if (p1Score == 50)
        {
            scoreText.text = "<size=150%><color=yellow>Player 1 WIN!!</color></size>";
            isGameFinished = true;
        }
        else if (p2Score == 50)
        {
            scoreText.text = "<size=150%><color=yellow>Player 2 WIN!!</color></size>";
            isGameFinished = true;
        }

        // 💡 修正ポイント：ゲームが終わっていない場合のみ、通常スコアの更新と次ターンボタンの表示を行う！
        if (!isGameFinished)
        {
            UpdateScoreUI();

            if (nextTurnButtonUI != null)
            {
                nextTurnButtonUI.SetActive(true);
            }
        }

        isCheckingTurnEnd = false;
    }

    public void OnNextTurnButtonPressed()
    {
        // 💡 ゲーム終了時はボタンを押しても何もしないようにガード
        if (isGameFinished) return;

        // 1. 傾いたピンを立て直す（再配置）
        foreach (Skittle s in skittles)
        {
            if (s != null)
            {
                s.StandUp();
            }
        }

        // 2. プレイヤーを交代（1なら2へ、2なら1へ）
        currentPlayer = (currentPlayer == 1) ? 2 : 1;
        UpdateScoreUI();

        // 3. ターン交代ボタンを非表示にする
        if (nextTurnButtonUI != null)
        {
            nextTurnButtonUI.SetActive(false);
        }

        Debug.Log($"ターン交代完了！次のプレイヤー: Player {currentPlayer}");
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"<color=yellow>Turn: Player {currentPlayer} </color>\n" +
                             $"Player 1: {p1Score} / 50 (Miss: {p1Misses}/3)\n" +
                             $"Player 2: {p2Score} / 50 (Miss: {p2Misses}/3)";
        }
    }

    public void StartGame()
    {
        isGameStarted = true;
        if (startMenuUI != null)
        {
            startMenuUI.SetActive(false);
        }
        Debug.Log("ゲームスタート！");
    }
}