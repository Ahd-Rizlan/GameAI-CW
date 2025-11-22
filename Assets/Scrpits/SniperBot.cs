using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SniperBot : MonoBehaviour, IDamageable
{
    private static int GlobalSniperCount = 0;
    private int myID;

    public enum SniperState
    {
        Patrol,
        Attack,
        Reposition,
        Search
    }

    [Header("Text Mesh")]
    [SerializeField] private TMP_Text Name;
    [SerializeField] private TMP_Text State;
    [SerializeField] private TMP_Text HP;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Pathfinding pathfindingManager;
    [SerializeField] private MeshRenderer meshRenderer;

    [Header("Health")]
    [SerializeField] private float maxHealth = 60f;
    [SerializeField] private float currentHealth;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float turnSpeed = 6f;
    [SerializeField] private float patrolRadius = 10f;

    [Header("Detection Settings")]
    [SerializeField] private float visionRange = 25f;
    [SerializeField] private float tooCloseRange = 8f;

    [Header("Combat Settings")]
    [SerializeField] private float fireRate = 3.0f;
    [SerializeField] private float nextShotTime = 0;
    [SerializeField] private float bulletSpeed = 15f;
    [SerializeField] private float bullet_Min_Damage = 10f;
    [SerializeField] private float bullet_Max_Damage = 20f;

    [Header("Material")]
    [SerializeField] private Material PatrolMaterial;
    [SerializeField] private Material ChaseMaterial; // Used for Search
    [SerializeField] private Material AttackMaterial;
    [SerializeField] private Material RetreatMaterial;

    [Header("Search Settings")]
    [SerializeField] private float searchDuration = 4f;
    private float searchTimer = 0;

    // Internal Logic variables
    private SniperState currentState = SniperState.Patrol;
    private TerrainScanner scanner;

    // Pathfinding Variables
    private Vector3[] path;
    private int targetIndex;
    private bool isMoving = false;

    // --- FIX 1: Add this variable to stop spamming the Pathfinding system ---
    private bool isWaitingForPath = false;

    void Awake()
    {
        GlobalSniperCount++;
        myID = GlobalSniperCount;
    }

    void Start()
    {
        currentHealth = maxHealth;
        scanner = GetComponent<TerrainScanner>();
        currentState = SniperState.Patrol;
        UpdateUI();
    }

    void Update()
    {
        SwitchState();
        UpdateUI();
    }

    private void SwitchState()
    {
        switch (currentState)
        {
            case SniperState.Patrol:
                Patrol();
                break;
            case SniperState.Attack:
                Attack();
                break;
            case SniperState.Reposition:
                Reposition();
                break;
            case SniperState.Search:
                Search();
                break;
            default:
                Patrol();
                break;
        }
    }

    // --- STATE LOGIC ---

    private void Patrol()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (meshRenderer) meshRenderer.material = PatrolMaterial;

        // Transition: See Player
        if (distToPlayer < visionRange)
        {
            currentState = SniperState.Attack;
            return;
        }

        // Logic: Move randomly
        // --- FIX 2: Only request if not moving AND not already waiting ---
        if (!isMoving && !isWaitingForPath)
        {
            RequestRandomPath();
        }
    }

    private void Attack()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (meshRenderer) meshRenderer.material = AttackMaterial;

        // Logic: Stop and Shoot
        if (isMoving) StopMoving();
        AimAndShoot();

        // --- DELETED: The check for 'tooCloseRange' is gone! ---
        // It will now stand its ground even if you are 1 meter away.

        // Transition: Player ran away -> Go to SEARCH
        if (distToPlayer > visionRange)
        {
            searchTimer = Time.time + searchDuration;
            currentState = SniperState.Search;
            return;
        }
    }

    private void Reposition()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (meshRenderer) meshRenderer.material = RetreatMaterial;

        // Logic: Run away
        if (!isMoving && !isWaitingForPath)
        {
            RequestRetreatPath();
        }

        // Condition: Keep running until we are far enough away (Safe Distance)
        // Once we are safe, turn around and start shooting again
        if (distToPlayer > tooCloseRange * 2.0f) // Increased multiplier for safety
        {
            StopMoving();
            currentState = SniperState.Attack;
        }
    }

    private void Search()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (meshRenderer) meshRenderer.material = ChaseMaterial;

        // Logic: Spin around to look for target
        if (isMoving) StopMoving();

        transform.Rotate(Vector3.up * turnSpeed * 1f * Time.deltaTime);

        // Transition: Found Player again
        if (distToPlayer < visionRange)
        {
            currentState = SniperState.Attack;
            return;
        }

        // Transition: Time up -> Go back to Patrol
        if (Time.time > searchTimer)
        {
            currentState = SniperState.Patrol;
        }
    }

    // --- ACTIONS ---

    private void AimAndShoot()
    {
        if (player == null) return;

        // Rotate towards player (Y-axis only)
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }

        // Fire Logic
        if (Time.time > nextShotTime)
        {
            nextShotTime = Time.time + fireRate;

            GameObject b = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            Bullet script = b.GetComponent<Bullet>();

            if (script != null)
            {
                script.minDamage = bullet_Min_Damage;
                script.maxDamage = bullet_Max_Damage;
                script.Speed = bulletSpeed;
                script.owner = this.gameObject;
            }

            Rigidbody rb = b.GetComponent<Rigidbody>();
            if (rb) rb.velocity = firePoint.forward * (script ? script.Speed : 10f);
        }
    }

    // --- PATHFINDING HELPERS ---

    void RequestRandomPath()
    {
        isWaitingForPath = true; // --- FIX 4: Set Flag ---
        Vector3 randomSpot = transform.position + new Vector3(Random.Range(-patrolRadius, patrolRadius), 0, Random.Range(-patrolRadius, patrolRadius));
        pathfindingManager.FindPath(transform.position, randomSpot, this);
    }

    void RequestRetreatPath()
    {
        isWaitingForPath = true; // --- FIX 4: Set Flag ---
        Vector3 dir = (transform.position - player.position).normalized;
        Vector3 retreatSpot = transform.position + (dir * 10f);
        pathfindingManager.FindPath(transform.position, retreatSpot, this);
    }

    void StopMoving()
    {
        StopCoroutine("FollowPath");
        isMoving = false;
        path = null;
    }

    // Callback called by Pathfinding script
    public void OnPathFound(Vector3[] newPath, bool pathSuccessful)
    {
        isWaitingForPath = false; // --- FIX 5: Reset Flag ---

        // --- FIX 6: If we switched to Attack mode while waiting for path, ignore this path ---
        if (currentState == SniperState.Attack) return;

        if (pathSuccessful)
        {
            path = newPath;
            isMoving = true;
            StopCoroutine("FollowPath");
            StartCoroutine("FollowPath");
        }
    }

    IEnumerator FollowPath()
    {
        if (path.Length > 0)
        {
            targetIndex = 0;
            Vector3 currentWaypoint = path[0];

            // Fix sinking
            currentWaypoint.y = transform.position.y;

            while (true)
            {
                if (Vector3.Distance(transform.position, currentWaypoint) < 0.1f)
                {
                    targetIndex++;
                    if (targetIndex >= path.Length)
                    {
                        isMoving = false;
                        yield break;
                    }
                    currentWaypoint = path[targetIndex];
                    currentWaypoint.y = transform.position.y; // Fix sinking
                }

                float speedMult = (scanner != null) ? scanner.GetSpeedMultiplier() : 1f;

                transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, moveSpeed * speedMult * Time.deltaTime);

                Vector3 dir = currentWaypoint - transform.position;
                if (dir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * turnSpeed);
                }

                yield return null;
            }
        }
    }

    // --- INTERFACE IMPLEMENTATION ---

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        // THE TRIGGER: If I get shot, I run away immediately!
        if (currentState != SniperState.Reposition)
        {
            currentState = SniperState.Reposition;
            StopMoving();
            // The Update loop will pick this up and call Reposition() next frame
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Destroy(gameObject, 0.2f);
    }

    // --- UI & DEBUG ---

    void UpdateUI()
    {
        if (Name && State && HP != null)
        {
            Name.text = "ID: " + myID.ToString("D2");
            State.text = "STATE: " + currentState.ToString();
            HP.text = "HP: " + currentHealth.ToString("F0") + "/" + maxHealth.ToString("F0");

            HP.color = (currentHealth <= maxHealth * 0.3f) ? Color.red : Color.green;

            if (Camera.main != null)
            {
                Name.transform.rotation = Camera.main.transform.rotation;
                State.transform.rotation = Camera.main.transform.rotation;
                HP.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, tooCloseRange);
    }
}