using UnityEngine;
using FSM;
using Steering;
using Boids;
using Hunter.States;
using Environment;

namespace Hunter
{
    public class HunterNPC : SteeringAgent
    {
        [Header("Requerimientos de Consigna.")]
        [Tooltip("Tiempo mínimo entre ataques.")]
        public float TBA = 3f;
        [Tooltip("Distancia máxima para ataques a distancia.")]
        public float RangeAttackRadius = 8f;
        [Tooltip("Distancia máxima para ataques cuerpo a cuerpo.")]
        public float MeleeAttackRadius = 2f;

        [Header("Detección y Configuración.")]
        public float visionRadius = 10f;
        public LayerMask boidLayer;
        public WaypointPath path;
        public ObjectSpawner spawner;

        [Header("Muestra Visual Adicional.")]
        [Tooltip("Cambiar su color según el estado.")]
        [SerializeField] private Renderer hunterRenderer;
     

        public FiniteStateMachine FSM { get; private set; }
        public HunterPatrolState PatrolState { get; private set; }
        public HunterAttackState AttackState { get; private set; }
        public HunterGatherState GatherState { get; private set; }

        public float TBATimer { get; set; }
        public BoidAgent CurrentTarget { get; set; }
        public BoidAgent DestroyedTarget { get; set; }

        private readonly Collider[] hitColliders = new Collider[32];

        protected void Awake()
        {
            FSM = new FiniteStateMachine();

            
            PatrolState = new HunterPatrolState(this);
            AttackState = new HunterAttackState(this);
            GatherState = new HunterGatherState(this);

            if (hunterRenderer == null)
                hunterRenderer = GetComponentInChildren<Renderer>();

        }

        private void Start()
        {
            TBATimer = 0f; 
            FSM.ChangeState(PatrolState);
        }

        protected override void Update()
        {
           
            if (TBATimer > 0)
            {
                TBATimer -= Time.deltaTime;
            }

            FSM.Update();
            base.Update();
        }
      
        public BoidAgent FindValidBoidInVision()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, visionRadius, hitColliders, boidLayer);
            for (int i = 0; i < count; i++)
            {
                if (hitColliders[i] != null && hitColliders[i].TryGetComponent<BoidAgent>(out var boid) && !boid.IsDestroyed)
                {
                    return boid;
                }
            }
            return null;
        }
     
        public BoidAgent FindDestroyedBoidInVision()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, visionRadius, hitColliders, boidLayer);
            for (int i = 0; i < count; i++)
            {
                if (hitColliders[i] != null && hitColliders[i].TryGetComponent<BoidAgent>(out var boid) && boid.IsDestroyed)
                {
                    return boid;
                }
            }
            return null;
        }

        public void SetStateVisualFeedback(Color stateColor)
        {
            if (hunterRenderer != null)
            {
                hunterRenderer.material.color = stateColor;
            }
           
        }

        public void Stop()
        {
            Velocity = Vector3.zero;
        }

    }
}