using UnityEngine;

public class SoldierMemory : MonoBehaviour
{
    [Header("Memory Settings")]
    public float memoryDuration = 5f;

    private Transform lastSeenEnemy;
    private Vector3 lastKnownEnemyPosition;
    private float lastSeenTime = -999f;

    public Transform LastSeenEnemy => lastSeenEnemy;
    public Vector3 LastKnownEnemyPosition => lastKnownEnemyPosition;
    public float LastSeenTime => lastSeenTime;

    public bool HasMemory
    {
        get
        {
            if (lastSeenEnemy == null)
                return false;
            return (Time.time - lastSeenTime) <= memoryDuration;
        }
    }

    public void UpdateMemory(Transform enemy, Vector3 position)
    {
        lastSeenEnemy = enemy;
        lastKnownEnemyPosition = position;
        lastSeenTime = Time.time;
    }

    public void ClearMemory()
    {
        lastSeenEnemy = null;
        lastKnownEnemyPosition = Vector3.zero;
        lastSeenTime = -999f;
    }
}
