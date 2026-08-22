using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class ScoreAttackManager : MonoBehaviour
{
    public static ScoreAttackManager instance;

    [Header("登録設定")]
    public List<Skittle> skittles = new List<Skittle>();
    public ScoreAttackPlayerController playerController;
    public Rigidbody molkkyRb;

    [Header("UI設定")]
    public TextMeshProUGUI scoreText;

    [Header("次の投球UI")]
    public GameObject nextThrowButtonUI;

    [Header("結果UI")]
    public GameObject resultUI;
    public TextMeshProUGUI resultText;
    [SerializeField] private string titleSceneName = "TitleScene";

    [Header("スコアアタック設定")]
    [SerializeField] private int totalThrows = 10;

    private const string HighScoreKey = "ScoreAttack_HighScore";

    private int throwsRemaining;
    private int totalScore = 0;

    // ★ PlayerControllerのパワーゲージ加速に使う（GameManager.currentScoreと同じ役割）
    public int currentScore => totalScore;

    public static bool isGameFinished = false;
    public static bool isGameStarted = false;
    private bool isCheckingThrowEnd = false;
    private bool canCheckStop = false;

    [SerializeField] private float lowSpeedRequiredDuration = 0.3f;
    private float lowSpeedTimer = 0f;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        isGameFinished = false;
        isGameStarted = true;
        throwsRemaining = totalThrows;
        totalScore = 0;

        if (nextThrowButtonUI != null) nextThrowButtonUI.SetActive(false);
        if (resultUI != null) resultUI.SetActive(false);

        UpdateScoreUI();
    }

    // ★ ScoreAttackPlayerControllerから、投げた瞬間に呼ばれる
    public void OnMolkkyLaunched()
    {
        StartCoroutine(EnableCheckDelay());
    }

    void Update()
    {
        if (canCheckStop && !isCheckingThrowEnd)
        {
            if (molkkyRb.transform.position.y < -10f)
            {
                StartCoroutine(ThrowEndRoutine(0f));
                return;
            }

            // 💡 放物線の頂点付近で一瞬速度が落ちることがあるため、低速状態が一定時間続いた場合のみ「停止した」とみなす
            if (molkkyRb.linearVelocity.magnitude < 0.1f && !AreSkittlesMoving())
            {
                lowSpeedTimer += Time.deltaTime;
                if (lowSpeedTimer >= lowSpeedRequiredDuration)
                {
                    StartCoroutine(ThrowEndRoutine(1.5f));
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
        if (canCheckStop && !isCheckingThrowEnd)
        {
            StartCoroutine(ThrowEndRoutine(0f));
        }
    }

    IEnumerator ThrowEndRoutine(float delayTime)
    {
        isCheckingThrowEnd = true;
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
                if (isDown)
                {
                    downedCount++;
                    lastDownedNumber = s.skittleNumber;
                }
            }
        }

        int throwScore = 0;
        if (downedCount == 1) throwScore = lastDownedNumber;
        else if (downedCount > 1) throwScore = downedCount;

        totalScore += throwScore;
        throwsRemaining--;

        if (throwsRemaining <= 0)
        {
            FinishGame();
        }
        else
        {
            UpdateScoreUI();
            if (nextThrowButtonUI != null) nextThrowButtonUI.SetActive(true);
        }

        isCheckingThrowEnd = false;
    }

    // ★ 「次の投球へ」ボタンから呼ぶ（GameManager.OnNextTurnButtonPressedに相当）
    public void OnNextThrowButtonPressed()
    {
        foreach (Skittle s in skittles)
        {
            if (s != null) s.StandUp();
        }

        if (nextThrowButtonUI != null) nextThrowButtonUI.SetActive(false);
        UpdateScoreUI();
    }

    private void FinishGame()
    {
        isGameFinished = true;

        int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        bool isNewRecord = totalScore > highScore;
        if (isNewRecord)
        {
            PlayerPrefs.SetInt(HighScoreKey, totalScore);
            PlayerPrefs.Save();
            highScore = totalScore;
        }

        if (scoreText != null) scoreText.gameObject.SetActive(false);
        if (nextThrowButtonUI != null) nextThrowButtonUI.SetActive(false);

        if (resultUI != null) resultUI.SetActive(true);
        if (resultText != null)
        {
            string recordLine = isNewRecord ? "<color=red>New Record!</color>" : $"ハイスコア: {highScore}";
            resultText.text = $"<size=100>合計スコア: {totalScore}</size>\n{recordLine}";
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"<color=white>残り投球数: {throwsRemaining} / {totalThrows}</color>\n" +
                              $"<color=white>合計スコア: {totalScore}</color>\n" +
                              $"<color=white>ハイスコア: {PlayerPrefs.GetInt(HighScoreKey, 0)}</color>";
        }
    }

    public void OnRetryButtonPressed()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    public void OnTitleButtonPressed()
    {
        SceneManager.LoadScene(titleSceneName);
    }
}
