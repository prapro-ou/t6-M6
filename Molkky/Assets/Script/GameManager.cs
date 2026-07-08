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

    // 💡 二人プレイ用のデータ管理
    private int currentPlayer = 1; // 1 = Player1, 2 = Player2

    private int p1Score = 0;
    private int p1Misses = 0;

    private int p2Score = 0;
    private int p2Misses = 0;

    public static bool isGameFinished = false;
    private bool isCheckingTurnEnd = false;
    private bool canCheckStop = false;

    // 💡 修正ポイント①：ゲームが開始されたかを判定するフラグ（スイッチ）
    public static bool isGameStarted = false;

    // 💡 修正ポイント②：Unity画面からスタートメニュー（Panel）を登録するための箱
    public GameObject startMenuUI;

    void Start()
    {
        isGameFinished = false;

        // 💡 修正ポイント：別シーンのタイトルから来たら即遊べるように、最初から true にする！
        isGameStarted = true;

        currentPlayer = 1;
        p1Score = 0; p1Misses = 0;
        p2Score = 0; p2Misses = 0;

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

        Debug.Log($"--- Player {currentPlayer} のターン終了：得点計算開始 ---");

        // 倒れた本数とピン番号の集計
        int downedCount = 0;
        int lastDownedNumber = 0;

        foreach (Skittle s in skittles)
        {
            if (s != null && s.IsDown())
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

        // 💡 現在のプレイヤーのスコアに反映
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

        // 💡 勝敗・失格のチェック
        if (p1Misses >= 3)
        {
            scoreText.text = " Player 2 WIN! \n";
            isGameFinished = true;
        }
        else if (p2Misses >= 3)
        {
            scoreText.text = " Player 1 WIN! \n";
            isGameFinished = true;
        }
        else if (p1Score == 50)
        {
            scoreText.text = " Player 1 WIN!! \n";
            isGameFinished = true;
        }
        else if (p2Score == 50)
        {
            scoreText.text = " Player 2 WIN!! \n";
            isGameFinished = true;
        }

        // 💡 ゲームが続いていれば、ターン（プレイヤー）を交代する
        if (!isGameFinished)
        {
            currentPlayer = (currentPlayer == 1) ? 2 : 1;
            UpdateScoreUI();
        }

        // ピンをその場で立て直す
        foreach (Skittle s in skittles)
        {
            if (s != null)
            {
                s.StandUp();
            }
        }

        isCheckingTurnEnd = false;
    }

    // 💡 画面のUI表示を二人用に更新する
    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"<color=yellow>Now: Player {currentPlayer} </color>\n" +
                             $"Player 1: {p1Score} / 50 (Miss: {p1Misses}/3)\n" +
                             $"Player 2: {p2Score} / 50 (Miss: {p2Misses}/3)";
        }
    }
    // 💡 修正ポイント④：スタートボタンと紐付けるための関数
    public void StartGame()
    {
        isGameStarted = true;             // スイッチをONにする！
        startMenuUI.SetActive(false);    // スタート画面のUIを非表示にして消す！
        Debug.Log("ゲームスタート！");
    }
}