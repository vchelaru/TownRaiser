using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.Entities;

namespace TownRaiser.AI
{
    public class WalkToHighLevelGoal : HighLevelGoal
    {


        public Vector3? TargetPosition;

        public AxisAlignedRectangle TargetResource { get; set; }

        public Unit Owner { get; set; }
        public Unit TargetUnit { get; set; }
        public Building TargetBuilding { get; set; }


        private void GetPath()
        {
            var vector3 = TargetPosition.Value;
            if(Owner.ImmediateGoal == null)
            {
                Owner.ImmediateGoal = new ImmediateGoal();
            }

            Owner.ImmediateGoal.Path = Owner.NodeNetwork.GetPath(ref Owner.Position, ref vector3);
        }

        public override bool GetIfDone()
        {
            return false;
        }

        public override void DecideWhatToDo()
        {
            var hasPath =
                Owner.ImmediateGoal?.Path?.Count > 0;

            if(!hasPath)
            {
                GetPath();
            }
        }
    }

}
