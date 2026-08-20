using UnityEngine;

public class MovingWall : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float moveDistance = 4f;

    private Vector3 startPos;
    private float randomOffset;

    private void Start()
    {
        startPos = transform.position;
        randomOffset = Random.Range(0f, 10f);
    }

    public void SetRandomParams(float minSpeed, float maxSpeed)
    {
        speed = Random.Range(minSpeed, maxSpeed);
    }

    private void Update()
    {
        float offset = Mathf.PingPong((Time.time + randomOffset) * speed, moveDistance) - (moveDistance / 2f);
        transform.position = startPos + new Vector3(offset, 0f, 0f);
    }
}