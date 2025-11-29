using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class SingleGunner : MonoBehaviour, IDamageable
{
    private static int GlobalSniperCount = 0;
    private int myID;

    public enum SingleGunnerState
    {
        Patrol,
        Chase,
        Attack,
        Reposition,
        Search
    }

    [Header("Text Mesh")]
    [SerializeField] private TMP_Text Name;
    [SerializeField] private TMP_Text State;
    [SerializeField] private TMP_Text HP;

    [Header("References")]
     private Transform player; // Will be auto-assigned
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    private Pathfinding pathfindingManager; // Will be auto-assigned
    [SerializeField] private MeshRenderer meshRenderer;

    [Header("Health")]
    [SerializeField] private float maxHealth = 60f;
    [SerializeField] private float currentHealth;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float turnSpeed = 3f;
    [SerializeField] private float patrolRadius = 20f;
    private float repathTimer = 0;
    private float repathRate = 0.5f;

    [Header("Detection Settings")]
    [SerializeField] private float visionRange = 25f;
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private float tooCloseRange = 5f;

    [Header("Combat Settings")]
    [SerializeField] private float fireRate = 2.0f;
    [SerializeField] private float nextShotTime = 0;
    [SerializeField] private float bulletSpeed = 15f;
    [SerializeField] private float bullet_Min_Damage = 1f;
    [SerializeField] private float bullet_Max_Damage = 5f;

    [Header("Material")]
    [SerializeField] private Material PatrolMaterial;
    [SerializeField] private Material ChaseMaterial;
    [SerializeField] private Material AttackMaterial;
    [SerializeField] private Material RetreatMaterial;
    [SerializeField] private Material SearchMaterial;

    [Header("Search Settings")]
    [SerializeField] private float searchDuration = 4f;
    private float searchTimer = 0;

    // Internal Logic variables
    private SingleGunnerState currentState = SingleGunnerState.Patrol;

    // Pathfinding Variables
    private Vector3[] path;
    private int targetIndex;
    private bool isMoving = false;
    private bool isWaitingForPath = false;

    void Awake()
    {
        GlobalSniperCount++;
        myID = GlobalSniperCount;
    }

    void Start()
    {
        // --- AUTO ASSIGNMENT FIX ---
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else Debug.LogError("SingleGunner could not find object with tag 'Player'!");
        }

        if (pathfindingManager == null)
        {
            pathfindingManager = FindObjectOfType<Pathfinding>();
            if (pathfindingManager == null) Debug.LogError("SingleGunner could not find a 'Pathfinding' script in the scene!");
        }
        // ---------------------------

        currentHealth = maxHealth;
        currentState = SingleGunnerState.Patrol;
        UpdateUI();
    }

    void Update()
    {
        if (player == null) return; 

        SwitchState();
        UpdateUI();
    }

    private void SwitchState()
    {
        switch (currentState)
        {
            case SingleGunnerState.Patrol:
                Patrol();
                break;
            case SingleGunnerState.Chase:
                Chase();
                break;
            case SingleGunnerState.Attack:
                Attack();
                break;
            case SingleGunnerState.Reposition:
                Reposition();
                break;
            case SingleGunnerState.Search:
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
        State.color = (PatrolMaterial != null) ? PatrolMaterial.color : Color.blue;

        if (distToPlayer < visionRange)
        {
            
            if (distToPlayer <= attackRange)
            {
                currentState = SingleGunnerState.Attack;
            }
            else
            {
                currentState = SingleGunnerState.Chase; 
            }
            return;
        }

        if (Time.time > repathTimer && !isWaitingForPath)
        {
            RequestRandomPath();
        }
    }

  
    private void Chase()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (meshRenderer) meshRenderer.material = ChaseMaterial;
        State.color = (ChaseMaterial != null) ? ChaseMaterial.color : Color.blue;

        if (distToPlayer <= attackRange)
        {
            StopMoving(); 
            currentState = SingleGunnerState.Attack;
            return;
        }
        if (distToPlayer > visionRange)
        {
            searchTimer = Time.time + searchDuration;
            currentState = SingleGunnerState.Search;
            return;
        }
        if (Time.time > repathTimer && !isWaitingForPath)
        {
            repathTimer = Time.time + repathRate;
            RequestPathToPlayer();
        }
    }

    private void Attack()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (meshRenderer) meshRenderer.material = AttackMaterial;
        State.color = (AttackMaterial != null) ? AttackMaterial.color : Color.blue;

        if (isMoving) StopMoving();
        AimAndShoot();

        if (distToPlayer > attackRange && distToPlayer < visionRange)
        {
            currentState = SingleGunnerState.Chase; 
            return;
        }

        if (distToPlayer > visionRange)
        {
            searchTimer = Time.time + searchDuration;
            currentState = SingleGunnerState.Search;
            return;
        }
    }



    private void Reposition()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (meshRenderer) meshRenderer.material = RetreatMaterial;
        State.color = (RetreatMaterial != null) ? RetreatMaterial.color : Color.blue;


        if (Time.time > repathTimer && !isWaitingForPath)
        {
            repathTimer = Time.time + repathRate;
            RequestRetreatPath();
        }

        // Exit Condition: Safety reached
        if (distToPlayer > tooCloseRange * 2.0f)
        {
            StopMoving();
            currentState = SingleGunnerState.Attack;
        }
    }

    private void Search()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (meshRenderer) meshRenderer.material = SearchMaterial;
        State.color = (SearchMaterial != null) ? SearchMaterial.color : Color.blue;


        if (isMoving) StopMoving();
        transform.Rotate(Vector3.up * turnSpeed * 1f * Time.deltaTime);

        if (distToPlayer < visionRange)
        {
            if (distToPlayer <= attackRange) currentState = SingleGunnerState.Attack;
            else currentState = SingleGunnerState.Chase;
            return;
        }

        if (Time.time > searchTimer)
        {
            currentState = SingleGunnerState.Patrol;
        }
    }

  

    private void AimAndShoot()
    {
        if (player == null) return;

        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }

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


    void RequestRandomPath()
    {
        if (pathfindingManager == null) return;
        isWaitingForPath = true;
        Vector3 randomSpot = transform.position + new Vector3(Random.Range(-patrolRadius, patrolRadius), 0, Random.Range(-patrolRadius, patrolRadius));
        pathfindingManager.FindPath(transform.position, randomSpot, this);
    }

 
    void RequestPathToPlayer()
    {
        if (pathfindingManager == null) return;
        isWaitingForPath = true;
        pathfindingManager.FindPath(transform.position, player.position, this);
    }

    void RequestRetreatPath()
    {
        if (pathfindingManager == null) return;
        isWaitingForPath = true;
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

    public void OnPathFound(Vector3[] newPath, bool pathSuccessful)
    {
        isWaitingForPath = false;

       
        if (currentState == SingleGunnerState.Attack) return;

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
                    currentWaypoint.y = transform.position.y;
                }

                transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, moveSpeed * Time.deltaTime);

                Vector3 dir = currentWaypoint - transform.position;
                if (dir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * turnSpeed);
                }

                yield return null;
            }
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

     
        if (currentState != SingleGunnerState.Reposition)
        {
            currentState = SingleGunnerState.Reposition;
            StopMoving();
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

    private void OnDrawGizmos()
    {
        if (path != null)
        {
            Gizmos.color = Color.black;
            for (int i = targetIndex; i < path.Length; i++)
            {
                Gizmos.DrawCube(path[i], Vector3.one * 0.5f);
                if (i == targetIndex) Gizmos.DrawLine(transform.position, path[i]);
                else Gizmos.DrawLine(path[i - 1], path[i]);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, tooCloseRange);
    }
}