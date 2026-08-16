using UnityEngine;

public class ItemBox : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // ぶつかったのがモルックかどうか判定（Tag "Molkky" または Rigidbody等で判定）
        if (other.CompareTag("Molkky") || other.GetComponentInParent<MolkkyItemHandler>() != null)
        {
            // ボム(1) か ロケット(2) か 暗闇(3) か 風(4) をランダム取得
            MolkkyType randomItem = (MolkkyType)Random.Range(1, 5);

            // GameManagerに「現在のプレイヤーの次ターンアイテム」として保存
            GameManager.instance.GetItem(randomItem);

            // 獲得エフェクトなどを出す場合はここに追加

            // アイテムボックスを削除
            Destroy(gameObject);
        }
    }
}