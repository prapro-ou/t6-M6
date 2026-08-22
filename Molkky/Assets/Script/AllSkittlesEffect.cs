using UnityEngine;

// レアアイテム「全スキットル一撃」の効果本体。
// フィールド上の全スキットルに力を加えて倒す（Bomb.csの爆風処理と同じ考え方だが、距離を無視して全ピンが対象）。
public static class AllSkittlesEffect
{
    public static void KnockDownAll(float force, float upwardsModifier)
    {
        Skittle[] allSkittles = Object.FindObjectsByType<Skittle>(FindObjectsInactive.Exclude);

        foreach (Skittle skittle in allSkittles)
        {
            Rigidbody rb = skittle.GetComponent<Rigidbody>();
            if (rb == null || rb.isKinematic) continue;

            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            Vector3 impulse = (randomDirection + Vector3.up * upwardsModifier) * force;

            rb.AddForce(impulse, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * force, ForceMode.Impulse);
        }
    }
}
