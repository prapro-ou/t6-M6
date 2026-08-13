using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;

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

    private int currentPlayer = 1;
    private int p1Score = 0;
    private int p1Misses = 0;
    private int p2Score = 0;
    private int p2Misses = 0;

    // --- 【追加】各プレイヤーの次のターンのアイテム保持変数 ---
    private MolkkyType p1NextItem = MolkkyType.Normal;
    private MolkkyType p2NextItem = MolkkyType.Normal;

    public static bool isGameFinished = false;
    private bool isCheckingTurnEnd = false;
    private bool canCheckStop = false;

    public GameObject startMenuUI;
    public static bool isGameStarted = false;


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

        UpdateScoreUI();
    }

    // --- 【追加】アイテムを獲得した時に呼ばれる関数 ---
    public void GetItem(MolkkyType item)
    {
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
        // 今回のモルックがBombタイプなら、投げた瞬間に爆発できる状態にする
        if (molkkyItemHandler != null && molkkyItemHandler.currentType == MolkkyType.Bomb && molkkyItemHandler.bombModel != null)
        {
            BombImpact bomb = molkkyItemHandler.bombModel.GetComponent<BombImpact>();
            if (bomb != null) bomb.Arm();
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
            }
            else if (molkkyRb.linearVelocity.magnitude < 0.05f)
            {
                StartCoroutine(TurnEndRoutine(4f));
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
            if (s != null)
            {
                bool isDown = s.IsDownForScore();
                // 角度（Vector3.Angle）も一緒にログに出すと原因が一目瞭然になります
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

        if (p1Misses >= 3) { scoreText.text = " Player 2 WIN! \n"; isGameFinished = true; }
        else if (p2Misses >= 3) { scoreText.text = " Player 1 WIN! \n"; isGameFinished = true; }
        else if (p1Score == 50) { scoreText.text = " Player 1 WIN!! \n"; isGameFinished = true; }
        else if (p2Score == 50) { scoreText.text = " Player 2 WIN!! \n"; isGameFinished = true; }

        UpdateScoreUI();

        if (!isGameFinished && nextTurnButtonUI != null)
        {
            nextTurnButtonUI.SetActive(true);
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

        // --- 【追加】次のプレイヤーが保持しているアイテムをモルックに適用 ---
        MolkkyType currentPlayersItem = (currentPlayer == 1) ? p1NextItem : p2NextItem;
        if (molkkyItemHandler != null)
        {
            molkkyItemHandler.SetMolkkyType(currentPlayersItem);
        }

        // 使用したアイテムストックをNormalリセット（1回使い切りにする場合）
        if (currentPlayer == 1) p1NextItem = MolkkyType.Normal;
        else p2NextItem = MolkkyType.Normal;

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
            // 勝利時は「NEXT TURN」ボタンを出さないように隠す
            if (nextTurnButtonUI != null)
            {
                nextTurnButtonUI.SetActive(false);
            }
        }
    }
    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            if (isGameFinished) return;

            scoreText.text = $"<color=yellow>Turn: Player {currentPlayer} </color>\n" +
                             $"Player 1: {p1Score} / 50 (Miss: {p1Misses}/3)\n" +
                             $"Player 2: {p2Score} / 50 (Miss: {p2Misses}/3)";
        }
    }

    public void StartGame()
    {
        isGameStarted = true;
        if (startMenuUI != null) startMenuUI.SetActive(false);
    }
}