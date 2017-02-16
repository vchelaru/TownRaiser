using FlatRedBall.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.Entities;

namespace TownRaiser.AI
{
    public class FindTargetToAttackHighLevelGoal : HighLevelGoal
    {
        public Unit Owner { get; set; }

        public PositionedObjectList<Unit> AllUnits { get; set; }

        public override void DecideWhatToDo()
        {
            const float aggroRadius = 80;
            const float aggroSquared = aggroRadius * aggroRadius;
            bool isTargetAnEnemyUnit = !Owner.UnitData.IsEnemy;

            var found = AllUnits.FirstOrDefault(item => 
                (item.Position - Owner.Position).LengthSquared() < aggroSquared &&
                item.UnitData.IsEnemy == isTargetAnEnemyUnit
                
                );

            if(found != null)
            {
                Owner.CreateAttackGoal(found);
            }
        }

        public override bool GetIfDone()
        {
            return false;
        }
    }
}
