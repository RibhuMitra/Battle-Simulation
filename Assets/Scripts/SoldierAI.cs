using UnityEngine;

public class SoldierAI : MonoBehaviour
{
    public float moveSpeed = 3f;

    private Vector3 targetPosition;
    private bool hasTarget;

    public void MoveTo(Vector3 position)
    {
        targetPosition = position;
        hasTarget = true;
    }

    public void Stop()
    {
        hasTarget = false;
    }

    void Update()
    {
        if (!hasTarget)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );
    }
}