using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    public Rigidbody molkkyRb;
    public Slider powerSlider;
    public Image sliderFillImage;
    public float maxForce = 40f;       // パワーの最大値
    public float chargeSpeed = 15f;    // ゲージの基本の速さ
    public float speedPerPoint = 0.3f; // 1点ごとに加算されるゲージの速さ

    [Header("ゲームマネージャーの参照")]
    public GameManager gameManager;

    [Header("回転スピードの設定")]
    public float rotateSpeed = 50f;

    [Header("★縦回転（ピッチ）調整設定")]
    public Slider spinPowerSlider;       // ★回転用スライダーUI
    public Image spinFillImage;          // ★回転ゲージのFill画像（任意）
    public float minSpinTorque = 10f;    // ★最小回転力
    public float maxSpinTorque = 100f;   // ★最大回転力
    public float spinChargeSpeed = 45f;  // ★回転ゲージのチャージスピード

    [Header("効果音")]
    public AudioSource audioSource;
    public AudioClip throwSound;        // 通常モルック
    public AudioClip rocketThrowSound;  // ミサイル

    [Header("★モルックの手元の位置（手動設定用）")]
    public Vector3 customDefaultLocalPosition = new Vector3(0f, 0f, 0f);
    public Vector3 customDefaultLocalRotation = new Vector3(0f, 0f, 0f);

    [Header("★モルックの角度（縦横）設定")]
    public Vector3 verticalRotation = new Vector3(0f, 0f, 0f);     // 縦向き時の角度
    public Vector3 horizontalRotation = new Vector3(0f, 0f, 90f);  // 横向き時の角度
    private bool isHorizontal = true; // ★ 初期値：横向き(true)

    private float currentPower = 0f;
    private bool isChargingUp = true;

    private float currentSpinPower = 0f;       // 現在の回転パワー
    private bool isSpinChargingUp = true;      // 回転チャージの方向

    // 💡 状態管理フラグ
    public bool isCanControl = true;
    private enum State { Aiming, SettingPower, SettingSpin, Launched }
    private State currentState = State.Aiming;

    private float inputX = 0f;
    private float inputY = 0f;
    private float currentRotationX = 0f;
    private float currentRotationY = 0f;

    void Start()
    {
        ResetMolkky();
    }

    void Update()
{
    // ガード1：ゲーム開始前・終了時
    if (!GameManager.isGameStarted || GameManager.isGameFinished)
    {
        return;
    }

    // ガード2：NEXT PLAYER ボタン表示中
    if (gameManager != null && gameManager.nextTurnButtonUI != null && gameManager.nextTurnButtonUI.activeSelf)
    {
        return;
    }

    // --- 1. 入力処理（投げた後もキー入力を受ける） ---
    if (Keyboard.current != null)
    {
        inputX = 0f;
        if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed) inputX = -1f;
        if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed) inputX = 1f;

        inputY = 0f;
        if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed) inputY = -1f;
        if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed) inputY = 1f;
    }

    // --- 2. 視点移動処理（狙い中と投げた後だけ動かす。パワー/回転ゲージ調整中は固定） ---
    bool isRocket = molkkyRb != null && molkkyRb.GetComponentInChildren<Rocket>() != null;

    if (currentState == State.Aiming || currentState == State.Launched)
    {
        currentRotationY += inputX * rotateSpeed * Time.deltaTime;

        if (isRocket)
        {
            currentRotationX = 0f;
        }
        else
        {
            currentRotationX -= inputY * rotateSpeed * Time.deltaTime;
            currentRotationX = Mathf.Clamp(currentRotationX, -45f, 5f);
        }

        currentRotationY = Mathf.Clamp(currentRotationY, -45f, 45f);

        // カメラ/プレイヤーの向きを更新
        transform.localRotation = Quaternion.Euler(currentRotationX, currentRotationY, 0f);
    }


    // --- ★ ガード3：投げた後はここで処理を中断（ゲージ進行やスペースキー操作をブロック） ---
    if (!isCanControl || currentState == State.Launched)
    {
        return;
    }


    // --- 3. 狙い・ゲージ調整・発射処理 ---
    // 1. 狙い（エイム）状態
    if (currentState == State.Aiming)
    {
        bool isBomb = molkkyRb.GetComponentInChildren<BombImpact>() != null;
        bool isNormalMolkky = !isRocket && !isBomb;

        // Rキーで縦横切り替え
        if (isNormalMolkky && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            isHorizontal = !isHorizontal;
            Vector3 targetRot = isHorizontal ? horizontalRotation : verticalRotation;
            molkkyRb.transform.localRotation = Quaternion.Euler(targetRot);

            if (spinPowerSlider != null)
            {
                spinPowerSlider.gameObject.SetActive(!isHorizontal);
            }
        }

        // スペースキーでパワー調整へ
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            currentState = State.SettingPower;
            currentPower = 0f;
            isChargingUp = true;

            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }
    // 2. パワー調整状態
    else if (currentState == State.SettingPower)
    {
        int currentScore = GetCurrentPlayerScore();
        float currentChargeSpeed = chargeSpeed + (currentScore * speedPerPoint);

        if (isChargingUp)
        {
            currentPower += currentChargeSpeed * Time.deltaTime;
            if (currentPower >= maxForce)
            {
                currentPower = maxForce;
                isChargingUp = false;
            }
        }
        else
        {
            currentPower -= currentChargeSpeed * Time.deltaTime;
            if (currentPower <= 0f)
            {
                currentPower = 0f;
                isChargingUp = true;
            }
        }

        if (powerSlider != null)
        {
            float fillRatio = currentPower / maxForce;
            powerSlider.value = fillRatio;

            if (sliderFillImage != null)
            {
                sliderFillImage.fillAmount = fillRatio;
            }
        }

        // スペースキー押下時
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            bool isBomb = molkkyRb.GetComponentInChildren<BombImpact>() != null;
            bool isNormalMolkky = !isRocket && !isBomb;

            if (isNormalMolkky && !isHorizontal && spinPowerSlider != null)
            {
                currentState = State.SettingSpin;
                currentSpinPower = 0f;
                isSpinChargingUp = true;
                spinPowerSlider.gameObject.SetActive(true);
            }
            else
            {
                if (spinPowerSlider != null) spinPowerSlider.gameObject.SetActive(false);
                LaunchMolkky();
            }
        }
    }
    // 3. 縦回転パワー調整状態
    else if (currentState == State.SettingSpin)
    {
        if (isSpinChargingUp)
        {
            currentSpinPower += spinChargeSpeed * Time.deltaTime;
            if (currentSpinPower >= maxSpinTorque)
            {
                currentSpinPower = maxSpinTorque;
                isSpinChargingUp = false;
            }
        }
        else
        {
            currentSpinPower -= spinChargeSpeed * Time.deltaTime;
            if (currentSpinPower <= 0f)
            {
                currentSpinPower = 0f;
                isSpinChargingUp = true;
            }
        }

        if (spinPowerSlider != null)
        {
            float fillRatio = currentSpinPower / maxSpinTorque;
            spinPowerSlider.value = fillRatio;

            if (spinFillImage != null)
            {
                spinFillImage.fillAmount = fillRatio;
            }
        }

        // スペースキーで発射
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            LaunchMolkky();
        }
    }
}

    private int GetCurrentPlayerScore()
    {
        if (gameManager != null)
        {
            return gameManager.currentScore;
        }
        return 0;
    }

    void LaunchMolkky()
    {
        bool isRocket = molkkyRb.GetComponentInChildren<Rocket>() != null;
        bool isBomb = molkkyRb.GetComponentInChildren<BombImpact>() != null;
        bool isNormalMolkky = !isRocket && !isBomb;

        isCanControl = false;
        currentState = State.Launched;

        if (spinPowerSlider != null)
        {
            spinPowerSlider.gameObject.SetActive(false);
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        molkkyRb.transform.SetParent(null);
        molkkyRb.isKinematic = false;

        Vector3 throwDirection = (transform.forward + Vector3.up * 0.15f).normalized;
        float finalPower = currentPower * 1.5f;

        molkkyRb.AddForce(throwDirection * finalPower, ForceMode.Impulse);

        // 縦向き(!isHorizontal)の時だけ縦回転を加える
        if (isNormalMolkky && !isHorizontal)
        {
            float spinRatio = (maxSpinTorque > 0f) ? currentSpinPower / maxSpinTorque : 0f;
            float finalSpinTorque = Mathf.Lerp(minSpinTorque, maxSpinTorque, spinRatio);

            molkkyRb.AddTorque(transform.right * finalSpinTorque, ForceMode.Impulse);
        }

        if (audioSource != null)
        {
            if (isRocket && rocketThrowSound != null)
            {
                audioSource.PlayOneShot(rocketThrowSound);
            }
            else if (!isRocket && throwSound != null)
            {
                audioSource.PlayOneShot(throwSound);
            }
        }

        float powerRatio = (maxForce > 0f) ? currentPower / maxForce : 0f;

        if (gameManager != null)
        {
            gameManager.OnMolkkyLaunched(powerRatio);
        }
        else
        {
            GameObject.Find("GameManager").GetComponent<GameManager>().OnMolkkyLaunched(powerRatio);
        }
    }

    public void ResetMolkky()
    {
        molkkyRb.gameObject.SetActive(false);

        // ★ 初期状態：横向き(isHorizontal = true)
        isHorizontal = true;

        molkkyRb.transform.SetParent(transform);

        molkkyRb.transform.localPosition = customDefaultLocalPosition;
        molkkyRb.transform.localRotation = Quaternion.Euler(horizontalRotation); // 横向き角度を適用

        transform.localRotation = Quaternion.identity;
        currentRotationX = 0f;
        currentRotationY = 0f;

        molkkyRb.isKinematic = true;
        molkkyRb.gameObject.SetActive(true);

        isCanControl = true;
        currentState = State.Aiming;

        if (powerSlider != null)
        {
            powerSlider.value = 0f;
        }

        // ★ 横向きスタートなので回転ゲージは非表示
        if (spinPowerSlider != null)
        {
            spinPowerSlider.value = 0f;
            spinPowerSlider.gameObject.SetActive(false);
        }
    }
}