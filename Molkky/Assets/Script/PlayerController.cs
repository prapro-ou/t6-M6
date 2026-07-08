using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Rigidbody molkkyRb;
    public Slider powerSlider;
    public float maxForce = 40f;
    public float chargeSpeed = 20f;

    [Header("回転スピードの設定")]
    public float rotateSpeed = 50f; // 💡 移動ではなく回転の速度にします

    [Header("★モルックの手元の位置（手動設定用）")]
    public Vector3 customDefaultLocalPosition = new Vector3(0f, 0f, 0f);
    public Vector3 customDefaultLocalRotation = new Vector3(0f, 0f, 0f);

    private float currentPower = 0f;
    private bool isAiming = true;
    private bool isSettingPower = false;
    private bool isChargingUp = true;

    // 入力値を保存する変数
    private float inputX = 0f;
    private float inputY = 0f;

    // 現在の回転角度を記録する変数
    private float currentRotationX = 0f;
    private float currentRotationY = 0f;

    void Start()
    {
        ResetMolkky();
    }

    void Update()
    {
        if (!GameManager.isGameStarted || GameManager.isGameFinished)
        {
            return;
        }


        // 💡 New Input SystemでのWASD・矢印キーの入力を正しく取得
        if (Keyboard.current != null)
        {
            inputX = 0f;
            if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed) inputX = -1f;
            if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed) inputX = 1f;

            inputY = 0f;
            if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed) inputY = -1f;
            if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed) inputY = 1f;
        }

        if (isAiming)
        {
            // 💡 修正ポイント：位置（X）を動かすのではなく、角度（Rotation）を変化させます
            currentRotationY += inputX * rotateSpeed * Time.deltaTime; // 左右（A/D）で首振り
            currentRotationX -= inputY * rotateSpeed * Time.deltaTime; // 上下（W/S）で仰角調整

            // 上下の角度がひっくり返らないように制限（下10度〜上45度まで）
            currentRotationX = Mathf.Clamp(currentRotationX, -45f, 0f);
            currentRotationY = Mathf.Clamp(currentRotationY, -45f, 45f);

            // 計算した角度をプレイヤー（Launcher）の回転に適用
            transform.localRotation = Quaternion.Euler(currentRotationX, currentRotationY, 0f);

            // 1回目のスペースキー：チャージ開始
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                isAiming = false;
                isSettingPower = true;
                currentPower = 0f;
                isChargingUp = true;
            }
        }
        else if (isSettingPower)
        {
            // ゲージの往復チャージ処理
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

            powerSlider.value = currentPower / maxForce;

            // 2回目のスペースキー：狙ったパワーで発射！
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                LaunchMolkky();
            }
        }
    }

    void LaunchMolkky()
    {
        isSettingPower = false;
        molkkyRb.isKinematic = false;

        // 💡 修正ポイント：プレイヤー自身が向いている「正面方向（transform.forward）」に向けて真っ直ぐ発射します！
        // これにより、上下左右で狙った方向に正しく飛ぶようになります。
        molkkyRb.AddForce(transform.forward * currentPower, ForceMode.Impulse);

        // GameManagerに発射を通知
        GameObject.Find("GameManager").GetComponent<GameManager>().OnMolkkyLaunched();
    }

    public void ResetMolkky()
    {
        molkkyRb.gameObject.SetActive(false);

        // 位置と角度を手元にリセット
        molkkyRb.transform.localPosition = customDefaultLocalPosition;

        // 💡 モルック自体は「横向きに寝かせた状態(X:90)」を維持して手元に戻します
        molkkyRb.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        // プレイヤー自身の回転（狙い）も正面にリセット
        transform.localRotation = Quaternion.identity;
        currentRotationX = 0f;
        currentRotationY = 0f;

        molkkyRb.isKinematic = true;
        molkkyRb.gameObject.SetActive(true);

        isAiming = true;
        isSettingPower = false;
        powerSlider.value = 0f;
    }
}