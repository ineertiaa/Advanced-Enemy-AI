using UnityEngine;
using UnityEngine.AI;

using Photon.Pun;
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(PhotonTransformView))]
public class InertiasAI : MonoBehaviourPun
{
    [Header("Made by inertia! <3")]

    [Header("Wandering")]
    [Tooltip("The max distance the monster picks a new point from.")] public float WanderRadius = 10;
    [Tooltip("The time the monster waits before picking a new point")] public float WaitTime = 0.5f;
    private float waitTimer;

    [Header("Vision")]
    public float SightRange = 15;
    public float ViewAngle = 90f;
    public Transform EyePos;
    public float ChaseSpeedMultiplier = 1.5f;
    public float SpottedTime;
    private float spottedTimer;
    [Tooltip("The time the monster still chases the player even after losing sight.")] public float LostSightTime = 1.5f;


    public enum States
    {
        Patrolling,
        Spotted,
        Chase,
        LostSight
    }

    [SerializeField] States currentState = States.Patrolling;
    
    public string PlayerTag;

    private Transform target; 

    private NavMeshAgent _agent;
    private float normalSpeed; 

    private float lostSightTimer;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        normalSpeed = _agent.speed;

        if (!PhotonNetwork.IsMasterClient)
        {
            _agent.enabled = false;
            return;
        }

        SetPoint();
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        else
            _agent.enabled = true;

        CheckSight();

        switch (currentState)
        {
            case States.Patrolling:
                Patrol();
                break;

            case States.Spotted:
                Spotted();
                break;

            case States.Chase:
                Chase();
                break;

            case States.LostSight:
                LostSight();
                break;
        }
    }

    public void CheckSight()
    {
        Collider[] hits = Physics.OverlapSphere(EyePos.position, SightRange);

        float closestDistance = Mathf.Infinity;
        Transform potentialTarget = null;
        bool spotted = false;

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag(PlayerTag))
            {
                Vector3 targetCenter = hit.bounds.center;
                Vector3 dirToPlayer = (targetCenter - EyePos.position).normalized;

                float angleToPlayer = Vector3.Angle(EyePos.forward, dirToPlayer);

                if (angleToPlayer < ViewAngle / 2f || currentState == States.Chase || currentState == States.Spotted)
                {
                    float distanceToPlayer = Vector3.Distance(EyePos.position, hit.transform.position);

                    Debug.DrawRay(EyePos.position, dirToPlayer * SightRange, Color.red);

                    if (Physics.Raycast(EyePos.position, dirToPlayer, out RaycastHit rayHit, SightRange))
                    {
                        if (rayHit.collider.CompareTag(PlayerTag))
                        {
                            if (distanceToPlayer < closestDistance)
                            {
                                closestDistance = distanceToPlayer;
                                potentialTarget = hit.transform;
                                spotted = true;
                            }
                        }
                    }
                }
            }
        }

        if (spotted)
        {
            target = potentialTarget;

            if (currentState == States.Patrolling)
            {
                currentState = States.Spotted;
                spottedTimer = 0f;
            }
        }
        else
        {
            if (currentState == States.Chase || currentState == States.Spotted)
            {
                currentState = States.LostSight;
                lostSightTimer = 0;

                SetPoint();
            }
        }
    }

    public void Patrol()
    {
        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;

            if (waitTimer >= WaitTime)
            {
                SetPoint();
                waitTimer = 0;
            }
        }
    }

    public void SetPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * WanderRadius;

        randomDirection += transform.position;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomDirection, out hit, WanderRadius, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
    }

    public void Spotted()
    {
        _agent.SetDestination(target.position);

        spottedTimer += Time.deltaTime;
        if (spottedTimer >= SpottedTime)
        {
            currentState = States.Chase;
        }
    }

    public void Chase()
    {
        _agent.speed = normalSpeed * ChaseSpeedMultiplier;
        _agent.SetDestination(target.position);
    }

    public void LostSight()
    {
        _agent.SetDestination(target.position);
        lostSightTimer += Time.deltaTime;

        if (lostSightTimer >= LostSightTime)
        {
            currentState = States.Patrolling;
            _agent.speed = normalSpeed;
            SetPoint();
        }
    }
}