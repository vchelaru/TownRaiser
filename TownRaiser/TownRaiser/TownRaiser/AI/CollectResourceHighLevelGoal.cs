﻿using FlatRedBall.AI.Pathfinding;
using System.Linq;
using TownRaiser.Entities;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using TownRaiser.Screens;
using FlatRedBall.Math;
using TownRaiser.DataTypes;

namespace TownRaiser.AI
{
    class ResourceCollectHighLevelGoal : HighLevelGoal
    {
        WalkToHighLevelGoal walkGoal;
        
        bool hasResourceToReturn;

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
        public PositionedObjectList<Building> AllBuildings { get; set; }
        public Building ResourceReturnBuilding { get; set; }
        public AxisAlignedRectangle ResourceReturnBuildingTile { get; set; }

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

        public ResourceCollectHighLevelGoal(Unit owner, TileNodeNetwork nodeNetwork, Vector3 clickPosition, AxisAlignedRectangle targetResourceTile, string targetResourceType, PositionedObjectList<Building> allBuildings)
        {
            Owner = owner;
            NodeNetwork = nodeNetwork;
            ClickPosition = clickPosition;
            TargetResourceTile = targetResourceTile;
            TargetResourceType = targetResourceType;
            AllBuildings = allBuildings;

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
            return !hasResourceToReturn && Owner.ResourceCollectCircleInstance.CollideAgainst(SingleTile);
        }
        public bool IsInRangeToReturnResource()
        {
            return hasResourceToReturn
                && ResourceReturnBuilding != null
                && Owner.ResourceCollectCircleInstance.CollideAgainst(ResourceReturnBuildingTile);
        }

        const float CollectFrequencyInSeconds = 1;
        /// <summary>
        /// The last time resource was collected. Resource is collected one time every X seconds
        /// as defined by the <paramref name="CollectFrequency"/> value;
        /// </summary>
        double arrivedAtResourceTime;

        public override void DecideWhatToDo()
        {
            if (IsInRangeToReturnResource())
            {
                // We're close enough to our target resource: harvest!

                // Stop moving
                walkGoal = null;

                var screen = FlatRedBall.Screens.ScreenManager.CurrentScreen as GameScreen;

                // Increment appropriate resource.
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

                // Update UI
                screen.UpdateResourceDisplay();

                hasResourceToReturn = false;
                ResourceReturnBuilding = null;
                // Default to !isWalking later to set up return-to-resource trip.
            }
            else if (IsInRangeToCollect())
            {
                // We're close enough to our target resource: harvest!

                // Stop moving.
                walkGoal = null;

                var screen = FlatRedBall.Screens.ScreenManager.CurrentScreen as GameScreen;

                // Delay resource pick-up once arrived.
                if (arrivedAtResourceTime == 0)
                {
                    arrivedAtResourceTime = screen.PauseAdjustedCurrentTime;
                }
                bool canCollect = screen.PauseAdjustedSecondsSince(arrivedAtResourceTime) >= CollectFrequencyInSeconds;

                if (canCollect)
                {
                    hasResourceToReturn = true;
                    arrivedAtResourceTime = 0;
                    // Set up to return resource
                    // Find "closest" building by position comparison.
                    // FUTURE: Get building with shorted node path (in case closest is a long winding path).
                    ResourceReturnBuilding = AllBuildings
                        .Where(building => building.BuildingData.Name == BuildingData.TownHall)
                        .OrderBy(building => (building.Position - Owner.Position).Length())
                        .FirstOrDefault();
                    ResourceReturnBuildingTile = GetSingleTile(ResourceReturnBuilding.Position);

                    if (ResourceReturnBuilding != null)
                    {
                        walkGoal = new WalkToHighLevelGoal();
                        walkGoal.Owner = Owner;
                        walkGoal.TargetPosition = ResourceReturnBuilding.Position;
                        walkGoal.ForceAttemptToGetToExactTarget = true;
                        walkGoal.DecideWhatToDo();
                    }
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
                }
            }
        }
    }
}