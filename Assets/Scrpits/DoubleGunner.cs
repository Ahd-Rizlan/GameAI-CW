using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class DoubleGunner : MonoBehaviour,IDamageable
{

    public enum GunnerState
    {
        Patrol,
        Chase,
        Attack,
        Retreat
    }
    private TerrainScanner scanner;

    [Header("Text Mesh")]
    [SerializeField] private TMP_Text Name;
    [SerializeField] private TMP_Text State;
    [SerializeField] private TMP_Text HP;
    

    [Header("References")]
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform Gun_01;
    [SerializeField] private Transform Gun_02;
    [SerializeField] private GameObject Bullet;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100;
    [SerializeField] private float currentHealth;



    [Header("Patrol Settings")]
    [SerializeField] Vector3[] PatrolPoints;  
    int nextPatrolPoint = 0;

    [Header("Attack Settings")]
    [SerializeField] private float nextShootTime = 0;
    [SerializeField]private float FireRate = 2f;

    [Header("Movement")]
    [SerializeField] private float normalSpeed = 3.5f;

    [Header("Detection Settings")]
    [SerializeField] private float visionRange = 20f;
    [SerializeField] private float engagementRange = 15f;
    [SerializeField] private float AttackRange = 10f;

    [Header("Material")]
    [SerializeField] private Material PatrolMaterial;
    [SerializeField] private Material ChaseMaterial;
    [SerializeField] private Material AttackMaterial;
    [SerializeField] private Material RetreatMaterial;

    [Header("Cover Settings")]
    [SerializeField] private LayerMask wallLayer; 
    [SerializeField] private float retreatDistance = 10f;
    [SerializeField] private bool isRetreating = false;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    GunnerState currentState = GunnerState.Patrol;


    void Start()
    {
        currentHealth = maxHealth;
        scanner = GetComponent<TerrainScanner>();
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.SetDestination(PatrolPoints[nextPatrolPoint]);
        navAgent.speed = normalSpeed;
        Name.text = "ID: "+"Double Gunner";
        State.text ="STATE: " + currentState.ToString();
        HP.text = "HP: " + currentHealth.ToString("F0") + "/" + maxHealth.ToString("F0");
        HP.color = Color.green;
    }

    void Update()
    {
        SwitchState();
        HandleTerrainSpeed();
        State.text = "STATE: " + currentState.ToString();
        
    }


    private void SwitchState()
    {
        switch (currentState)
        {
            case GunnerState.Patrol:
                Patrol();
                break;
            case GunnerState.Chase:
                Chase();
                break;
            case GunnerState.Attack:
                Attack();
                break;
            case GunnerState.Retreat:
                Retreat();
                break;
            
            default:
                Patrol();
                break;
        }
    }


    private void Retreat()
    {
        
        // 1. Find a spot if we haven't already
        if (!isRetreating)
        {
            meshRenderer.material = RetreatMaterial;
            State.color = RetreatMaterial.color;
            HP.color = Color.red;
            navAgent.isStopped = false; // Make sure we can move
            Vector3 coverPos = FindCoverPosition();
            navAgent.SetDestination(coverPos);
            isRetreating = true;
        }

        // 2. Once we arrive at the cover spot
        if (!navAgent.pathPending && navAgent.remainingDistance < 0.5f)
        {
            // Turn to face the player (defensive stance)
            transform.LookAt(playerTransform);

            // Heal over time
            currentHealth += Time.deltaTime * 10f; // Heal 10 HP per second

            // 3. Transition: If healed to 50%, go back to Patrol
            if (currentHealth >= maxHealth * 0.5f)
            {
                currentHealth = maxHealth * 0.5f; // Cap it
                isRetreating = false; // Reset for next time
                currentState = GunnerState.Patrol;
            }
        }
    }

    private void Attack()
    {
        navAgent.ResetPath();
        meshRenderer.material = AttackMaterial;
        State.color = AttackMaterial.color;
        transform.LookAt(playerTransform);

        //shooting login
        if (Time.time > nextShootTime)
        {
            nextShootTime = Time.time + FireRate;
            GameObject bullet_01 = Instantiate(Bullet, Gun_01.position, Gun_01.rotation);
            Rigidbody rb_01 = bullet_01.GetComponent<Rigidbody>();
            rb_01.velocity = transform.forward * bullet_01.GetComponent<Bullet>().Speed;


            GameObject bullet_02 = Instantiate(Bullet, Gun_02.position, Gun_02.rotation);
            Rigidbody rb_02 = bullet_02.GetComponent<Rigidbody>();
            rb_02.velocity = transform.forward * bullet_02.GetComponent<Bullet>().Speed;

        }

        //Transition back to chase
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > AttackRange)
        {
            currentState = GunnerState.Chase;

        }
        if (currentHealth < maxHealth * 0.3f)
        {
            currentState = GunnerState.Retreat;
        }

    }

    private void Chase()
    {
        meshRenderer.material = ChaseMaterial;
        State.color = ChaseMaterial.color;

        navAgent.SetDestination(playerTransform.position);
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        //Transation to Attach Close enough
        if (distanceToPlayer <= AttackRange)
        {
            currentState = GunnerState.Attack;
            return;
        }
        //Transation toif Player gets away
        if (distanceToPlayer > engagementRange)
        {
            currentState = GunnerState.Patrol;
            navAgent.ResetPath();
            return;

        }

    }


    private void Patrol()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= engagementRange)
        {
            currentState = GunnerState.Chase;
            return;
        }
        
        if (!navAgent.hasPath || navAgent.velocity.sqrMagnitude == 0f)
        {
            meshRenderer.material = PatrolMaterial;
            State.color = PatrolMaterial.color;

            nextPatrolPoint = (nextPatrolPoint + 1) % PatrolPoints.Length;
            navAgent.SetDestination((PatrolPoints[nextPatrolPoint]));
        }
    }


    private Vector3 FindCoverPosition()
    {
        // Try 5 times to find a spot hidden behind a wall
        for (int i = 0; i < 5; i++)
        {
            // Pick a random spot nearby
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * retreatDistance;
            NavMeshHit hit;

            // Check if that spot is valid on the NavMesh
            if (NavMesh.SamplePosition(randomPoint, out hit, 2f, NavMesh.AllAreas))
            {
                Vector3 possibleCoverSpot = hit.position;

                // Raycast Check: Can the player see this spot?
                Vector3 directionToPlayer = (playerTransform.position - possibleCoverSpot).normalized;
                float distanceToPlayer = Vector3.Distance(possibleCoverSpot, playerTransform.position);

                // If the ray hits a "Wall" (obstacleMask) before hitting the player, it is safe.
                if (Physics.Raycast(possibleCoverSpot, directionToPlayer, distanceToPlayer, wallLayer))
                {
                    return possibleCoverSpot; // Found a hidden spot!
                }
            }
        }

        // Fallback: If no walls found, just run directly away from the player
        Vector3 runAwayDirection = (transform.position - playerTransform.position).normalized;
        Vector3 runAwayPos = transform.position + runAwayDirection * retreatDistance;

        NavMeshHit finalHit;
        if (NavMesh.SamplePosition(runAwayPos, out finalHit, 2f, NavMesh.AllAreas))
        {
            return finalHit.position;
        }

        return transform.position; // Stay put if trapped
    }



    public void TakeDamage(float damageAmount)
    {

        currentHealth -= damageAmount;
        HP.text = "HP: " + currentHealth.ToString("F0") + "/" + maxHealth.ToString("F0");

        if (currentHealth <= 0)
        {
            Destroy(gameObject,0.5f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engagementRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }

    public void Die()
    {
        // Enemy death logic
        Destroy(gameObject, 0.2f);
    }

    private void HandleTerrainSpeed()
    {
        float speedMultiplier = 1f;
        if (scanner != null)
        {
            speedMultiplier = scanner.GetSpeedMultiplier();
        }

        float targetSpeed = normalSpeed * speedMultiplier;
        navAgent.speed = Mathf.Lerp(navAgent.speed, targetSpeed, Time.deltaTime * 5f);
    }

}
