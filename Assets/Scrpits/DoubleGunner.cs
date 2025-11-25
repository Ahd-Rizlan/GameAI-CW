using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class DoubleGunner : MonoBehaviour, IDamageable
{
    private static int GlobalGunnerCount = 0;
    private int myID;
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
    [SerializeField] private float patrolRadius = 20f; // Range to find random points
    [SerializeField] private float patrolWaitTime = 1f; // How long to wait at each point
    private float waitTimer = 0f;

    [Header("Attack Settings")]
    [SerializeField] private float nextShootTime = 0;
    [SerializeField] private float FireRate = 2f;
    [SerializeField] private float bulletSpeed = 7f;
    [SerializeField] private float bullet_Min_Damage = 5f;
    [SerializeField] private float bullet_Max_Damage = 10f;

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

    GunnerState currentState = GunnerState.Patrol;

    void Awake()
    {
        GlobalGunnerCount++;
        myID = GlobalGunnerCount;
        currentHealth = maxHealth;
    }

    void Start()
    {
        // Auto-assign Player
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        scanner = GetComponent<TerrainScanner>();
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.speed = normalSpeed;

        // Start patrolling immediately
        SetRandomPatrolDestination();
        UpdateUI();
    }

    void Update()
    {
        if (playerTransform == null) return;

        SwitchState();
        HandleTerrainSpeed();
        UpdateUI();
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

    private void Patrol()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        
        if (distanceToPlayer <= engagementRange)
        {
            currentState = GunnerState.Chase;
            navAgent.isStopped = false;
            return;
        }

        if (meshRenderer) meshRenderer.material = PatrolMaterial;
        State.color = (PatrolMaterial != null) ? PatrolMaterial.color : Color.blue;

        
        if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= patrolWaitTime)
            {
                SetRandomPatrolDestination();
                waitTimer = 0f;
            }
        }
    }

    void SetRandomPatrolDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;
        NavMeshHit hit;

        // Find a valid point on the NavMesh
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
        {
            navAgent.SetDestination(hit.position);
        }
    }

    private void Chase()
    {
        if (meshRenderer) meshRenderer.material = ChaseMaterial;
        State.color = (ChaseMaterial != null) ? ChaseMaterial.color : Color.yellow;

        navAgent.SetDestination(playerTransform.position);
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= AttackRange)
        {
            currentState = GunnerState.Attack;
            return;
        }

        if (distanceToPlayer > engagementRange)
        {
            currentState = GunnerState.Patrol;
            navAgent.ResetPath();
            return;
        }
    }

    private void Attack()
    {
        navAgent.ResetPath(); // Stop moving to shoot
        if (meshRenderer) meshRenderer.material = AttackMaterial;
        State.color = (AttackMaterial != null) ? AttackMaterial.color : Color.red;

        transform.LookAt(playerTransform);

        if (Time.time > nextShootTime)
        {
            nextShootTime = Time.time + FireRate;
            FireOneBullet(Gun_01);
            FireOneBullet(Gun_02);
        }

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

    private void Retreat()
    {
        if (!isRetreating)
        {
            if (meshRenderer) meshRenderer.material = RetreatMaterial;
            State.color = (RetreatMaterial != null) ? RetreatMaterial.color : Color.magenta;
            HP.color = Color.red;

            navAgent.isStopped = false;
            Vector3 coverPos = FindCoverPosition();
            navAgent.SetDestination(coverPos);
            isRetreating = true;
        }

        if (!navAgent.pathPending && navAgent.remainingDistance < 0.5f)
        {
            transform.LookAt(playerTransform);
            currentHealth += Time.deltaTime * 10f;
            HP.text = "HP: " + currentHealth.ToString("F0") + "/" + maxHealth.ToString("F0");

            if (currentHealth >= maxHealth * 0.5f)
            {
                currentHealth = maxHealth * 0.5f;
                HP.color = Color.green;
                isRetreating = false;
                currentState = GunnerState.Patrol;
            }
        }
    }

    private Vector3 FindCoverPosition()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * retreatDistance;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 2f, NavMesh.AllAreas))
            {
                Vector3 possibleCoverSpot = hit.position;
                Vector3 directionToPlayer = (playerTransform.position - possibleCoverSpot).normalized;
                float distanceToPlayer = Vector3.Distance(possibleCoverSpot, playerTransform.position);

                if (Physics.Raycast(possibleCoverSpot, directionToPlayer, distanceToPlayer, wallLayer))
                {
                    return possibleCoverSpot;
                }
            }
        }

        Vector3 runAwayDirection = (transform.position - playerTransform.position).normalized;
        Vector3 runAwayPos = transform.position + runAwayDirection * retreatDistance;
        NavMeshHit finalHit;
        if (NavMesh.SamplePosition(runAwayPos, out finalHit, 2f, NavMesh.AllAreas))
        {
            return finalHit.position;
        }
        return transform.position;
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        if (HP) HP.text = "HP: " + currentHealth.ToString("F0") + "/" + maxHealth.ToString("F0");

        if (currentHealth <= 0) Destroy(gameObject, 0.5f);
    }

    public void Die()
    {
        Destroy(gameObject, 0.2f);
    }

    private void HandleTerrainSpeed()
    {
        float speedMultiplier = 1f;
        if (scanner != null) speedMultiplier = scanner.GetSpeedMultiplier();
        float targetSpeed = normalSpeed * speedMultiplier;
        navAgent.speed = Mathf.Lerp(navAgent.speed, targetSpeed, Time.deltaTime * 5f);
    }

    void UpdateUI()
    {
        if (Name && State && HP != null)
        {
            Name.text = "ID: " + myID.ToString("D2");
            State.text = "STATE: " + currentState.ToString();
            HP.text = "HP: " + currentHealth.ToString("F0") + "/" + maxHealth.ToString("F0");

            if (Camera.main != null)
            {
                Name.transform.rotation = Camera.main.transform.rotation;
                State.transform.rotation = Camera.main.transform.rotation;
                HP.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }

    void FireOneBullet(Transform spawnPoint)
    {
        if (spawnPoint == null) return;
        GameObject b = Instantiate(Bullet, spawnPoint.position, spawnPoint.rotation);
        Bullet script = b.GetComponent<Bullet>();
        if (script != null)
        {
            script.minDamage = bullet_Min_Damage;
            script.maxDamage = bullet_Max_Damage;
            script.Speed = bulletSpeed;
            script.owner = this.gameObject;
        }
        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb) rb.velocity = spawnPoint.forward * (script ? script.Speed : 10f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engagementRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
}