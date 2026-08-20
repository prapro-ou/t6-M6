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

    /// <summary>
    /// 効果音
    /// </summary>
    [Header("効果音")]
    public AudioSource audioSource;
    public AudioClip startSound;        // 開始
    public AudioClip turnEndSound;      // ターン終了
    public AudioClip itemSound;         // アイテム取得
    public AudioClip winSound;          // 勝利
    public AudioClip buttonSound;       // ボタン

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

    // ★風アイテム：獲得済みで、モルックが手元に戻ったら選択画面を開く必要があるかどうか
    private bool hasPendingWindSelection = false;
    // ★風向き選択待ちのため、選択完了後に交代ボタンを表示する必要があるかどうか
    private bool pendingNextTurnButtonAfterWind = false;

    public static bool isGameFinished = false;
    private bool isCheckingTurnEnd = false;
    private bool canCheckStop = false;

    // ★前の投球分の戻りタイマー系コルーチンが止められないまま残り、次のプレイヤーの番に
    //   なってから遅れて発火してターンを勝手に進めてしまう不具合の対策として参照を保持する
    private Coroutine safetyTimeoutCoroutine;
    private Coroutine boundaryReturnCoroutine;
    private Coroutine specialReturnCoroutine; // ロケット・ボムの戻りルーチン

    // 💡 「停止した」と判定するために低速状態を継続させる必要がある秒数
    [SerializeField] private float lowSpeedRequiredDuration = 0.3f;
    private float lowSpeedTimer = 0f;

    // 💡 弱い投球で着地した瞬間すでに低速な場合でも、着地直後すぐに「停止」と判定しないための猶予秒数
    //   （継続接地時間の判定自体はMolkkyItemHandler.ContinuousGroundedDurationに一元化している）
    [SerializeField] private float minTimeAfterLanding = 0.3f;

    [Header("特殊モルック 戻りタイマー設定")]
    // ロケット・ボムは速度判定を使わず、専用タイマーで手元に戻す
    [SerializeField] private float rocketReturnMinTime = 6f;   // 投げてから戻るまでの最短秒数
    [SerializeField] private float rocketReturnMaxTime = 7f;   // 投げてから戻るまでの最長秒数
    [SerializeField] private float bombReturnDelayAfterExplode = 2f; // 爆発してから戻るまでの秒数

    // 通常モルックが再配置の線（CircleDrawerの扇形）を越えてから戻るまでの秒数
    [SerializeField] private float boundaryReturnDelay = 4f;
    private bool isBoundaryReturnScheduled = false;

    [Header("パワーゲージに応じた最低待機時間")]
    // 弱い投球だとすぐ低速判定になり早く戻りすぎるため、パワーゲージの強さに応じて
    // 「最低でもこの秒数が経つまでは手元に戻らない」下限を設ける
    [SerializeField] private float minReturnTimeAtMinPower = 3f; // パワー0（最弱）の時の最低待機秒数
    [SerializeField] private float minReturnTimeAtMaxPower = 6f;   // パワー最大の時の最低待機秒数
    private float currentMinReturnTime = 0f;
    private float launchElapsedTime = 0f;

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
        audioSource.PlayOneShot(startSound);
        currentPlayer = 1;
        p1Score = 0; p1Misses = 0;
        p2Score = 0; p2Misses = 0;

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

        PlaySound(itemSound);
        //========================================
        // ここから変更した！！
        //========================================
        // ★風アイテム処理：選択画面はモルックが手元に戻ったタイミングで開く（TurnEndRoutine側）
        if (item == MolkkyType.Wind)
        {
            hasPendingWindSelection = true;
            Debug.Log($"Player {currentPlayer} が風アイテム(Yellow)を獲得！ モルックが手元に戻ったら風向きを選択してください。");
            return;
        }

        //========================================
        // ここから変更した！！
        //========================================

        // 暗闇アイテムの場合は相手にデバフを付与
        if (item == MolkkyType.Darkness)
        {
            if (currentPlayer == 1) p2IsBlinded = true;
            else p1IsBlinded = true;

            Debug.Log($"Player {currentPlayer} が暗闇アイテムを獲得！ 相手の次のターンが暗闇になります。");
            return;
        }

        if (item == MolkkyType.MovingWall) // ★追加
        {
            if (WallObstacleManager.Instance != null) WallObstacleManager.Instance.RegisterObstacle();
            Debug.Log($"Player {currentPlayer} が壁アイテムを獲得！ 相手の次のターンに壁が出現します。");
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

    // ★風向きの選択が完了した時にWindManagerから呼ばれる
    public void OnWindDirectionSelected()
    {
        if (!pendingNextTurnButtonAfterWind) return;

        pendingNextTurnButtonAfterWind = false;
        if (nextTurnButtonUI != null)
        {
            nextTurnButtonUI.SetActive(true);
        }
    }

    public void OnMolkkyLaunched(float powerRatio = 1f)
    {
        // ★前の投球の戻りタイマー系コルーチンが万一まだ残っていたら、ここで確実に止めておく。
        //   放置すると次のプレイヤーが投げている最中や投げる前に遅れて発火し、
        //   勝手にターンが進んでしまう不具合の原因になる
        CancelPendingReturnCoroutines();

        currentMinReturnTime = Mathf.Lerp(minReturnTimeAtMinPower, minReturnTimeAtMaxPower, Mathf.Clamp01(powerRatio));
        launchElapsedTime = 0f;

        foreach (Skittle s in skittles)
        {
            if (s != null) s.StopSwaying();
        }

        // ★飛行中に何にも当たっていないうちは「停止した」と判定させないため、接地状態をリセット
        if (molkkyItemHandler != null)
        {
            molkkyItemHandler.ResetGroundState();
        }

        MolkkyType launchedType = (molkkyItemHandler != null) ? molkkyItemHandler.currentType : MolkkyType.Normal;

        // ★ロケット・ボムは投げている最中に速度判定で誤って戻らないよう、
        //   通常の速度判定（EnableCheckDelay）を使わず専用タイマーで戻す
        if (launchedType == MolkkyType.Bomb && molkkyItemHandler.bombModel != null)
        {
            BombImpact bomb = molkkyItemHandler.bombModel.GetComponent<BombImpact>();
            if (bomb != null)
            {
                bomb.Arm();
                bomb.OnExploded += HandleBombExploded;
            }
        }
        else if (launchedType == MolkkyType.Rocket)
        {
            specialReturnCoroutine = StartCoroutine(RocketReturnRoutine());
        }
        else
        {
            StartCoroutine(EnableCheckDelay());
        }
    }

    IEnumerator RocketReturnRoutine()
    {
        float waitTime = Random.Range(rocketReturnMinTime, rocketReturnMaxTime);
        yield return new WaitForSeconds(waitTime);

        // ★ロケットは直進中に重力を切っている（Rocket.cs）ため、何かに当たるまで着地しない。
        //   タイマーが来てもまだ飛行中なら、着地（＝何かに接触）するまで少し待ってから戻す
        yield return WaitUntilGroundedOrTimeout(5f);

        TriggerSpecialReturn();
    }

    private void HandleBombExploded()
    {
        if (molkkyItemHandler != null && molkkyItemHandler.bombModel != null)
        {
            BombImpact bomb = molkkyItemHandler.bombModel.GetComponent<BombImpact>();
            if (bomb != null) bomb.OnExploded -= HandleBombExploded;
        }
        specialReturnCoroutine = StartCoroutine(BombReturnRoutine());
    }

    IEnumerator BombReturnRoutine()
    {
        yield return new WaitForSeconds(bombReturnDelayAfterExplode);
        yield return WaitUntilGroundedOrTimeout(3f);
        TriggerSpecialReturn();
    }

    private void TriggerSpecialReturn()
    {
        if (!isCheckingTurnEnd)
        {
            StartCoroutine(TurnEndRoutine(0f));
        }
    }

    // ★タイマーが来た時点でまだ空中にいる場合、着地するまで少し待ってから戻す。
    //   ただし何らかの理由で着地を検知できない場合に無限に待ち続けないよう、上限秒数で必ず打ち切る。
    //
    //   「一瞬だけ接触した」を着地と誤判定しないよう、minTimeAfterLanding秒以上
    //   連続で接地し続けているかどうかは MolkkyItemHandler.ContinuousGroundedDuration で判定する
    //   （Update()側の停止判定と同じ基準を共有し、二重実装を避けている）
    private IEnumerator WaitUntilGroundedOrTimeout(float maxWaitSeconds)
    {
        if (molkkyItemHandler == null) yield break;

        float elapsed = 0f;
        while (elapsed < maxWaitSeconds)
        {
            if (molkkyItemHandler.ContinuousGroundedDuration >= minTimeAfterLanding)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void Update()
    {
        if (canCheckStop || isCheckingTurnEnd) // ★戻り待ち中も含め、投げてからの経過時間として数える
        {
            launchElapsedTime += Time.deltaTime;
        }

        if (canCheckStop && !isCheckingTurnEnd)
        {
            if (molkkyRb.transform.position.y < -10f)
            {
                StartCoroutine(TurnEndRoutine(0f));
                return;
            }

            if (!isBoundaryReturnScheduled && CircleDrawer.Boundary != null &&
                !CircleDrawer.Boundary.IsInside(molkkyRb.transform.position))
            {
                isBoundaryReturnScheduled = true;
                boundaryReturnCoroutine = StartCoroutine(BoundaryReturnRoutine());
            }

            // ★地面やスキットルに接触していない（＝空中に浮いている）うちや、着地した直後（弱い投球で
            //   着地時点ですでに低速な場合を含む）は、まだ「停止した」と判定しない
            bool pastLandingGracePeriod = molkkyItemHandler == null ||
                molkkyItemHandler.ContinuousGroundedDuration >= minTimeAfterLanding;

            // ★パワーゲージに応じた最低待機時間が経過するまでは、低速判定による「停止した」扱いにしない
            bool pastMinReturnTime = launchElapsedTime >= currentMinReturnTime;

            if (pastLandingGracePeriod && pastMinReturnTime && molkkyRb.linearVelocity.magnitude < 0.1f && !AreSkittlesMoving())
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
        isBoundaryReturnScheduled = false;
        yield return new WaitForSeconds(0.5f);
        canCheckStop = true;
        safetyTimeoutCoroutine = StartCoroutine(SafetyTimeoutRoutine());
    }

    IEnumerator BoundaryReturnRoutine()
    {
        yield return new WaitForSeconds(boundaryReturnDelay);

        // ★境界判定は高さを見ていないため、放物線の途中で範囲外に出ただけの
        //   「まだ空中にいる」状態で戻さないよう、着地するまで待つ
        yield return WaitUntilGroundedOrTimeout(8f);

        TriggerSpecialReturn();
    }

    IEnumerator SafetyTimeoutRoutine()
    {
        yield return new WaitForSeconds(10.0f);
        if (canCheckStop && !isCheckingTurnEnd)
        {
            // ★10秒経ってもまだ空中にいる場合、接地判定に関係なく強制的に戻していたため、
            //   着地するまで少し待ってから戻すようにする
            yield return WaitUntilGroundedOrTimeout(5f);

            if (canCheckStop && !isCheckingTurnEnd)
            {
                StartCoroutine(TurnEndRoutine(0f));
            }
        }
    }

    // ★前の投球分の「戻りタイマー」系コルーチンを止める。
    //   止めずに放置すると、ターンが正常に終わった後もバックグラウンドで生き続け、
    //   次のプレイヤーが投げる前（またはまだ投げている最中）に遅れて発火し、
    //   勝手にターンを進めてしまう不具合の原因になっていた。
    private void CancelPendingReturnCoroutines()
    {
        CancelSafetyAndBoundaryCoroutines();

        if (specialReturnCoroutine != null)
        {
            StopCoroutine(specialReturnCoroutine);
            specialReturnCoroutine = null;
        }
    }

    private void CancelSafetyAndBoundaryCoroutines()
    {
        if (safetyTimeoutCoroutine != null)
        {
            StopCoroutine(safetyTimeoutCoroutine);
            safetyTimeoutCoroutine = null;
        }

        if (boundaryReturnCoroutine != null)
        {
            StopCoroutine(boundaryReturnCoroutine);
            boundaryReturnCoroutine = null;
        }
    }

    IEnumerator TurnEndRoutine(float delayTime)
    {
        isCheckingTurnEnd = true;
        canCheckStop = false;

        // ★このターンはもう終わるので、同じ投球に紐づく残りの戻りタイマーは不要。
        //   ここで止めておかないと、後から二重にTurnEndRoutineが呼ばれて
        //   次のプレイヤーのターンを勝手に進めてしまうことがある
        //   （specialReturnCoroutine自身から呼ばれているケースもあるため、ここでは触らない）
        CancelSafetyAndBoundaryCoroutines();

        if (delayTime > 0f)
        {
            yield return new WaitForSeconds(delayTime);
        }

        playerController.ResetMolkky();
        PlaySound(turnEndSound);
        // ★モルックが手元に戻ったので、保留していた風向き選択画面を開く
        //   風を選択した場合は、選択が終わるまで交代ボタンの表示を保留する
        bool openedWindSelector = false;
        if (hasPendingWindSelection)
        {
            hasPendingWindSelection = false;
            openedWindSelector = true;
            if (WindManager.instance != null)
            {
                WindManager.instance.OpenWindSelector();
            }
        }

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
            PlaySound(winSound);
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

            if (openedWindSelector)
            {
                // 風向き選択が終わるまで交代ボタンは表示しない（OnWindDirectionSelectedで表示する）
                pendingNextTurnButtonAfterWind = true;
            }
            else if (nextTurnButtonUI != null)
            {
                nextTurnButtonUI.SetActive(true);
            }
        }

        isCheckingTurnEnd = false;
        specialReturnCoroutine = null;
    }

    // --- 【3. 変更】ターン交代時の処理 ---
    public void OnNextTurnButtonPressed()
    {
        foreach (Skittle s in skittles)
        {
            if (s != null) s.StandUp();
        }

        if (WallObstacleManager.Instance != null) // ★追加
        {
            WallObstacleManager.Instance.OnSkittlesResetComplete();
        }

        // 1. ターン交代（P1 ⇆ P2）
        currentPlayer = (currentPlayer == 1) ? 2 : 1;

        PlaySound(buttonSound);
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
        PlaySound(buttonSound);
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

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    public void OnRematchButtonPressed()
    {
        PlaySound(buttonSound);
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    public void OnTitleButtonPressed()
    {
        PlaySound(buttonSound);
        SceneManager.LoadScene(titleSceneName);
    }
    IEnumerator LoadTitleScene()
    {
        yield return new WaitForSeconds(buttonSound.length);
        SceneManager.LoadScene(titleSceneName);
    }
}