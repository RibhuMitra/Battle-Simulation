using UnityEngine;

public class SoldierBrain : MonoBehaviour
{
    [Header("State")]
    public SoldierState currentState = SoldierState.Idle;

    [Header("Retreat Settings")]
    public float baseRetreatThreshold = 0.2f; // 20% health
    public float outnumberedRetreatThreshold = 0.4f; // 40% health
    public float retreatDistanceMultiplier = 5f;

    [Header("Threat Assessment Weights")]
    public float distanceWeight = 0.7f;
    public float healthWeight = 0.3f;

    [Header("Debugging")]
    public bool enableBrainLogging = true;

    [Header("Components")]
    public Health health;
    public Combat combat;
    public SoldierAI soldierAI;
    public SoldierPerception perception;
    public SoldierMemory memory;
    public Team team;

    private float originalMoveSpeed = 3f;

    private void Start()
    {
        if (health == null) health = GetComponent<Health>();
        if (combat == null) combat = GetComponent<Combat>();
        if (soldierAI == null) soldierAI = GetComponent<SoldierAI>();
        if (soldierAI != null) originalMoveSpeed = soldierAI.moveSpeed;
        if (perception == null) perception = GetComponent<SoldierPerception>();
        if (memory == null) memory = GetComponent<SoldierMemory>();
        if (team == null) team = GetComponent<Team>();
    }

    private void Update()
    {
        if (health != null && health.CurrentHealth <= 0)
        {
            currentState = SoldierState.Idle;
            if (soldierAI != null) soldierAI.Stop();
            if (combat != null) combat.enemy = null;
            return;
        }

        EvaluateState();
    }

    private void EvaluateState()
    {
        // 1. Check for retreat conditions
        bool isOutnumbered = perception != null && perception.nearbyEnemies > (perception.nearbyAllies + 1);
        float currentRetreatThreshold = isOutnumbered ? outnumberedRetreatThreshold : baseRetreatThreshold;
        bool shouldRetreat = health != null && (health.HealthPercentage < currentRetreatThreshold) && (perception != null && perception.nearbyEnemies > 0);

        if (shouldRetreat)
        {
            currentState = SoldierState.Retreat;
            ExecuteRetreat();
            return;
        }
        else
        {
            // Restore normal speed if we were in the Retreat state
            if (currentState == SoldierState.Retreat && soldierAI != null)
            {
                soldierAI.moveSpeed = originalMoveSpeed;
            }
        }

        // 2. Select target (Threat Assessment)
        Transform targetEnemy = ChooseBestTarget();
        Vector3 targetPosition = Vector3.zero;
        bool hasTarget = false;

        if (targetEnemy != null)
        {
            targetPosition = targetEnemy.position;
            hasTarget = true;
            if (memory != null)
            {
                memory.UpdateMemory(targetEnemy, targetPosition);
            }
        }
        else if (memory != null && memory.HasMemory)
        {
            targetEnemy = memory.LastSeenEnemy;
            targetPosition = memory.LastKnownEnemyPosition;
            hasTarget = true;
        }

        // 3. Apply state and execute actions
        if (hasTarget)
        {
            float distance = Vector3.Distance(transform.position, targetPosition);
            float range = combat != null ? combat.attackRange : 2f;

            if (distance <= range)
            {
                currentState = SoldierState.Attack;
                if (combat != null) combat.enemy = targetEnemy;
                if (soldierAI != null) soldierAI.Stop();
            }
            else
            {
                currentState = SoldierState.Move;
                if (combat != null) combat.enemy = null;
                if (soldierAI != null) soldierAI.MoveTo(targetPosition);
            }
        }
        else
        {
            currentState = SoldierState.Search;
            if (combat != null) combat.enemy = null;
            if (soldierAI != null) soldierAI.Stop();
        }
    }

    private void ExecuteRetreat()
    {
        if (combat != null) combat.enemy = null;

        Vector3 runDirection = Vector3.zero;

        // Flee from nearest enemy
        if (perception != null && perception.nearestEnemy != null)
        {
            runDirection = (transform.position - perception.nearestEnemy.position).normalized;
        }
        else
        {
            runDirection = transform.forward;
        }

        // Bias retreat towards nearest ally for protection
        if (perception != null && perception.nearestAlly != null)
        {
            Vector3 toAlly = (perception.nearestAlly.position - transform.position).normalized;
            runDirection = (runDirection * 0.6f + toAlly * 0.4f).normalized;
        }

        runDirection.y = 0f; // Keep movement in horizontal plane

        Vector3 retreatDestination = transform.position + runDirection * retreatDistanceMultiplier;

        if (enableBrainLogging)
        {
            Debug.Log($"{gameObject.name} (Retreating) | Current Pos: {transform.position} | Target Pos: {retreatDestination}");
        }

        if (soldierAI != null)
        {
            // Increase speed for tactical sprint fleeing
            soldierAI.moveSpeed = originalMoveSpeed * 1.5f;
            soldierAI.MoveTo(retreatDestination);
        }
    }

    private Transform ChooseBestTarget()
    {
        if (perception == null || perception.visibleEnemies.Count == 0)
            return null;

        Transform bestTarget = null;
        float highestScore = -Mathf.Infinity;

        foreach (Transform enemyTransform in perception.visibleEnemies)
        {
            if (enemyTransform == null) continue;

            float distance = Vector3.Distance(transform.position, enemyTransform.position);
            Health enemyHealth = enemyTransform.GetComponentInParent<Health>();
            float enemyHealthPercent = enemyHealth != null ? enemyHealth.HealthPercentage : 1f;

            // Target Scoring Formula:
            // High score = Priority target. We prioritize closer enemies and lower-health enemies.
            float distanceScore = 1f / Mathf.Max(distance, 0.1f);
            float healthScore = 1f - enemyHealthPercent;

            float totalScore = (distanceScore * distanceWeight) + (healthScore * healthWeight);

            if (totalScore > highestScore)
            {
                highestScore = totalScore;
                bestTarget = enemyTransform;
            }
        }

        return bestTarget;
    }
}
