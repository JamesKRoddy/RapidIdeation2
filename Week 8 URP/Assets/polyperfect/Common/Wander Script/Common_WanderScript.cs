using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PolyPerfect
{
    [RequireComponent(typeof(Animator)), RequireComponent(typeof(CharacterController))]
    public class Common_WanderScript : MonoBehaviour
    {
        private const float contingencyDistance = 1f;

        [SerializeField]
        public IdleState[] idleStates;
        [SerializeField]
        private MovementState[] movementStates;
        [SerializeField]
        private AIState[] attackingStates;
        [SerializeField]
        private AIState[] deathStates;

        [SerializeField]
        public string species = "NA";

        [SerializeField, Tooltip("This specific animal stats asset, create a new one from the asset menu under (LowPolyAnimals/NewAnimalStats)")]
        public AIStats stats;

        [SerializeField, Tooltip("How far away from it's origin this animal will wander by itself.")]
        private float wanderZone = 10f;
        public float MaxDistance
        {
            get
            {
                return wanderZone;
            }
            set
            {
#if UNITY_EDITOR
                SceneView.RepaintAll();
#endif
                wanderZone = value;
            }
        }

        // [SerializeField, Tooltip("How dominent this animal is in the food chain, agressive animals will attack less dominant animals.")]
        private int dominance = 1;
        private int originalDominance = 0;

        [SerializeField, Tooltip("How far this animal can sense a predator.")]
        private float awareness = 30f;

        [SerializeField, Tooltip("How far this animal can sense it's prey.")]
        private float scent = 30f;
        private float originalScent = 0f;

        // [SerializeField, Tooltip("How many seconds this animal can run for before it gets tired.")]
        private float stamina = 10f;

        // [SerializeField, Tooltip("How much this damage this animal does to another animal.")]
        private float power = 10f;

        // [SerializeField, Tooltip("How much health this animal has.")]
        private float toughness = 5f;

        // [SerializeField, Tooltip("Chance of this animal attacking another animal."), Range(0f, 100f)]
        private float agression = 0f;
        private float originalAgression = 0f;

        // [SerializeField, Tooltip("How quickly the animal does damage to another animal (every 'attackSpeed' seconds will cause 'power' amount of damage).")]
        private float attackSpeed = 0.5f;

        // [SerializeField, Tooltip("If true, this animal will attack other animals of the same specices.")]
        private bool territorial = false;

        // [SerializeField, Tooltip("Stealthy animals can't be detected by other animals.")]
        private bool stealthy = false;

        [SerializeField, Tooltip("If true, this animal will never leave it's zone, even if it's chasing or running away from another animal.")]
        private bool constainedToWanderZone = false;

        [SerializeField, Tooltip("This animal will be peaceful towards species in this list.")]
        private string[] nonAgressiveTowards;

        private static List<Common_WanderScript> allAnimals = new List<Common_WanderScript>();
        public static List<Common_WanderScript> AllAnimals { get { return allAnimals; } }

        //[Space(), Space(5)]
        [SerializeField, Tooltip("If true, this animal will rotate to match the terrain. Ensure you have set the layer of the terrain as 'Terrain'.")]
        private bool matchSurfaceRotation = false;
        [SerializeField, Tooltip("How fast the animnal rotates to match the surface rotation.")]
        private float surfaceRotationSpeed = 2f;

        //[Space(), Space(5)]
        [SerializeField, Tooltip("If true, AI changes to this animal will be logged in the console.")]
        private bool logChanges = false;
        [SerializeField, Tooltip("If true, gizmos will be drawn in the editor.")]
        private bool showGizmos = false;
        [SerializeField]
        private bool drawWanderRange = true;
        [SerializeField]
        private bool drawScentRange = true;
        [SerializeField]
        private bool drawAwarenessRange = true;

        public UnityEngine.Events.UnityEvent deathEvent;
        public UnityEngine.Events.UnityEvent attackingEvent;
        public UnityEngine.Events.UnityEvent idleEvent;
        public UnityEngine.Events.UnityEvent movementEvent;



        private Color distanceColor = new Color(0f, 0f, 205f);
        private Color awarnessColor = new Color(1f, 0f, 1f, 1f);
        private Color scentColor = new Color(1f, 0f, 0f, 1f);
        private Animator animator;
        private CharacterController characterController;
        private NavMeshAgent navMeshAgent;
        private Vector3 origin;
        private int totalIdleStateWeight;
        private int currentStateIndex = 0;
        private bool dead = false;
        private bool moving = false;
        private bool useNavMesh = false;
        private Vector3 targetLocation = Vector3.zero;
        private float currentTurnSpeed = 0f;
        private bool attacking = false;

        public void OnDrawGizmosSelected()
        {
            if (!showGizmos)
                return;

            if (drawWanderRange)
            {
                // Draw circle of radius wander zone
                Gizmos.color = distanceColor;
                Gizmos.DrawWireSphere(origin == Vector3.zero ? transform.position : origin, wanderZone);

                Vector3 IconWander = new Vector3(transform.position.x, transform.position.y + wanderZone, transform.position.z);
                Gizmos.DrawIcon(IconWander, "ico-wander", true);
            }

            if (drawAwarenessRange)
            {
                //Draw circle radius for Awarness.
                Gizmos.color = awarnessColor;
                Gizmos.DrawWireSphere(transform.position, awareness);


                Vector3 IconAwareness = new Vector3(transform.position.x, transform.position.y + awareness, transform.position.z);
                Gizmos.DrawIcon(IconAwareness, "ico-awareness", true);
            }

            if (drawScentRange)
            {
                //Draw circle radius for Scent.
                Gizmos.color = scentColor;
                Gizmos.DrawWireSphere(transform.position, scent);

                Vector3 IconScent = new Vector3(transform.position.x, transform.position.y + scent, transform.position.z);
                Gizmos.DrawIcon(IconScent, "ico-scent", true);
            }

            if (!Application.isPlaying)
                return;

            // Draw target position.
            if (useNavMesh)
            {
                if (navMeshAgent.remainingDistance > 1f)
                {
                    Gizmos.DrawSphere(navMeshAgent.destination + new Vector3(0f, 0.1f, 0f), 0.2f);
                    Gizmos.DrawLine(transform.position, navMeshAgent.destination);
                }
            }
            else
            {
                if (targetLocation != Vector3.zero)
                {
                    Gizmos.DrawSphere(targetLocation + new Vector3(0f, 0.1f, 0f), 0.2f);
                    Gizmos.DrawLine(transform.position, targetLocation);
                }
            }
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();

            var runtimeController = animator.runtimeAnimatorController;

            if (logChanges)
            {
                if (runtimeController == null)
                {
                    Debug.LogError(string.Format("{0} has no animator controller, make sure you put one in to allow the character to walk. See documentation for more details (1)", gameObject.name));
                    enabled = false;
                    return;
                }

                if (animator.avatar == null)
                {
                    Debug.LogError(string.Format("{0} has no avatar, make sure you put one in to allow the character to animate. See documentation for more details (2)", gameObject.name));
                    enabled = false;
                    return;
                }

                if (animator.hasRootMotion == true)
                {
                    Debug.LogError(string.Format("{0} has root motion applied, consider turning this off as our script will deactivate this on play as we do not use it (3)", gameObject.name));
                    animator.applyRootMotion = false;
                }

                if (idleStates.Length == 0 || movementStates.Length == 0)
                {
                    Debug.LogError(string.Format("{0} has no idle or movement states, make sure you fill these out. See documentation for more details (4)", gameObject.name));
                    enabled = false;
                    return;
                }

                if (idleStates.Length > 0)
                {
                    for (int i = 0; i < idleStates.Length; i++)
                    {
                        if (idleStates[i].animationBool == "")
                        {
                            Debug.LogError(string.Format("{0} has " + idleStates.Length + " Idle states, you need to make sure that each state has an animation boolean. See documentation for more details (4)", gameObject.name));
                            enabled = false;
                            return;
                        }
                    }
                }

                if (movementStates.Length > 0)
                {
                    for (int i = 0; i < movementStates.Length; i++)
                    {
                        if (movementStates[i].animationBool == "")
                        {
                            Debug.LogError(string.Format("{0} has " + movementStates.Length + " Movement states, you need to make sure that each state has an animation boolean to see the character walk. See documentation for more details (4)", gameObject.name));
                            enabled = false;
                            return;
                        }

                        if (movementStates[i].moveSpeed <= 0)
                        {
                            Debug.LogError(string.Format("{0} has a movement state with a speed of 0 or less, you need to set the speed higher than 0 to see the character move. See documentation for more details (4)", gameObject.name));
                            enabled = false;
                            return;
                        }

                        if (movementStates[i].turnSpeed <= 0)
                        {
                            Debug.LogError(string.Format("{0} has a turn speed state with a speed of 0 or less, you need to set the speed higher than 0 to see the character turn. See documentation for more details (4)", gameObject.name));
                            enabled = false;
                            return;
                        }
                    }
                }

                if (attackingStates.Length == 0)
                {
                    Debug.Log(string.Format("{0} has " + attackingStates.Length + " this character will not be able to attack. See documentation for more details (4)", gameObject.name));
                }

                if (attackingStates.Length > 0)
                {
                    for (int i = 0; i < attackingStates.Length; i++)
                    {
                        if (attackingStates[i].animationBool == "")
                        {
                            Debug.LogError(string.Format("{0} has " + attackingStates.Length + " attacking states, you need to make sure that each state has an animation boolean. See documentation for more details (4)", gameObject.name));
                            enabled = false;
                            return;
                        }
                    }
                }

                if (stats == null)
                {
                    Debug.LogError(string.Format("{0} has no AI stats, make sure you assign one to the wander script. See documentation for more details (5)", gameObject.name));
                    enabled = false;
                    return;
                }
            }

            foreach (IdleState state in idleStates)
            {
                totalIdleStateWeight += state.stateWeight;
            }

            origin = transform.position;
            animator.applyRootMotion = false;
            characterController = GetComponent<CharacterController>();
            navMeshAgent = GetComponent<NavMeshAgent>();

            //Assign the stats to variables
            originalDominance = stats.dominance;
            dominance = originalDominance;

            toughness = stats.toughness;
            territorial = stats.territorial;

            stamina = stats.stamina;

            originalAgression = stats.agression;
            agression = originalAgression;
            power = stats.power;
            attackSpeed = stats.attackSpeed;
            stealthy = stats.stealthy;

            originalScent = scent;
            scent = originalScent;

            if (navMeshAgent)
            {
                useNavMesh = true;
                navMeshAgent.stoppingDistance = contingencyDistance;
            }

            if (matchSurfaceRotation && transform.childCount > 0)
            {
                transform.GetChild(0).gameObject.AddComponent<Common_SurfaceRotation>().SetRotationSpeed(surfaceRotationSpeed);
            }

            allAnimals.Add(this);
        }

        private void Start()
        {
            if (Common_WanderManager.Instance != null && Common_WanderManager.Instance.PeaceTime)
            {
                SetPeaceTime(true);
            }

            StartCoroutine(InitYield());
            InvokeRepeating("Hunger", 2.0f, 20.0f);
        }

        private void OnDestroy()
        {
            allAnimals.Remove(this);
        }

        private IEnumerator InitYield()
        {
            yield return new WaitForSeconds((Random.Range(0, 200) / 100));
            DecideNextState(false, true);
        }

        private void DecideNextState(bool wasIdle, bool firstState = false)
        {
            attacking = false;

            if (currentStateIndex < idleStates.Length)
                animator.SetBool(idleStates[currentStateIndex].animationBool, false);
            if (currentStateIndex < movementStates.Length)
                animator.SetBool(movementStates[currentStateIndex].animationBool, false);
            if (currentStateIndex < attackingStates.Length)
                animator.SetBool(attackingStates[currentStateIndex].animationBool, false);

            if (logChanges)
            {
                Debug.Log(string.Format("{0}: Deciding next state.", gameObject.name));
            }

            // Look for a predator.
            if (awareness > 0)
            {
                for (int i = 0; i < allAnimals.Count; i++)
                {
                    if (allAnimals[i].dead == true || allAnimals[i] == this || allAnimals[i].species == species || allAnimals[i].dominance <= dominance || allAnimals[i].stealthy || allAnimals[i].gameObject.activeSelf == false)
                    {
                        continue;
                    }

                    if (Vector3.Distance(transform.position, allAnimals[i].transform.position) > awareness)
                    {
                        continue;
                    }

                    if (useNavMesh)
                    {
                        RunAwayFromAnimal(allAnimals[i]);
                    }
                    else
                    {
                        NonNavMeshRunAwayFromAnimal(allAnimals[i]);
                    }

                    if (logChanges)
                    {
                        Debug.Log(string.Format("{0}: Found predator ({1}), running away.", gameObject.name, allAnimals[i].gameObject.name));
                    }

                    return;
                }
            }


            // Look for pray if dominant and hungry
            if (dominance > 0 && toughness <= stats.toughness/2)
            {
                for (int i = 0; i < allAnimals.Count; i++)
                {
                    if (allAnimals[i].dead == true || allAnimals[i] == this || (allAnimals[i].species == species && !territorial) || allAnimals[i].dominance > dominance || allAnimals[i].stealthy)
                    {
                        continue;
                    }

                    int p = System.Array.IndexOf(nonAgressiveTowards, allAnimals[i].species);
                    if (p > -1)
                    {
                        continue;
                    }

                    if (Vector3.Distance(transform.position, allAnimals[i].transform.position) > scent)
                    {
                        continue;
                    }

                    if (Random.Range(0, 99) > agression)
                    {
                        continue;
                    }

                    if (logChanges)
                    {
                        Debug.Log(string.Format("{0}: Found prey ({1}), chasing.", gameObject.name, allAnimals[i].gameObject.name));
                    }

                    if (allAnimals[i] == null)
                    {
                        continue;
                    }

                    else
                    {
                        ChaseAnimal(allAnimals[i]);
                        return;
                    }
                }
            } else if (toughness <= stats.toughness) //Herbivore looking for food
            {
                if (logChanges)
                {
                    Debug.Log(string.Format("{0}: Herbefore looking for food", gameObject.name));
                }

                List<Plant> plants = PlantManager.Instance.plantsInScene;

                for (int i = 0; i < plants.Count; i++)
                {
                    if(plants[i] == null)
                    {
                        continue;
                    }

                    if (Vector3.Distance(transform.position, plants[i].transform.position) > scent)
                    {
                        continue;
                    }

                    if(plants[i].resource.resourceCount < 1)
                    {
                        continue;
                    }

                    if (logChanges)
                    {
                        Debug.Log(string.Format("{0}: Found plant ({1}), moving towards it.", gameObject.name, plants[i].gameObject.name));
                    }

                    MoveToPlant(plants[i]);

                    break;
                }
            }

            if (wasIdle && movementStates.Length > 0)
            {
                if (logChanges)
                {
                    Debug.Log(string.Format("{0}: Wandering.", gameObject.name));
                }
                BeginWanderState();
                return;
            }
            else if (idleStates.Length > 0)
            {
                if (logChanges)
                {
                    Debug.Log(string.Format("{0}: Idling.", gameObject.name));
                }
                BeginIdleState(firstState);
                return;
            }

            // Backup selection.
            if (idleStates.Length == 0)
            {
                BeginWanderState();
            }
            else if (movementStates.Length == 0)
            {
                BeginIdleState();
            }
        }

        private void BeginIdleState(bool firstState = false)
        {
            if (!firstState)
            {
                int randomValue = Random.Range(0, totalIdleStateWeight);
                for (int i = 0; i < idleStates.Length; i++)
                {
                    if (randomValue < idleStates[i].stateWeight)
                    {
                        currentStateIndex = i;
                        break;
                    }

                    randomValue = randomValue - idleStates[i].stateWeight;
                }
            }

            if (idleStates.Length == 0)
            {
                BeginWanderState();
                return;
            }

            if(currentStateIndex >= idleStates.Length)
            {
                currentStateIndex = 0;
            }

            if (!string.IsNullOrEmpty(idleStates[currentStateIndex].animationBool))
            {
                animator.SetBool(idleStates[currentStateIndex].animationBool, true);
            }

            float stateTime = firstState ?
              (Random.Range(50f, idleStates[currentStateIndex].minStateTime * 100f)) / 100f
              : (Random.Range(idleStates[currentStateIndex].minStateTime * 100f, idleStates[currentStateIndex].maxStateTime * 100f)) / 100f;

            StartCoroutine(IdleState(stateTime));
        }

        private IEnumerator IdleState(float stateTime)
        {
            moving = false;

            yield return new WaitForSeconds(stateTime);

            if(currentStateIndex >= idleStates.Length)
            {
                currentStateIndex = 0;
            }

            if (idleStates[currentStateIndex] != null || idleStates.Length < currentStateIndex)
            {
                if (!string.IsNullOrEmpty(idleStates[currentStateIndex].animationBool))
                {
                    animator.SetBool(idleStates[currentStateIndex].animationBool, false);
                }
            }

            idleEvent.Invoke();

            DecideNextState(true);
        }

        private void BeginWanderState()
        {
            Vector3 target = RandonPointInRange();

            int slowestMovementState = 0;
            for (int i = 0; i < movementStates.Length; i++)
            {
                if (movementStates[i].moveSpeed < movementStates[slowestMovementState].moveSpeed)
                {
                    slowestMovementState = i;
                }
            }
            currentStateIndex = slowestMovementState;

            if (!string.IsNullOrEmpty(movementStates[currentStateIndex].animationBool))
            {
                animator.SetBool(movementStates[currentStateIndex].animationBool, true);
            }

            movementEvent.Invoke();

            if (useNavMesh)
            {
                StartCoroutine(MovementState(target));
            }
            else
            {
                StartCoroutine(NonNavMeshMovementState(target));
            }
        }

        private IEnumerator MovementState(Vector3 target)
        {
            moving = true;

            navMeshAgent.speed = movementStates[currentStateIndex].moveSpeed;
            navMeshAgent.angularSpeed = movementStates[currentStateIndex].turnSpeed;
            navMeshAgent.SetDestination(target);

            float timeMoving = 0f;
            while ((navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance || timeMoving < 0.1f) && timeMoving < movementStates[currentStateIndex].maxStateTime)
            {
                timeMoving += Time.deltaTime;
                yield return null;
            }

            navMeshAgent.SetDestination(transform.position);

            if (!string.IsNullOrEmpty(movementStates[currentStateIndex].animationBool))
            {
                animator.SetBool(movementStates[currentStateIndex].animationBool, false);
            }

            DecideNextState(false);
        }

        private IEnumerator NonNavMeshMovementState(Vector3 target)
        {
            moving = true;
            targetLocation = target;
            currentTurnSpeed = movementStates[currentStateIndex].turnSpeed;

            float walkTime = 0f;
            float timeUntilAbortWalk = Vector3.Distance(transform.position, target) / movementStates[currentStateIndex].moveSpeed;

            while (Vector3.Distance(transform.position, target) > contingencyDistance && walkTime < timeUntilAbortWalk)
            {
                characterController.SimpleMove(transform.TransformDirection(Vector3.forward) * movementStates[currentStateIndex].moveSpeed);

                Vector3 relativePos = target - transform.position;
                Quaternion rotation = Quaternion.LookRotation(relativePos);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * (currentTurnSpeed / 10));
                currentTurnSpeed += Time.deltaTime;

                walkTime += Time.deltaTime;
                yield return null;
            }

            targetLocation = Vector3.zero;

            if (!string.IsNullOrEmpty(movementStates[currentStateIndex].animationBool))
            {
                animator.SetBool(movementStates[currentStateIndex].animationBool, false);
            }

            DecideNextState(false);
        }

        private void RunAwayFromAnimal(Common_WanderScript predator)
        {
            moving = true;

            Quaternion startRotation = transform.rotation;
            transform.rotation = Quaternion.LookRotation(transform.position - predator.transform.position);
            Vector3 areaAwayFromPredator = transform.position + transform.forward * 5f;
            NavMeshHit hit;
            NavMesh.SamplePosition(areaAwayFromPredator, out hit, 5, 1 << NavMesh.GetAreaFromName("Walkable"));
            Vector3 target = hit.position;
            transform.rotation = startRotation;

            if (constainedToWanderZone && Vector3.Distance(target, origin) > wanderZone)
            {
                target = RandonPointInRange();
            }

            int fastestMovementState = 0;
            for (int i = 0; i < movementStates.Length; i++)
            {
                if (movementStates[i].moveSpeed > movementStates[fastestMovementState].moveSpeed)
                {
                    fastestMovementState = i;
                }
            }
            currentStateIndex = fastestMovementState;

            if (!string.IsNullOrEmpty(movementStates[currentStateIndex].animationBool))
            {
                animator.SetBool(movementStates[currentStateIndex].animationBool, true);
            }

            StartCoroutine(RunAwayState(target, predator));
        }

        private IEnumerator RunAwayState(Vector3 target, Common_WanderScript predator)
        {
            navMeshAgent.speed = movementStates[currentStateIndex].moveSpeed;
            navMeshAgent.angularSpeed = movementStates[currentStateIndex].turnSpeed;
            navMeshAgent.SetDestination(target);

            float timeMoving = 0f;
            while ((navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance || timeMoving < 0.1f) && timeMoving < stamina)
            {
                timeMoving += Time.deltaTime;
                yield return null;
            }

            navMeshAgent.SetDestination(transform.position);

            if (!string.IsNullOrEmpty(movementStates[currentStateIndex].animationBool))
            {
                animator.SetBool(movementStates[currentStateIndex].animationBool, false);
            }

            if (timeMoving > stamina || predator.dead || Vector3.Distance(transform.position, predator.transform.position) > awareness)
            {
                BeginIdleState();
            }
            else
            {
                RunAwayFromAnimal(predator);
            }
        }

        private void NonNavMeshRunAwayFromAnimal(Common_WanderScript predator)
        {
            moving = true;

            Quaternion startRotation = transform.rotation;
            transform.rotation = Quaternion.LookRotation(transform.position - predator.transform.position);
            targetLocation = transform.position + transform.forward * 5f;
            transform.rotation = startRotation;

            if (constainedToWanderZone && Vector3.Distance(targetLocation, origin) > wanderZone)
            {
                targetLocation = RandonPointInRange();
            }

            int fastestMovementState = 0;
            for (int i = 0; i < movementStates.Length; i++)
            {
                if (movementStates[i].moveSpeed > movementStates[fastestMovementState].moveSpeed)
                {
                    fastestMovementState = i;
                }
            }
            currentStateIndex = fastestMovementState;

            if (!string.IsNullOrEmpty(movementStates[currentStateIndex].animationBool))
            {
                animator.SetBool(movementStates[currentStateIndex].animationBool, true);
            }

            StartCoroutine(NonNavMeshRunAwayState(targetLocation, predator));
        }

        private IEnumerator NonNavMeshRunAwayState(Vector3 target, Common_WanderScript predator)
        {
            currentTurnSpeed = movementStates[currentStateIndex].turnSpeed;

            float walkTime = 0f;
            float timeUntilAbortWalk = Vector3.Distance(transform.position, target) / movementStates[currentStateIndex].moveSpeed;

            while (Vector3.Distance(transform.position, target) > contingencyDistance && walkTime < timeUntilAbortWalk && stamina > 0)
            {
                characterController.SimpleMove(transform.TransformDirection(Vector3.forward) * movementStates[currentStateIndex].moveSpeed);

                Vector3 relativePos = target - transform.position;
                Quaternion rotation = Quaternion.LookRotation(relativePos);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * (currentTurnSpeed / 10));
                currentTurnSpeed += Time.deltaTime;

                walkTime += Time.deltaTime;
                stamina -= Time.deltaTime;
                yield return null;
            }

            targetLocation = Vector3.zero;

            if (!string.IsNullOrEmpty(movementStates[currentStateIndex].animationBool))
            {
                animator.SetBool(movementStates[currentStateIndex].animationBool, false);
            }

            if (stamina <= 0 || predator.dead || Vector3.Distance(transform.position, predator.transform.position) > awareness)
            {
                BeginIdleState();
            }
            else
            {
                NonNavMeshRunAwayFromAnimal(predator);
            }
        }

        private void ChaseAnimal(Common_WanderScript prey)
        {
            Vector3 target = prey.transform.position;
            prey.BeginChase(this);

            if (movementStates.Length <= 0)
            {
                Debug.Log("Movement states length is 0");
                this.enabled = false;
                return;
            }
            int fastestMovementState = 0;
            for (int i = 0; i < movementStates.Length; i++)
            {
                if (movementStates[i].moveSpeed > movementStates[fastestMovementState].moveSpeed)
                {
                    fastestMovementState = i;
                }
            }
            currentStateIndex = fastestMovementState;

            if (!string.IsNullOrEmpty(movementStates[currentStateIndex].animationBool))
            {
                animator.SetBool(movementStates[currentStateIndex].animationBool, true);
            }

            if (useNavMesh)
            {
                StartCoroutine(ChaseState(prey));
            }
            else
            {
                StartCoroutine(NonNavMeshChaseState(prey));
            }
        }

        private void MoveToPlant(Plant plant)
        {
            Vector3 target = plant.transform.position;

            if (movementStates.Length <= 0)
            {
                Debug.Log("Movement states length is 0");
                this.enabled = false;
                return;
            }
            int fastestMovementState = 0;
            for (int i = 0; i < movementStates.Length; i++)
            {
                if (movementStates[i].moveSpeed > movementStates[fastestMovementState].moveSpeed)
                {
                    fastestMovementState = i;
                }
            }
            currentStateIndex = fastestMovementState;

            if (!string.IsNullOrEmpty(movementStates[currentStateIndex].animationBool))
            {
                animator.SetBool(movementStates[currentStateIndex].animationBool, true);
            }

            if (useNavMesh)
            {
                StartCoroutine(MoveToPlantState(plant));
            }
            //else
            //{
            //    StartCoroutine(MoveToPlantState(plant));
            //}
        }

        private IEnumerator ChaseState(Common_WanderScript prey)
        {
            moving = true;

            navMeshAgent.speed = movementStates[currentStateIndex].moveSpeed;
            navMeshAgent.angularSpeed = movementStates[currentStateIndex].turnSpeed;
            navMeshAgent.SetDestination(prey.transform.position);

            float timeMoving = 0f;
            bool gotAway = false;
            while ((navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance || timeMoving < 0.1f) && timeMoving < stamina)
            {
                navMeshAgent.SetDestination(prey.transform.position);

                timeMoving += Time.deltaTime;

                if (Vector3.Distance(transform.position, prey.transform.position) < 2f)
                {
                    if (logChanges)
                    {
                        Debug.Log(string.Format("{0}: Caught prey ({1})!", gameObject.name, prey.gameObject.name));
                    }

                    if (!string.IsNullOrEmpty(movementStates[currentStateIndex].animationBool))
                    {
                        animator.SetBool(movementStates[currentStateIndex].animationBool, false);
                    }

                    AttackAnimal(prey);
                    yield break;
                }

                if (constainedToWanderZone && Vector3.Distance(transform.position, origin) > wanderZone)
                {
                    gotAway = true;
                    navMeshAgent.SetDestination(transform.position);
                    break;
                }

                yield return null;
            }

            navMeshAgent.SetDestination(transform.position);

            if (!string.IsNullOrEmpty(movementStates[currentStateIndex].animationBool))
            {
                animator.SetBool(movementStates[currentStateIndex].animationBool, false);
            }

            if (timeMoving > stamina || prey.dead || Vector3.Distance(transform.position, prey.transform.position) > scent || gotAway)
            {
                BeginIdleState();
            }
            else
            {
                ChaseAnimal(prey);
            }
        }

        private IEnumerator MoveToPlantState(Plant plant)
        {
            moving = true;

            navMeshAgent.speed = movementStates[currentStateIndex].moveSpeed;
            navMeshAgent.angularSpeed = movementStates[currentStateIndex].turnSpeed;
            navMeshAgent.SetDestination(plant.transform.position);

            float timeMoving = 0f;
            while ((navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance || timeMoving < 0.1f) && timeMoving < stamina)
            {
                navMeshAgent.SetDestination(plant.transform.position);

                timeMoving += Time.deltaTime;

                if (Vector3.Distance(transform.position, plant.transform.position) < 2f)
                {
                    if (logChanges)
                    {
                        Debug.Log(string.Format("{0}: Moved to plant ({1})!", gameObject.name, plant.gameObject.name));
                    }

                    if (!string.IsNullOrEmpty(movementStates[currentStateIndex].animationBool))
                    {
                        animator.SetBool(movementStates[currentStateIndex].animationBool, false);
                    }

                    EatPlant(plant);
                    yield break;
                }

                if (constainedToWanderZone && Vector3.Distance(transform.position, origin) > wanderZone)
                {
                    navMeshAgent.SetDestination(transform.position);
                    break;
                }

                yield return null;
            }

            navMeshAgent.SetDestination(transform.position);

            if (!string.IsNullOrEmpty(movementStates[currentStateIndex].animationBool))
            {
                animator.SetBool(movementStates[currentStateIndex].animationBool, false);
            }

            if (plant.resource.resourceCount < 1 || plant == null)
            {
                BeginIdleState();
            }
            else
            {
                MoveToPlant(plant);
            }
        }

        private IEnumerator NonNavMeshChaseState(Common_WanderScript prey)
        {
            moving = true;
            targetLocation = prey.transform.position;
            currentTurnSpeed = movementStates[currentStateIndex].turnSpeed;

            float walkTime = 0f;
            bool gotAway = false;
            float timeUntilAbortWalk = Vector3.Distance(transform.position, targetLocation) / movementStates[currentStateIndex].moveSpeed;


            while (Vector3.Distance(transform.position, targetLocation) > contingencyDistance && walkTime < timeUntilAbortWalk && stamina > 0)
            {
                characterController.SimpleMove(transform.TransformDirection(Vector3.forward) * movementStates[currentStateIndex].moveSpeed);
                targetLocation = prey.transform.position;

                Vector3 relativePos = targetLocation - transform.position;
                Quaternion rotation = Quaternion.LookRotation(relativePos);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * (currentTurnSpeed / 10));
                currentTurnSpeed += Time.deltaTime;

                walkTime += Time.deltaTime;
                stamina -= Time.deltaTime;

                if (Vector3.Distance(transform.position, prey.transform.position) < 2f)
                {
                    if (logChanges)
                    {
                        Debug.Log(string.Format("{0}: Caught prey ({1})!", gameObject.name, prey.gameObject.name));
                    }

                    if (!string.IsNullOrEmpty(movementStates[currentStateIndex].animationBool))
                    {
                        animator.SetBool(movementStates[currentStateIndex].animationBool, false);
                    }

                    AttackAnimal(prey);
                    yield break;
                }

                if (constainedToWanderZone && Vector3.Distance(transform.position, origin) > wanderZone)
                {
                    gotAway = true;
                    targetLocation = transform.position;
                    break;
                }

                yield return null;
            }

            targetLocation = Vector3.zero;

            if (!string.IsNullOrEmpty(movementStates[currentStateIndex].animationBool))
            {
                animator.SetBool(movementStates[currentStateIndex].animationBool, false);
            }

            if (stamina <= 0 || prey.dead || Vector3.Distance(transform.position, prey.transform.position) > scent || gotAway)
            {
                BeginIdleState();
            }
            else
            {
                ChaseAnimal(prey);
            }
        }

        private void AttackAnimal(Common_WanderScript target)
        {
            attacking = true;

            if (logChanges)
            {
                Debug.Log(string.Format("{0}: Attacking {1}!", gameObject.name, target.gameObject.name));
            }

            if (useNavMesh)
            {
                navMeshAgent.SetDestination(transform.position);
            }
            else
            {
                targetLocation = transform.position;
            }

            currentStateIndex = Random.Range(0, attackingStates.Length);

            if (attackingStates.Length > 0)
            {
                if (!string.IsNullOrEmpty(attackingStates[currentStateIndex].animationBool))
                {
                    animator.SetBool(attackingStates[currentStateIndex].animationBool, true);
                }
            }

            StartCoroutine(MakeAttack(target));
        }

        private void EatPlant(Plant plant)
        {
            attacking = true;

            if (logChanges)
            {
                Debug.Log(string.Format("{0}: Eating {1}!", gameObject.name, plant.gameObject.name));
            }

            if (useNavMesh)
            {
                navMeshAgent.SetDestination(transform.position);
            }
            else
            {
                targetLocation = transform.position;
            }

            currentStateIndex = Random.Range(0, attackingStates.Length);

            if (attackingStates.Length > 0)
            {
                if (!string.IsNullOrEmpty(attackingStates[currentStateIndex].animationBool))
                {
                    animator.SetBool(attackingStates[currentStateIndex].animationBool, true);
                }
            }

            StartCoroutine(MakeEatPlant(plant));
        }

        private IEnumerator MakeAttack(Common_WanderScript target)
        {
            Debug.Log(gameObject.name + "  " + target.gameObject.name);

            target.GetAttacked(this);

            float timer = 0f;
            while (!target.dead)
            {
                timer += Time.deltaTime;

                if (timer > attackSpeed)
                {
                    target.TakeDamage(power, this);
                    timer = 0f;
                }

                yield return null;
            }

            if (attackingStates.Length > 0 && !string.IsNullOrEmpty(attackingStates[currentStateIndex].animationBool))
            {
                animator.SetBool(attackingStates[currentStateIndex].animationBool, false);
            }

            attackingEvent.Invoke();

            StopAllCoroutines();
            DecideNextState(false);
        }

        private IEnumerator MakeEatPlant(Plant target)
        {
            float timer = 0f;
            while (target.resource.resourceCount > 0)// || target.resource.resourceCount > 0 || toughness < stats.toughness)
            {
                timer += Time.deltaTime;

                navMeshAgent.speed = 0;

                if (target.gameObject == null)
                    yield break;

                if (toughness >= stats.toughness)
                    yield break;

                if (timer > attackSpeed)
                {
                    toughness += target.GetEaten();
                    timer = 0f;
                }

                if(toughness > stats.toughness)
                {
                    toughness = stats.toughness;
                }

                yield return null;
            }

            if (attackingStates.Length > 0 && !string.IsNullOrEmpty(attackingStates[currentStateIndex].animationBool))
            {
                animator.SetBool(attackingStates[currentStateIndex].animationBool, false);
            }

            attackingEvent.Invoke();

            StopAllCoroutines();
            DecideNextState(false);
        }

        private void GetAttacked(Common_WanderScript attacker)
        {
            if (attacking)
            {
                return;
            }

            if (logChanges)
            {
                Debug.Log(string.Format("{0}: Getting attacked by {1}!", gameObject.name, attacker.gameObject.name));
            }
            StopAllCoroutines();

            StartCoroutine(TurnToLookAtTarget(attacker.transform));

            if (agression > 0)
            {
                if (!string.IsNullOrEmpty(movementStates[currentStateIndex].animationBool))
                {
                    animator.SetBool(movementStates[currentStateIndex].animationBool, false);
                }

                AttackAnimal(attacker);
            }
            else
            {
                if (moving)
                {
                    if (useNavMesh)
                    {
                        navMeshAgent.SetDestination(transform.position);
                    }
                    else
                    {
                        targetLocation = transform.position;
                    }

                    if (!string.IsNullOrEmpty(movementStates[currentStateIndex].animationBool))
                    {
                        animator.SetBool(movementStates[currentStateIndex].animationBool, false);
                    }

                    moving = false;
                }
                else
                {
                    if (idleStates.Length > 0 && !string.IsNullOrEmpty(idleStates[currentStateIndex].animationBool))
                    {
                        animator.SetBool(idleStates[currentStateIndex].animationBool, false);
                    }
                }
            }
        }

        private void TakeDamage(float damage, Common_WanderScript attacker)
        {
            Debug.Log(gameObject.name + "   " + toughness + "   " + damage);

            toughness -= damage;

            if (toughness <= 0)
            {
                Die(attacker);
            }
        }

        public void Die(Common_WanderScript attacker = null)
        {
            if (logChanges)
            {
                Debug.Log(string.Format("{0}: Died!", gameObject.name));
            }

            if(attacker != null)
                attacker.RegainHealth(stats.foodValue);

            StopAllCoroutines();
            CancelInvoke("Hunger");
            dead = true;

            if (useNavMesh)
            {
                navMeshAgent.SetDestination(transform.position);
            }
            else
            {
                targetLocation = transform.position;
            }

            foreach (AIState state in idleStates)
            {
                if (!string.IsNullOrEmpty(state.animationBool))
                {
                    animator.SetBool(state.animationBool, false);
                }
            }

            foreach (AIState state in movementStates)
            {
                if (!string.IsNullOrEmpty(state.animationBool))
                {
                    animator.SetBool(state.animationBool, false);
                }
            }

            foreach (AIState state in attackingStates)
            {
                if (!string.IsNullOrEmpty(state.animationBool))
                {
                    animator.SetBool(state.animationBool, false);
                }
            }

            if (deathStates.Length > 0)
            {
                currentStateIndex = Random.Range(0, deathStates.Length);

                if (!string.IsNullOrEmpty(deathStates[currentStateIndex].animationBool))
                {
                    animator.SetBool(deathStates[currentStateIndex].animationBool, true);
                }
            }

            else
            {
                var renderer = GetComponentsInChildren<SkinnedMeshRenderer>();

                foreach (var item in renderer)
                {
                    item.enabled = false;
                }
            }

            deathEvent.Invoke();
            this.enabled = false;
        }

        public void Hunger()
        {
            toughness -= 1f;
            if(toughness <= 0)
            {
                Die();
            }
        }

        public void RegainHealth(float regenValue)
        {
            if (logChanges)
            {
                Debug.Log(string.Format("{0}: Regained health! " + regenValue, gameObject.name));
            }

            if (toughness < stats.toughness)
            {
                toughness += regenValue;

                if(toughness > stats.toughness)
                {
                    toughness = stats.toughness;
                }
            }
        }

        public void SetPeaceTime(bool peace)
        {
            if (peace)
            {
                dominance = 0;
                scent = 0f;
                agression = 0f;
            }
            else
            {
                dominance = originalDominance;
                scent = originalScent;
                agression = originalAgression;
            }
        }

        private Vector3 RandonPointInRange()
        {
            Vector3 randomPoint = origin + Random.insideUnitSphere * wanderZone;
            return new Vector3(randomPoint.x, transform.position.y, randomPoint.z);
        }

        private IEnumerator TurnToLookAtTarget(Transform target)
        {
            while (true)
            {
                Vector3 direction = target.position - transform.position;

                if (Vector3.Angle(direction, transform.forward) < 1f)
                {
                    break;
                }

                float step = 2f * Time.deltaTime;
                Vector3 newDirection = Vector3.RotateTowards(transform.forward, direction, step, 0.0f);
                transform.rotation = Quaternion.LookRotation(newDirection);
                yield return null;
            }
        }

        private void BeginChase(Common_WanderScript chasingAnimal)
        {
            if (attacking)
            {
                return;
            }

            StartCoroutine(ChaseCheck(chasingAnimal));
        }

        private IEnumerator ChaseCheck(Common_WanderScript chasingAnimal)
        {
            while (Vector3.Distance(transform.position, chasingAnimal.transform.position) > awareness)
            {
                yield return new WaitForSeconds(0.5f);
            }

            StopAllCoroutines();
            if (moving)
            {
                if (useNavMesh)
                {
                    navMeshAgent.SetDestination(transform.position);
                }
                else
                {
                    targetLocation = transform.position;
                }

                if (!string.IsNullOrEmpty(movementStates[currentStateIndex].animationBool))
                {
                    animator.SetBool(movementStates[currentStateIndex].animationBool, false);
                }

                moving = false;
            }
            else
            {
                if (idleStates.Length - 1 >= currentStateIndex)
                    if (idleStates.Length > 0 && !string.IsNullOrEmpty(idleStates[currentStateIndex].animationBool))
                    {
                        animator.SetBool(idleStates[currentStateIndex].animationBool, false);
                    }
            }

            DecideNextState(false);
        }
    }
}