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
        public PositionedObjectList<Building> AllBuildings { get; set; }

        public override void DecideWhatToDo()
        {
            const float aggroRadius = 80;
            const float aggroSquared = aggroRadius * aggroRadius;
            bool isTargetAnEnemyUnit = !Owner.UnitData.IsEnemy;

            var foundUnit = AllUnits.FirstOrDefault(item => 
                (item.Position - Owner.Position).LengthSquared() < aggroSquared &&
                item.UnitData.IsEnemy == isTargetAnEnemyUnit
                
                );

            if(foundUnit != null)
            {
                Owner.CreateAttackGoal(foundUnit);
            }

            // we prioritize units over buildings, since units can fight back
            // At this time, only bad guys can attack buildings. May need to change
            // this if we decide to add units that are built by the bad guys:
            if(Owner.UnitData.IsEnemy)
            {
#if DEBUG
                if(AllBuildings == null)
                {
                    throw new NullReferenceException($"Need to assing {nameof(AllBuildings)} when instantiating this unit.");
                }
#endif

                const float buildingAggroSquared = (80 + 24) * (80 + 24);
                var foundBuilding = AllBuildings
                    .Where(item => (item.Position - Owner.Position).LengthSquared() < buildingAggroSquared)
                    .OrderBy(item => (item.Position - Owner.Position).LengthSquared())
                    .FirstOrDefault();

                if(foundBuilding != null)
                {
                    Owner.CreateAttackGoal(foundBuilding);
                }
            }


        }

        public override bool GetIfDone()
        {
            return false;
        }
    }
}
