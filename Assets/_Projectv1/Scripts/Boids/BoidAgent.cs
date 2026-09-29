using System.Collections.Generic;
using UnityEngine;
using Steering;
using Environment;

namespace Boids
{
    [RequireComponent(typeof(BoidSensor))]
    public class BoidAgent : SteeringAgent
    {
        [Header("Rangos de Percepción.")]
        [Tooltip("Rango para Cohesión y Alineación.")]
        public float neighborRadius = 5f;
        [Tooltip("Rango para Separación.")]
        public float separationRadius = 2f;
        public float visionRadius = 8f;

        [Header("Pesos de Comportamiento.")]
        public float separationWeight = 2.5f;
        public float alignmentWeight = 1.0f;
        public float cohesionWeight = 1.0f;
        public float evadeWeight = 4.0f;
        public float arriveWeight = 1.5f;
         
        [Header("Límites del Escenario.")]
        public float mapLimitX = 18f;
        public float mapLimitZ = 18f;
        public float boundaryWeight = 3.5f;

        [Header("Durabilidad e Interacción.")]
        public float maxDurability = 100f;
        public float currentDurability;
        public float damagePerSecond = 25f;
        public float interactDistance = 1.2f;

        public bool IsDestroyed { get; private set; }

        private BoidSensor sensor;

        protected void Awake()
        {
            sensor = GetComponent<BoidSensor>();
            currentDurability = maxDurability;
        }
        
        protected override void Update()
        {
            
            if (IsDestroyed)
            {
                Velocity = Vector3.zero;
                return;
            }

            CalculateSteeringForces();
            base.Update();
        }
        
        private void CalculateSteeringForces()
        {
            
            Vector3 boundaryForce = CalculateBoundaryForce() * boundaryWeight;
            AddForce(boundaryForce);

        
            Transform hunter = sensor.GetNearestHunter(visionRadius);
            if (hunter != null && hunter.TryGetComponent<SteeringAgent>(out var hunterAgent))
            {
                Vector3 evadeForce = SteeringBehaviors.Evade(this, hunterAgent) * evadeWeight;
                AddForce(evadeForce);
                return;
            }
 
            Transform interestTarget = sensor.GetNearestInterestObject(visionRadius);
            if (interestTarget != null)
            {
                Vector3 arriveForce = SteeringBehaviors.Arrive(this, interestTarget.position, 2f) * arriveWeight;
                AddForce(arriveForce);

                if (Vector3.Distance(transform.position, interestTarget.position) <= interactDistance)
                {
                    if (interestTarget.TryGetComponent<InterestObject>(out var targetObj))
                    {
                        targetObj.TakeDamage(damagePerSecond * Time.deltaTime);
                    }
                }
            }
           
            List<BoidAgent> neighbors = sensor.GetNeighbors(neighborRadius);
            if (neighbors.Count > 0)
            {
                Vector3 sepForce = CalculateSeparation(neighbors) * separationWeight;
                Vector3 alignForce = CalculateAlignment(neighbors) * alignmentWeight;
                Vector3 cohForce = CalculateCohesion(neighbors) * cohesionWeight;

                AddForce(sepForce);
                AddForce(alignForce);
                AddForce(cohForce);
            }
          
            if (Velocity.sqrMagnitude < 0.5f)
            {
                AddForce(transform.forward * maxSpeed);
            }
        }

        private Vector3 CalculateBoundaryForce()
        {
            Vector3 force = Vector3.zero;
            Vector3 pos = transform.position;
           
            if (pos.x > mapLimitX) force.x = -1f;
            else if (pos.x < -mapLimitX) force.x = 1f;

           
            if (pos.z > mapLimitZ) force.z = -1f;
            else if (pos.z < -mapLimitZ) force.z = 1f;

            if (force != Vector3.zero)
            {
                Vector3 desired = force.normalized * maxSpeed;
                return desired - Velocity;
            }

            return Vector3.zero;
        }

        private Vector3 CalculateSeparation(List<BoidAgent> neighbors)
        {
            Vector3 separation = Vector3.zero;
            int count = 0;
            float sqrSeparationRadius = separationRadius * separationRadius;

            foreach (var neighbor in neighbors)
            {
                if (neighbor == null || neighbor == this || neighbor.IsDestroyed) continue;

                Vector3 diff = transform.position - neighbor.transform.position;
                float sqrDist = diff.sqrMagnitude;

                if (sqrDist > 0 && sqrDist < sqrSeparationRadius)
                {
                    if (sqrDist < 0.04f)
                    {
                        diff = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
                    }

                    separation += diff.normalized / Mathf.Max(sqrDist, 0.05f);
                    count++;
                }
            }

            if (count > 0) separation /= count;
            return separation;
        }

        private Vector3 CalculateAlignment(List<BoidAgent> neighbors)
        {
            Vector3 averageVelocity = Vector3.zero;
            int validNeighbors = 0;

            foreach (var neighbor in neighbors)
            {
                if (neighbor == null || neighbor.IsDestroyed) continue;
                averageVelocity += neighbor.Velocity;
                validNeighbors++;
            }

            if (validNeighbors == 0) return Vector3.zero;

            averageVelocity /= validNeighbors;
            return averageVelocity - Velocity;
        }

        private Vector3 CalculateCohesion(List<BoidAgent> neighbors)
        {
            Vector3 centerOfMass = Vector3.zero;
            int validNeighbors = 0;

            foreach (var neighbor in neighbors)
            {
                if (neighbor == null || neighbor.IsDestroyed) continue;
                centerOfMass += neighbor.transform.position;
                validNeighbors++;
            }

            if (validNeighbors == 0) return Vector3.zero;

            centerOfMass /= validNeighbors;
            return SteeringBehaviors.Seek(this, centerOfMass);
        }

        public void TakeDamage(float amount)
        {
            if (IsDestroyed) return;

            currentDurability -= amount;
            if (currentDurability <= 0)
            {
                currentDurability = 0;
                IsDestroyed = true;
            }
        }

        public void ResetAgent()
        {
            currentDurability = maxDurability;
            IsDestroyed = false;
            gameObject.SetActive(true);
        }
    }
}