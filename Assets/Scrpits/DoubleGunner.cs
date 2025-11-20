using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.AI;

public class DoubleGunner : MonoBehaviour,IDamageable
{

    public enum GunnerState
    {
        Patrol,
        Chase,
        Attack,
        Retreat
    }

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



    [Header("Detection Settings")]
    [SerializeField] private float visionRange = 20f;
    [SerializeField] private float engagementRange = 15f;
    [SerializeField] private float AttackRange = 10f;

    [Header("Material")]
    [SerializeField] private Material PatrolMaterial;
    [SerializeField] private Material ChaseMaterial;
    [SerializeField] private Material AttackMaterial;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    GunnerState currentState = GunnerState.Patrol;


    void Start()
    {
        currentHealth = maxHealth;
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.SetDestination(PatrolPoints[nextPatrolPoint]);
    }

    void Update()
    {
        SwitchState();
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
                //Retreat();
                break;
            default:
                Patrol();
                break;
        }
    }




    private void Attack()
    {
        navAgent.ResetPath();
        meshRenderer.material = AttackMaterial;
        transform.LookAt(playerTransform);

        //shooting login
        if (Time.time > nextShootTime)
        {
            nextShootTime = Time.time + FireRate;
            GameObject bullet_01 = Instantiate(Bullet, Gun_01.position, Gun_01.rotation);
            Rigidbody rb_01 = bullet_01.GetComponent<Rigidbody>();
            rb_01.velocity = transform.forward * bullet_01.GetComponent<Bullet>().Speed;

        }

        //Transition back to chase
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > AttackRange)
        {
            currentState = GunnerState.Chase;

        }
        if (currentHealth < maxHealth * 0.3f)
        {
            currentState = GunnerState.Patrol;
        }

    }

    private void Chase()
    {
        meshRenderer.material = ChaseMaterial;
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
            nextPatrolPoint = (nextPatrolPoint + 1) % PatrolPoints.Length;
            navAgent.SetDestination((PatrolPoints[nextPatrolPoint]));
        }
    }




    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
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

}
