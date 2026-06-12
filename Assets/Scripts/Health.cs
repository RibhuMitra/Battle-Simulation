using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;

    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float HealthPercentage => maxHealth > 0 ? (currentHealth / maxHealth) : 0f;

    public bool enableCombatLogging = true;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (enableCombatLogging)
        {
            Debug.Log($"{gameObject.name} HP = {currentHealth}");
        }

        if(currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (enableCombatLogging)
        {
            Debug.Log($"{gameObject.name} Died");
        }

        Destroy(gameObject);
    }
}