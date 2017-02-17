using FlatRedBall.AI.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.Entities;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using TownRaiser.Screens;

namespace TownRaiser.AI
{
    class CollectResourceHighLevelGoal : HighLevelGoal
    {
        public AxisAlignedRectangle TargetResourceTile { get; set; }
        public Unit Owner { get; set; }
        
        const float CollectFrequency = 1;
        const float MaxCollectDistanct = 20;
        
        // The last time damage was dealt. Damage is dealt one time every X seconds
        // as defined by the DamageFrequency value;
        double lastCollectionTime;

        public bool IsInRangeToCollect()
        {
            // TODO: Do more math based on all rectangle sides/vertices.
            var currentDistance = (Owner.Position - TargetResourceTile.Position).Length();

            return currentDistance < MaxCollectDistanct;
        }

        public override void DecideWhatToDo()
        {
            if(Owner.ImmediateGoal?.Path?.Count > 0)
            {
                PerformPathfindingDecisions();
            }
            else if(IsInRangeToCollect() == false)
            {
                PathfindToTarget();
            }
            else
            {
                // we're close, harvest!
                var screen = FlatRedBall.Screens.ScreenManager.CurrentScreen as GameScreen;
                bool canCollect = screen.PauseAdjustedSecondsSince(lastCollectionTime) >= CollectFrequency;

                if(canCollect)
                {
                    lastCollectionTime = screen.PauseAdjustedCurrentTime;
                    // TODO: Figure out what kind of resource we are harvesting (or different Collect*ResourceHighLevelGoal per resource type).
                    screen.Lumber += Owner.UnitData.ResourceHarvestAmount;
                }
            }
        }

        public override bool GetIfDone()
        {
            // TODO: Look up default we are overriding. May not need override.
            // Resources are unlimited, only restricted by collision of units trying to harvest simultaneously.
            return false;
        }

        private void PathfindToTarget()
        {
            if(Owner.ImmediateGoal == null)
            {
                Owner.ImmediateGoal = new ImmediateGoal();
            }
            // TODO: Get to closest side of tile. Or find node in said position.
            Owner.ImmediateGoal.Path = 
                NodeNetwork.GetPath(ref Owner.Position, ref TargetResourceTile.Position);

        }

        private void PerformPathfindingDecisions()
        {
            bool hasReachedTarget = Owner.CollideAgainst(TargetResourceTile);

            if(hasReachedTarget)
            {
                Owner.ImmediateGoal.Path.Clear();
                Owner.Velocity = Vector3.Zero;
            }
            else
            {
                // TODO: Get to closest side of tile. Or find node in said position.
                var closestNodeToTarget = NodeNetwork.GetClosestNodeTo(ref TargetResourceTile.Position);

                var lastPoint = Owner.ImmediateGoal.Path.Last();

                var hasTargetMovedFromPath = lastPoint.Position != closestNodeToTarget.Position;

                if (hasTargetMovedFromPath)
                {
                    PathfindToTarget();
                }


            }

        }
    }
}
