using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.Entities;
using FlatRedBall.AI.Pathfinding;

namespace TownRaiser.AI
{
    public class ImmediateGoal
    {
        public Vector3? TargetPosition;

        public Unit TargetUnit { get; set; }
        public Building TargetBuilding { get; set; }

        public AxisAlignedRectangle TargetResource { get; set; }
        public List<PositionedNode> Path { get; set; }
    }
}
