using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.AI;

public class DoubleGunner : MonoBehaviour
{
    
    [Header("References")]
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform firePoint1;
    [SerializeField] private Transform firePoint2;
    [SerializeField] private GameObject bulletPrefab;

    [Header("Layers")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;


    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 10f;
    private Vector3 currentPatrolPoint;
    private bool hasPatrolPoint;

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField]private float bulletSpeed = 20f;
    private bool isOnAttackCoolDown;


    [Header("Detection Settings")]
    [SerializeField] private float visionRange = 20f;
    [SerializeField] private float engagementRange = 15f;

    [Header("Materials")]
    [SerializeField] Material PatrolMaterial;  
    [SerializeField] Material ChaseMaterial; 
    [SerializeField] Material AttackMaterial; 
    [SerializeField] Material RetreatMaterial;


    private bool isPlayerVisible;
    private bool isPlayerInRange;    

    // Start is called before the first frame update
    void Awake()
    {
        
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }

        if (navAgent == null)
        {
            navAgent = GetComponent<NavMeshAgent>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        DetectPlayer();
        UpdateBehaviourState();
    }

    private void UpdateBehaviourState()
    {

        if (!isPlayerVisible && !isPlayerInRange) 
        {
            Patroling();
        }
        else if (isPlayerVisible && !isPlayerInRange) 
        {
            PeformChase();
        }
        else if (isPlayerInRange && isPlayerVisible) 
        {
            PerformAttack();
        }
    }


    private void Patroling()
    {
        if (!hasPatrolPoint)
        {
            FindPatrolPoint();
        }

        if (hasPatrolPoint)
        {
            navAgent.SetDestination(currentPatrolPoint);

            // FIX FOR "STAYING IN SAME PLACE":
            // We check if we are CLOSE enough, not exact.
            // We also check !navAgent.pathPending to make sure it finished calculating.
            if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
            {
                hasPatrolPoint = false;
            }
        }
    }
    private void PeformChase() 
    {
        if (playerTransform != null)
        {
            navAgent.SetDestination(playerTransform.position);
        }
    }
    private void PerformAttack()  
    {
        navAgent.SetDestination(transform.position);
        transform.LookAt(playerTransform);
        if (!isOnAttackCoolDown) 
        {
            Fire();
            StartCoroutine(AttackCooldownRoutine());
        }
    }
    private IEnumerator AttackCooldownRoutine()
    {
        isOnAttackCoolDown = true;
        yield return new WaitForSeconds(attackCooldown);
        isOnAttackCoolDown = false;
    }
    private void FindPatrolPoint()
    {
        for (int i = 0; i < 30; i++) // Try 30 times to find a point
        {
            // 1. Pick a random point in a sphere
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * patrolRadius;

            // 2. Ask NavMesh: "Is there a walkable floor near this random point?"
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                // 3. We found a valid spot on the NavMesh!
                currentPatrolPoint = hit.position;
                hasPatrolPoint = true;
                return; // Success, exit the function
            }
        }

        // If we tried 30 times and failed (rare), wait until next frame
        hasPatrolPoint = false;
    }
    private void Fire()
    {
        if (bulletPrefab == null || firePoint1 == null || firePoint2 == null) return;

        Rigidbody projectileRb1 = Instantiate(bulletPrefab, firePoint1.position, firePoint1.rotation).GetComponent<Rigidbody>();
        projectileRb1.velocity = firePoint1.forward * bulletSpeed;

        Rigidbody projectileRb2 = Instantiate(bulletPrefab, firePoint2.position, firePoint2.rotation).GetComponent<Rigidbody>();
        projectileRb2.velocity = firePoint2.forward * bulletSpeed;


    }
    private void DetectPlayer()
    {
        isPlayerInRange = Physics.CheckSphere(transform.position, engagementRange, playerLayer);
        isPlayerVisible = Physics.CheckSphere(transform.position, visionRange, playerLayer);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engagementRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
     


}
