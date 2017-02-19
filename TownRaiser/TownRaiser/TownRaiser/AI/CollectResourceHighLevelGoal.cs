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
using FlatRedBall.Math;

namespace TownRaiser.AI
{
    class ResourceCollectHighLevelGoal : HighLevelGoal
    {
        public Unit Owner { get; set; }
        public TileNodeNetwork NodeNetwork { get; set; }
        Vector3 _ClickPosition;
        public Vector3 ClickPosition
        {
            get
            {
                return _ClickPosition;
            }
            set
            {
                if (value != _ClickPosition)
                {
                    _ClickPosition = value;
                    SingleTileCenter = GetSingleTileCenterFromClickPosition(_ClickPosition);
                    SingleTile = GetSingleTile(SingleTileCenter);
                }
            }
        }
        /// <summary>
        /// The center of the desired resource tile, as if it weren't merged with any neighbors.
        /// </summary>
        Vector3 SingleTileCenter { get; set; }
        /// <summary>
        /// The AxisAlignedRectangle that represents what would be our desired resource tile, as if it weren't merged with any neighbors.
        /// </summary>
        AxisAlignedRectangle SingleTile { get; set; }
        public AxisAlignedRectangle TargetResourceTile { get; set; }
        public string TargetResourceType { get; set; }

        /// <summary>
        /// Find the center of the tile clicked on, rather than finding the node nearest the click (which could be opposite side of closest node).
        /// </summary>
        /// <param name="clickPosition">Center of resource clicked on, as if it weren't part of a merged group.</param>
        /// <returns></returns>
        private Vector3 GetSingleTileCenterFromClickPosition(Vector3 clickPosition)
        {
            const float tilesWide = 1;
            return new Vector3(
                MathFunctions.RoundFloat(ClickPosition.X, GameScreen.GridWidth * tilesWide, GameScreen.GridWidth * tilesWide / 2),
                MathFunctions.RoundFloat(ClickPosition.Y, GameScreen.GridWidth * tilesWide, GameScreen.GridWidth * tilesWide / 2),
                0);
        }
        private AxisAlignedRectangle GetSingleTile(Vector3 singleTileCenter)
        {
            float roundedX = MathFunctions.RoundFloat(singleTileCenter.X - GameScreen.GridWidth / 2.0f, GameScreen.GridWidth);
            float roundedY = MathFunctions.RoundFloat(singleTileCenter.Y - GameScreen.GridWidth / 2.0f, GameScreen.GridWidth);

            AxisAlignedRectangle newAar = new AxisAlignedRectangle();
            newAar.Width = GameScreen.GridWidth;
            newAar.Height = GameScreen.GridWidth;
            newAar.Left = roundedX;
            newAar.Bottom = roundedY;
            newAar.Visible = false;

#if DEBUG
            newAar.Visible = DebuggingVariables.ShowResourceCollision;
#endif

            return newAar;
        }

        const float CollectFrequency = 1;

        /// <summary>
        /// The last time resource was collected. Resource is collected one time every X seconds
        /// as defined by the <paramref name="CollectFrequency"/> value;
        /// </summary>
        double lastCollectionTime;

        // TODO: Needs to go away when using a resource collision.
        const float MaxCollectDistanct = 20;
        public bool IsInRangeToCollect()
        {
            // TODO: Use collide check with Owner.ResourceCollectCircle.
            var currentDistance = (Owner.Position - SingleTileCenter).Length();
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

                // Stop moving.
                Owner.ImmediateGoal.Path?.Clear();
                Owner.Velocity = Vector3.Zero;

                var screen = FlatRedBall.Screens.ScreenManager.CurrentScreen as GameScreen;
                bool canCollect = screen.PauseAdjustedSecondsSince(lastCollectionTime) >= CollectFrequency;

                if(canCollect)
                {
                    // TODO: Take resource back to nearest Town Hall?
                    lastCollectionTime = screen.PauseAdjustedCurrentTime;
                    if (TargetResourceType == "Wood")
                    {
                        screen.Lumber += Owner.UnitData.ResourceHarvestAmount;
                    }
                    else if (TargetResourceType == "Stone")
                    {
                        screen.Stone += Owner.UnitData.ResourceHarvestAmount;
                    }
                    else if (TargetResourceType == "Gold")
                    {
                        screen.Gold += Owner.UnitData.ResourceHarvestAmount;
                    }
                    screen.UpdateResourceDisplay();
                }
            }
        }

        public override bool GetIfDone()
        {
            // TODO: Take resource back to nearest Town Hall?

            // Resources are unlimited, only restricted by mosh pit of units trying to harvest simultaneously.
            return false;
        }

        private void PathfindToTarget()
        {
            if (Owner.ImmediateGoal == null)
            {
                Owner.ImmediateGoal = new ImmediateGoal();
            }
            // Get path to our clicked [single] tile.
            var pathToTileCenter = Owner.GetPathTo(SingleTileCenter);
            // Add a node for the center to make sure unit tries to get close enough to harvest.
            pathToTileCenter.Add(new PositionedNode() { X = SingleTileCenter.X, Y = SingleTileCenter.Y, Z = SingleTileCenter.Z });
            Owner.ImmediateGoal.Path = pathToTileCenter;
        }

        private void PerformPathfindingDecisions()
        {
            // Don't worry about checking for reaching target since we put it in the middle of a collidable tile.
            // TODO: Get to closest side of tile. Or find node in said position.
            var closestNodeToTarget = NodeNetwork.GetClosestNodeTo(ref SingleTile.Position);

            var lastPoint = Owner.ImmediateGoal.Path.Last();

            var hasTargetMovedFromPath = lastPoint.Position != closestNodeToTarget.Position;

            if (hasTargetMovedFromPath)
            {
                PathfindToTarget();
            }
        }
    }
}