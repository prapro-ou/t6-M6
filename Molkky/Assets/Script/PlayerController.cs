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

    [Header("★モルックの手元の位置（手動設定用）")]
    public Vector3 customDefaultLocalPosition = new Vector3(0f, 0f, 0f);
    public Vector3 customDefaultLocalRotation = new Vector3(0f, 0f, 0f);

    [Header("★モルックの角度（縦横）設定")]
    public Vector3 verticalRotation = new Vector3(0f, 0f, 0f);     // 縦向きの角度
    public Vector3 horizontalRotation = new Vector3(0f, 0f, 90f);  // 横向きの角度（モデルに合わせて変更可）
    private bool isHorizontal = false;

    private float currentPower = 0f;
    private bool isChargingUp = true;

    // 💡 状態管理フラグ
    public bool isCanControl = true; // 操作可能かどうか（投げた瞬間に false にする）
    private enum State { Aiming, SettingPower, Launched }
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
        // 一時的に追加
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            GameManager.instance.GetItem(MolkkyType.Wind);
        }
        
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            GameManager.instance.GetItem(MolkkyType.Darkness);
        }
        // ここまで

        // 💡 ★最優先ガード1：投げた後（isCanControl == false）は Update 内の処理を完全遮断！
        if (!isCanControl || currentState == State.Launched)
        {
            return;
        }

        // ガード2：ゲーム開始前・終了時
        if (!GameManager.isGameStarted || GameManager.isGameFinished)
        {
            return;
        }

        // ガード3：NEXT PLAYER ボタン表示中
        if (gameManager != null && gameManager.nextTurnButtonUI != null && gameManager.nextTurnButtonUI.activeSelf)
        {
            return;
        }

        // --- 入力処理 ---
        if (Keyboard.current != null)
        {
            inputX = 0f;
            if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed) inputX = -1f;
            if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed) inputX = 1f;

            inputY = 0f;
            if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed) inputY = -1f;
            if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed) inputY = 1f;
        }

        // 1. 狙い（エイム）状態
        if (currentState == State.Aiming)
        {
            // 💡 特殊アイテム（ロケットや爆弾など）がついているか判定
            bool isRocket = GetComponentInChildren<Rocket>() != null;
            bool isBomb = GetComponentInChildren<BombImpact>() != null;
            bool isNormalMolkky = !isRocket && !isBomb; // 通常モルックかどうか

            // 💡 追加：通常モルックの時だけ、エイム中にRキーで縦横切り替え
            if (isNormalMolkky && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                isHorizontal = !isHorizontal;
                Vector3 targetRot = isHorizontal ? horizontalRotation : verticalRotation;
                molkkyRb.transform.localRotation = Quaternion.Euler(targetRot);
            }

            // 左右（Y軸回転）は通常通り操作可能
            currentRotationY += inputX * rotateSpeed * Time.deltaTime;
            
            // ★変更：ロケット時は上下角度を 0f（水平）に固定、通常時はW/Sキーで操作
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

            transform.localRotation = Quaternion.Euler(currentRotationX, currentRotationY, 0f);

            // スペースキーが押されたら「パワー調整」へ移行
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                currentState = State.SettingPower;
                currentPower = 0f;
                isChargingUp = true;

                // UIのフォーカスを外す（スペースキーがUIに連動するのを防止）
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

            // スペースキーが押されたら「発射」
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
        isCanControl = false;
        currentState = State.Launched;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // 親子関係を解除
        molkkyRb.transform.SetParent(null);
        molkkyRb.isKinematic = false;

        // 💡 1. 打ち出し角度を調整 (Y方向を 0.3f から 0.45f〜0.5f に増やして綺麗な放物線にする)
        Vector3 throwDirection = (transform.forward + Vector3.up * 0.15f).normalized;

        // 💡 2. 発射の力をさらに底上げ（currentPower に 1.5倍〜2倍などの倍率をかけることも可能です）
        float finalPower = currentPower * 1.5f; // 必要に応じて倍率（1.5fなど）を調整

        molkkyRb.AddForce(throwDirection * finalPower, ForceMode.Impulse);

        if (gameManager != null)
        {
            gameManager.OnMolkkyLaunched();
        }
        else
        {
            GameObject.Find("GameManager").GetComponent<GameManager>().OnMolkkyLaunched();
        }
    }

    public void ResetMolkky()
    {
        molkkyRb.gameObject.SetActive(false);

        // 💡 縦横フラグを初期化（縦に戻す）
        isHorizontal = false;

        // 発射台の子に戻す
        molkkyRb.transform.SetParent(transform);

        // 位置・角度リセット
        molkkyRb.transform.localPosition = customDefaultLocalPosition;
        molkkyRb.transform.localRotation = Quaternion.Euler(verticalRotation);

        transform.localRotation = Quaternion.identity;
        currentRotationX = 0f;
        currentRotationY = 0f;

        molkkyRb.isKinematic = true;
        molkkyRb.gameObject.SetActive(true);

        // 💡 状態をリセットして操作可能（true）に戻す
        isCanControl = true;
        currentState = State.Aiming;

        if (powerSlider != null)
        {
            powerSlider.value = 0f;
        }
    }
}