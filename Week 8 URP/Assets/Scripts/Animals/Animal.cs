using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using PolyPerfect;

public class Animal : MonoBehaviour, IHarvestable
{
    
    [SerializeField] NavMeshAgent agent;
    [SerializeField] protected AIStats stats;
    [SerializeField] protected Animator animator;

    [Tooltip("How far the animal can sense food and predetors")]
    [SerializeField] protected float awareness = 30.0f;
        
    protected float health; //Used to both track damage and hunger of the animal

    [Tooltip("0 Idle, 1 Walk, 3 Run")]
    [SerializeField] List<AIState> movementStates;
    [SerializeField] protected List<AIState> attackStates;
    [SerializeField] AIState deadState;

    [HideInInspector] public bool dead = false;

    [SerializeField] protected bool logChanges;

    protected AIState currentState;

    protected Coroutine currentStateRoutine;

    private bool wasIdle = false;

    public Resource resource;

    // Start is called before the first frame update
    public virtual void Start()
    {
        health = stats.toughness;
        resource.resourceCount = Mathf.RoundToInt(health);
        DecideNextState(false);
        InvokeRepeating("Hunger", 2.0f, 20.0f);
    }

    public void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        if (agent.remainingDistance > 1f)
        {
            Gizmos.DrawSphere(agent.destination + new Vector3(0f, 0.1f, 0f), 0.2f);
            Gizmos.DrawLine(transform.position, agent.destination);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //TODO possibly move this
        animator.SetFloat("Speed", Mathf.Sqrt(agent.velocity.sqrMagnitude));
    }

    public void DecideNextState(bool previousIdle = false)
    {
        if (logChanges)
        {
            Debug.Log(gameObject + " deciding state");
        }

        if(currentStateRoutine != null)
            StopCoroutine(currentStateRoutine);

        //First check for predetors?

        Vector3 runAwayFromAnimal = Vector3.zero;

        if (AwarenessCheck(out runAwayFromAnimal))
        {
            Run(runAwayFromAnimal);
            return;
        }

        if (health < stats.toughness)// - 3f)
        {
            Vector3 target;

            if (FindFood(out target))
            {
                Run(target);
                return;
            }           
        }

        if (previousIdle)
        {
            Wander();
        }
        else
        {
            Idle();
        }
    }

    public virtual bool AwarenessCheck(out Vector3 target)
    {
        target = Vector3.zero;
        return false;
    }

    public void Idle()
    {
        if (logChanges)
        {
            Debug.Log(gameObject + " idle state");
        }

        UpdateState(movementStates[0], transform.position);
        float timeInIdle = Random.Range(4.0f, currentState.maxTimeInState);
        currentStateRoutine = StartCoroutine(IdleState());
    }

    public IEnumerator IdleState()
    {
        yield return new WaitForSeconds(currentState.maxTimeInState);

        DecideNextState(true);
    }

    public void Wander()
    {
        if (logChanges)
        {
            Debug.Log(gameObject + " wander state");
        }

        Vector3 targetPoint = RandonPointInRange();

        UpdateState(movementStates[1], targetPoint);
        currentStateRoutine = StartCoroutine(WanderState(targetPoint));
    }

    public IEnumerator WanderState(Vector3 target)
    {
        float timer = 0f;

        while (!ReachedTarget(target))
        {
            timer += Time.deltaTime;

            if (timer > currentState.maxTimeInState)
            {
                DecideNextState();
                yield break;
            }

            yield return new WaitForSeconds(0.01f);
        }

        DecideNextState();
    }

    public void Run(Vector3 target)
    {
        if (logChanges)
        {
            Debug.Log(gameObject + " run state");
        }

        UpdateState(movementStates[2], target);
        currentStateRoutine = StartCoroutine(RunState(target));
    }

    public IEnumerator RunState(Vector3 target, bool updateTargetPosition = false)
    {
        float timer = 0f;

        while (!ReachedTarget(target))
        {
            timer += Time.deltaTime;

            if(timer > currentState.maxTimeInState) //Cant find food or its too far away, go back to wander/idle
            {
                DecideNextState();
                yield break;
            }

            yield return new WaitForSeconds(0.01f);
        }

        //Found food
        Eat();
    }

    bool ReachedTarget(Vector3 target)
    {
        if(Vector3.Distance(transform.position, target) < agent.stoppingDistance + 1.0f)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void TakeDamage(float value)
    {
        health -= value;
        resource.resourceCount = Mathf.RoundToInt(health);
        if (health <= 0)
        {
            Die();
        }
    }

    public virtual bool FindFood(out Vector3 target)
    {
        Debug.LogError(gameObject.name + " cant find a food source");

        if (currentStateRoutine != null)
            StopCoroutine(currentStateRoutine);

        Wander();

        target = Vector3.zero;

        return false;
    }

    public virtual void Eat()
    {
        Debug.Log(gameObject.name + " has no valid food source stored");
    }

    public void Hunger()
    {
        TakeDamage(1f);
    }

    public void Die()
    {
        if (logChanges)
        {
            Debug.Log(gameObject.name + " is dead");
        }

        UpdateState(deadState, transform.position);
        animator.SetBool(deadState.animationParameterName, true);
        agent.SetDestination(transform.position);
        currentState = deadState;
        dead = true;
        Destroy(gameObject, 5.0f);
    }

    //Utility Functions********************************

    private Vector3 RandonPointInRange()
    {
        Vector3 randomPoint = transform.position + Random.insideUnitSphere * awareness;
        return new Vector3(randomPoint.x, transform.position.y, randomPoint.z);
    }

    protected void UpdateState(AIState state, Vector3 targetPoint)
    {
        currentState = state;
        agent.SetDestination(targetPoint);
        agent.speed = state.agentSpeedValue;
    }

    public Resource Harvest()
    {
        Die();
        return resource;
    }
}

[System.Serializable]
public class AIState
{
    public string animationParameterName;
    [Tooltip("Used for movement states only")]
    public float agentSpeedValue;
    [Tooltip("Used for time based states only")]
    [Range(1.0f, 20.0f)]
    public float maxTimeInState = 10.0f;
}
