using UnityEngine;
using System.Collections.Generic;

public class SoldierPerception : MonoBehaviour
{
    [Header("Vision")]
    public float visionRadius = 10f;

    [Header("Detected Units")]
    public Transform nearestEnemy;
    public Transform nearestAlly;

    public int nearbyEnemies;
    public int nearbyAllies;

    public List<Transform> visibleEnemies = new List<Transform>();
    public List<Transform> visibleAllies = new List<Transform>();

    private Team myTeam;

    private void Start()
    {
        myTeam = GetComponent<Team>();
    }

    private void Update()
    {
        ScanBattlefield();
    }

    void ScanBattlefield()
    {
        nearbyEnemies = 0;
        nearbyAllies = 0;

        nearestEnemy = null;
        nearestAlly = null;

        visibleEnemies.Clear();
        visibleAllies.Clear();

        float closestEnemyDistance = Mathf.Infinity;
        float closestAllyDistance = Mathf.Infinity;

        Collider[] units =
            Physics.OverlapSphere(
                transform.position,
                visionRadius
            );

        foreach (Collider unit in units)
        {
            if (unit.gameObject == gameObject)
                continue;

            Team otherTeam =
                unit.GetComponent<Team>();

            if (otherTeam == null)
                continue;

            float distance =
                Vector3.Distance(
                    transform.position,
                    unit.transform.position
                );

            // ENEMY
            if (otherTeam.faction != myTeam.faction)
            {
                nearbyEnemies++;
                visibleEnemies.Add(unit.transform);

                if (distance < closestEnemyDistance)
                {
                    closestEnemyDistance = distance;
                    nearestEnemy = unit.transform;
                }
            }
            // ALLY
            else
            {
                nearbyAllies++;
                visibleAllies.Add(unit.transform);

                if (distance < closestAllyDistance)
                {
                    closestAllyDistance = distance;
                    nearestAlly = unit.transform;
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            visionRadius
        );
    }
}