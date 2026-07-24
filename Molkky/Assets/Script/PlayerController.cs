using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    public Rigidbody molkkyRb;
    public Slider powerSlider;
    public float maxForce = 40f;
    public float chargeSpeed = 20f;

    [Header("ゲームマネージャーの参照")]
    public GameManager gameManager;

    [Header("回転スピードの設定")]
    public float rotateSpeed = 50f;

    [Header("★モルックの手元の位置（手動設定用）")]
    public Vector3 customDefaultLocalPosition = new Vector3(0f, 0f, 0f);
    public Vector3 customDefaultLocalRotation = new Vector3(0f, 0f, 0f);

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
            currentRotationY += inputX * rotateSpeed * Time.deltaTime;
            currentRotationX -= inputY * rotateSpeed * Time.deltaTime;

            currentRotationX = Mathf.Clamp(currentRotationX, -45f, 0f);
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
            if (isChargingUp)
            {
                currentPower += chargeSpeed * Time.deltaTime;
                if (currentPower >= maxForce)
                {
                    currentPower = maxForce;
                    isChargingUp = false;
                }
            }
            else
            {
                currentPower -= chargeSpeed * Time.deltaTime;
                if (currentPower <= 0f)
                {
                    currentPower = 0f;
                    isChargingUp = true;
                }
            }

            if (powerSlider != null)
            {
                powerSlider.value = currentPower / maxForce;
            }

            // スペースキーが押されたら「発射」
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                LaunchMolkky();
            }
        }
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

        // 💡 ★ここを修正！正面方向(transform.forward)に少し上向き(Vector3.up * 0.3f)を足す
        Vector3 throwDirection = (transform.forward + Vector3.up * 0.3f).normalized;

        // 力を加える
        molkkyRb.AddForce(throwDirection * currentPower, ForceMode.Impulse);

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

        // 発射台の子に戻す
        molkkyRb.transform.SetParent(transform);

        // 位置・角度リセット
        molkkyRb.transform.localPosition = customDefaultLocalPosition;
        molkkyRb.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

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