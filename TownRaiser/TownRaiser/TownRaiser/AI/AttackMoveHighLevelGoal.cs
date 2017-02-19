using FlatRedBall.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.Entities;

namespace TownRaiser.AI
{
    class AttackMoveHighLevelGoal : HighLevelGoal
    {
        AttackUnitHighLevelGoal attackGoal;
        WalkToHighLevelGoal walkGoal;
        FindTargetToAttackHighLevelGoal findTargetGoal;

        Unit owner;
        public Unit Owner
        {
            get
            {
                return owner;
            }
            set
            {
                owner = value;
                findTargetGoal.Owner = owner;
            }
        }

        PositionedObjectList<Unit> allUnits;
        public PositionedObjectList<Unit> AllUnits
        {
            get { return allUnits; }
            set
            {
                allUnits = value;
                findTargetGoal.AllUnits = allUnits;
            }
        }

        PositionedObjectList<Building> allBuildings;
        public PositionedObjectList<Building> AllBuildings
        {
            get { return allBuildings; }
            set
            {
                allBuildings = value;
                findTargetGoal.AllBuildings = allBuildings;
            }
        }
        

        public AttackMoveHighLevelGoal()
        {
            CreateFindTargetGoal();
        }

        private void CreateFindTargetGoal()
        {
            findTargetGoal = new AI.FindTargetToAttackHighLevelGoal();

            
        }

        public override bool GetIfDone()
        {
            return false;
        }

        public override void DecideWhatToDo()
        {
            findTargetGoal.DecideWhatToDo();
        }
    }
}
