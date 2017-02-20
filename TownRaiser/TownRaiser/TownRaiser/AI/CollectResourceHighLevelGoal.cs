using FlatRedBall.AI.Pathfinding;
using System.Linq;
using TownRaiser.Entities;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using TownRaiser.Screens;
using FlatRedBall.Math;

namespace TownRaiser.AI
{
    class ResourceCollectHighLevelGoal : HighLevelGoal
    {
        WalkToHighLevelGoal walkGoal;
        // TODO: Toggle between going to resource and returning to building with resource.

        const float CollectFrequencyInSeconds = 1;
        /// <summary>
        /// The last time resource was collected. Resource is collected one time every X seconds
        /// as defined by the <paramref name="CollectFrequency"/> value;
        /// </summary>
        double lastCollectionTime;

        public Unit Owner { get; private set; }
        public TileNodeNetwork NodeNetwork { get; private set; }
        public Vector3 ClickPosition { get; private set; }
        /// <summary>
        /// The center of the desired resource tile, as if it weren't merged with any neighbors.
        /// </summary>
        public Vector3 SingleTileCenter { get; private set; }
        /// <summary>
        /// The AxisAlignedRectangle that represents what would be our desired resource tile, as if it weren't merged with any neighbors.
        /// </summary>
        public AxisAlignedRectangle SingleTile { get; private set; }
        public AxisAlignedRectangle TargetResourceTile { get; private set; }
        public string TargetResourceType { get; private set; }

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

        public ResourceCollectHighLevelGoal(Unit owner, TileNodeNetwork nodeNetwork, Vector3 clickPosition, AxisAlignedRectangle targetResourceTile, string targetResourceType)
        {
            Owner = owner;
            NodeNetwork = nodeNetwork;
            ClickPosition = clickPosition;
            TargetResourceTile = targetResourceTile;
            TargetResourceType = targetResourceType;

            // TODO: Handle when we can't get to desired tile (e.g., tree in the middle of forest).
            SingleTileCenter = GetSingleTileCenterFromClickPosition(ClickPosition);
            SingleTile = GetSingleTile(SingleTileCenter);
        }

        public override bool GetIfDone()
        {
            // Resources are unlimited, only restricted by mosh pit of units trying to harvest simultaneously.
            return false;
        }

        public bool IsInRangeToCollect()
        {
            return Owner.ResourceCollectCircleInstance.CollideAgainst(SingleTile);
        }

        public override void DecideWhatToDo()
        {
            if (IsInRangeToCollect())
            {
                // we're close, harvest!

                // Stop moving.
                walkGoal = null;

                var screen = FlatRedBall.Screens.ScreenManager.CurrentScreen as GameScreen;
                bool canCollect = screen.PauseAdjustedSecondsSince(lastCollectionTime) >= CollectFrequencyInSeconds;

                if (canCollect)
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
            else
            {
                bool isWalking = Owner?.ImmediateGoal?.Path?.Count > 0;
                if (!isWalking)
                {
                    if (walkGoal == null)
                    {
                        walkGoal = new WalkToHighLevelGoal();
                        walkGoal.Owner = Owner;
                        walkGoal.TargetPosition = SingleTileCenter;
                        walkGoal.ForceAttemptToGetToExactTarget = true;
                        walkGoal.DecideWhatToDo();
                    }
                    else
                    {
                        // Wasn't in range to collect, isn't walking, and goal was already set.
                        // TODO: May not be able to reach target right now (collisions?).
                    }
                }
            }
        }
    }
}