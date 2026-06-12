using UnityEngine;

public class Combat : MonoBehaviour
{
    public Transform enemy;

    public float damage = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 1f;

    public bool enableCombatLogging = true;

    private float attackTimer;

    void Update()
    {
        if (enemy == null)
            return;

        attackTimer += Time.deltaTime;

        float distance = Vector3.Distance(
            transform.position,
            enemy.position
        );

        if (distance <= attackRange &&
            attackTimer >= attackCooldown)
        {
            Attack();
        }
    }

    void Attack()
    {
        attackTimer = 0f;

        Health health =
            enemy.GetComponent<Health>();

        if (health != null)
        {
            health.TakeDamage(damage);
        }

        if (enableCombatLogging)
        {
            Debug.Log($"{gameObject.name} attacks {enemy.name}");
        }
    }
}