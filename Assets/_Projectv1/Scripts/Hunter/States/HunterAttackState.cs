using UnityEngine;
using FSM;
using Steering;

namespace Hunter.States
{
    public class HunterAttackState : IState
    {
        private readonly HunterNPC hunter;

        public HunterAttackState(HunterNPC hunter)
        {
            this.hunter = hunter;
        }

        public void Enter() { }

        public void Update()
        {
            var target = hunter.CurrentTarget;

           
            if (target == null || target.IsDestroyed)
            {
                hunter.CurrentTarget = null;
                hunter.FSM.ChangeState(hunter.PatrolState);
                return;
            }

            float sqrDistance = (hunter.transform.position - target.transform.position).sqrMagnitude;
            float rangeRadiusSqr = hunter.RangeAttackRadius * hunter.RangeAttackRadius;

            
            if (sqrDistance <= rangeRadiusSqr)
            {
              
                ExeAttack(target, 100f);
            }
            else
            {
               
                hunter.AddForce(SteeringBehaviors.Seek(hunter, target.transform.position));
            }
        }
         
        private void ExeAttack(Boids.BoidAgent target, float damage)
        {
           
            target.TakeDamage(damage);

          
            hunter.TBATimer = hunter.TBA;

            if (target.IsDestroyed)
            {
                hunter.DestroyedTarget = target;
                hunter.CurrentTarget = null;
                hunter.FSM.ChangeState(hunter.GatherState);
            }
            else
            {
                hunter.CurrentTarget = null;
                hunter.FSM.ChangeState(hunter.PatrolState);
            }
        }

        public void Exit() { }
    }
}