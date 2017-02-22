using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;

using FlatRedBall.Math.Geometry;
using FlatRedBall.Math.Splines;

using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using FlatRedBall.Localization;

using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using FlatRedBall.Math;
using FlatRedBall.TileCollisions;
using TownRaiser.DataTypes;
using TownRaiser.Entities;

namespace TownRaiser.Screens
{
    #region Enums

    public enum ActionMode
    {
        Select,
        Train,
        Build
    }

    #endregion

    public partial class GameScreen
	{
        #region Fields/Properties
        public int Lumber { get; set; } = 1200;
        public int Stone { get; set; } = 1000;
        public int Gold { get; set; } = 1000;
        
        public int CurrentCapacityUsed
        {
            get
            {
                return currentCapacityUsedButUseProperty + currentTrainingCapacity;
            }
            set
            {
                currentCapacityUsedButUseProperty = value;
            }
        }
        public int MaxCapacity { get; set; }

        TileNodeNetwork tileNodeNetwork;

        TileShapeCollection woodResourceShapeCollection;
        TileShapeCollection stoneResourceShapeCollection;
        TileShapeCollection waterResourceShapeCollection;
        TileShapeCollection goldResourceShapeCollection;

        public const float GridWidth = 16;

        List<Entities.Unit> selectedUnits = new List<Entities.Unit>();
        Entities.Building selectedBuilding;

        private float mapXMin;
        private float mapXMax;
        private float mapYMin;
        private float mapYMax;

        private int currentCapacityUsedButUseProperty;
        private int currentTrainingCapacity;

#if DEBUG
        //Debug fields and properties.
        I2DInput cameraControls;
#endif

        #endregion

        #region Initialize Methods

        void CustomInitialize()
        {
            InitializeCamera();


            FlatRedBall.Debugging.Debugger.TextCorner = FlatRedBall.Debugging.Debugger.Corner.TopRight;

            InitializeEvents();

            InitializeNodeNetwork();

            InitializeResourceTileShapeCollections();

            InitializeUi();
        }

        private void InitializeResourceTileShapeCollections()
        {
            woodResourceShapeCollection = new TileShapeCollection();
            woodResourceShapeCollection.AddMergedCollisionFrom(WorldMap,
            (list) => list.Any(item => item.Name == "ResourceType" && item.Value as string == "Wood"));
#if DEBUG
            woodResourceShapeCollection.Visible = Entities.DebuggingVariables.ShowResourceCollision;
#endif

            stoneResourceShapeCollection = new TileShapeCollection();
            stoneResourceShapeCollection.AddMergedCollisionFrom(WorldMap,
              (list) => list.Any(item => item.Name == "ResourceType" && item.Value as string == "Stone"));
#if DEBUG
            stoneResourceShapeCollection.Visible = Entities.DebuggingVariables.ShowResourceCollision;
#endif

            waterResourceShapeCollection = new TileShapeCollection();
            waterResourceShapeCollection.AddMergedCollisionFrom(WorldMap,
              (list) => list.Any(item => item.Name == "ResourceType" && item.Value as string == "Water"));
#if DEBUG
            waterResourceShapeCollection.Visible = Entities.DebuggingVariables.ShowResourceCollision;
#endif

            goldResourceShapeCollection = new TileShapeCollection();
            goldResourceShapeCollection.AddMergedCollisionFrom(WorldMap,
              (list) => list.Any(item => item.Name == "ResourceType" && item.Value as string == "Gold"));
#if DEBUG
            goldResourceShapeCollection.Visible = Entities.DebuggingVariables.ShowResourceCollision;
#endif


            
        }

        private void InitializeCamera()
        {
            //Eventually place the map at the main base spawn point.
            Camera.Main.X = Camera.Main.RelativeXEdgeAt(0) + .2f;
            Camera.Main.Y = -Camera.Main.RelativeYEdgeAt(0) + .2f;
#if DEBUG
            cameraControls = InputManager.Keyboard.Get2DInput(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
#endif
            //Initialize Map bounds
            //World map stars drawing at the upper left corner of the map.
            mapXMin = WorldMap.X;
            mapXMax = mapXMin + WorldMap.Width;
            
            mapYMax = WorldMap.Y;
            mapYMin = mapYMax - WorldMap.Height;

            ClampCameraToMapEdge();
        }

        private void InitializeUi()
        {
            this.GroupSelectorInstance.VisualRepresentation = GroupSelectorGumInstance;
            this.GroupSelectorInstance.IsInSelectionMode = true;

            this.GroupSelectorInstance.SelectionFinished += HandleGroupSelection;

            UpdateResourceDisplay();
        }

        private void HandleGroupSelection(object sender, EventArgs e)
        {
            selectedUnits.Clear();

            //If we are attempting a group selection we should prep for the selected building being deselcted.
            selectedBuilding = null;

            foreach(var unit in this.UnitList)
            {
                bool canSelect =
                    unit.UnitData.IsEnemy == false && unit.CollideAgainst(GroupSelectorInstance);

#if DEBUG
                if(DebuggingVariables.CanSelectEnemies)
                {
                    // a little inefficient but whatever, it's debug
                    canSelect = unit.CollideAgainst(GroupSelectorInstance);
                }
#endif

                if (canSelect)
                {
                    selectedUnits.Add(unit);
                }
            }
            
            //If we did not select any units and a buildng is under the cursor, select the building.
            if (selectedUnits.Count == 0)
            {

                var buildingOver = BuildingList.FirstOrDefault(item => item.HasCursorOver(GuiManager.Cursor));
                if (buildingOver != null)
                {
                    selectedBuilding = buildingOver;
                    if (selectedBuilding.IsConstructionComplete)
                    {
                        //ActionToolbarInstance.ShowAvailableUnits(selectedBuilding.TrainableUnits);
                        ActionToolbarInstance.SetViewFromEntity(selectedBuilding);
                    }
                }
            }

            UpdateSelectionMarker();
            CheckSelectionState();
        }

        private void InitializeNodeNetwork()
        {
            TileNodeNetwork.VisibleCoefficient = 3;

            tileNodeNetwork = new TileNodeNetwork(GridWidth / 2f,
                -WorldMap.Height + GridWidth / 2f,
                GridWidth,
                MathFunctions.RoundToInt(WorldMap.Width / WorldMap.WidthPerTile.Value),
                MathFunctions.RoundToInt(WorldMap.Height / WorldMap.HeightPerTile.Value),
                DirectionalType.Eight);

            tileNodeNetwork.FillCompletely();

            var namesToExclude = WorldMap.Properties
                .Where(item => item.Value
                    .Any(customProperty => customProperty.Name == "BlocksPathfinding" && (string)customProperty.Value == "true"));

            foreach(var layer in WorldMap.MapLayers)
            {
                foreach(var name in namesToExclude)
                {
                    var indexes =  layer.NamedTileOrderedIndexes.ContainsKey(name.Key) ? layer.NamedTileOrderedIndexes[name.Key] : null;

                    if(indexes != null)
                    {
                        foreach(var index in indexes)
                        {
                            float x, y;
                            layer.GetBottomLeftWorldCoordinateForOrderedTile(index, out x, out y);

                            var toRemove = tileNodeNetwork.TiledNodeAtWorld(x + GridWidth/2, y + GridWidth/2);

                            if(toRemove != null)
                            {
                                tileNodeNetwork.Remove(toRemove);
                            }
                        }
                    }
                }
            }

#if DEBUG
            tileNodeNetwork.Visible = Entities.DebuggingVariables.ShowNodeNetwork;
#else
            tileNodeNetwork.Visible = false;
#endif
        }

        private void InitializeEvents()
        {
            ActionToolbarInstance.TrainUnitInvokedFromActionToolbar += (notused, args) =>
            {
                HandlePerfromTrain(args.UnitData);
            };
        }

        #endregion

        #region Activity Methods

        void CustomActivity(bool firstTimeCalled)
        {
            HotkeyActivity();

            ClickActivity();

            CameraMovementActivity();

            CollisionActivity();

            BuildMarkerActivity();
        }

        private void BuildMarkerActivity()
        {
            if(ActionToolbarInstance.GetActionStateBaseOnUi() == ActionMode.Build && GetIfCanClickInWorld())
            {
                BuildingMarkerInstance.Visible = true;
                BuildingMarkerInstance.BuildingData = ActionToolbarInstance.SelectedBuildingData;
                float x, y;

                GetBuildLocationFromCursor(out x, out y);


                bool isInvalid = BuildingList.Any(item => item.Collision.CollideAgainst(BuildingMarkerInstance.AxisAlignedRectangleInstance))
                    || woodResourceShapeCollection.Rectangles.Any(rect => rect.CollideAgainst(BuildingMarkerInstance.AxisAlignedRectangleInstance))
                    || stoneResourceShapeCollection.Rectangles.Any(rect => rect.CollideAgainst(BuildingMarkerInstance.AxisAlignedRectangleInstance))
                    || waterResourceShapeCollection.Rectangles.Any(rect => rect.CollideAgainst(BuildingMarkerInstance.AxisAlignedRectangleInstance));

                if (isInvalid )
                {
                    BuildingMarkerInstance.CurrentState = Entities.BuildingMarker.VariableState.Invalid;
                }
                else
                {
                    BuildingMarkerInstance.CurrentState = Entities.BuildingMarker.VariableState.Normal;

                }

                BuildingMarkerInstance.X = x;
                BuildingMarkerInstance.Y = y;
                // put it above other stuff
                BuildingMarkerInstance.Z = 3;
            }
            else
            {
                // Reset BuildingMarkerInstance state.
                BuildingMarkerInstance.Visible = false;
                if (BuildingMarkerInstance.CurrentState != Entities.BuildingMarker.VariableState.Invalid) {
                    BuildingMarkerInstance.CurrentState = Entities.BuildingMarker.VariableState.Invalid;
                }
            }
        }

        private void CollisionActivity()
        {
            PerformUnitsVsTerrainCollision();

            PerformUnitsVsUnitsCollision();
        }

        private void PerformUnitsVsTerrainCollision()
        {
            for (int i = 0; i < UnitList.Count; i++)
            {
                stoneResourceShapeCollection.CollideAgainstSolid(UnitList[i].CircleInstance);
                woodResourceShapeCollection.CollideAgainstSolid(UnitList[i].CircleInstance);
            }
        }

        private void PerformUnitsVsUnitsCollision()
        {
            for(int i = 0; i < UnitList.Count -1; i++)
            {
                var first = UnitList[i];
                for(int j = i+1; j < UnitList.Count; j++)
                {
                    var second = UnitList[j];
                    if(first.CircleInstance.CollideAgainstMove(second.CircleInstance, 1, 1))
                    {
                        var firstRepositionVector = new Vector3(
                            first.CircleInstance.LastMoveCollisionReposition.X,
                            first.CircleInstance.LastMoveCollisionReposition.Y, 0);

                        var secondRepositionVector = new Vector3(
                            second.CircleInstance.LastMoveCollisionReposition.X,
                            second.CircleInstance.LastMoveCollisionReposition.Y, 0);

                        first.Position -= firstRepositionVector;
                        second.Position -= secondRepositionVector;

                        first.Position += firstRepositionVector * TimeManager.SecondDifference;
                        second.Position += secondRepositionVector * TimeManager.SecondDifference;
                    }
                }
            }
        }

        private void HotkeyActivity()
        {
            var completedBuildings = BuildingList.Where(item => item.IsConstructionComplete).ToList();
            ActionToolbarInstance.UpdateButtonEnabledStates(Lumber, Stone, Gold, CurrentCapacityUsed, MaxCapacity, completedBuildings);
            if(InputManager.Keyboard.AnyKeyPushed())
            {
                ActionToolbarInstance.ReactToKeyPress();
            }

        }

        private void CameraMovementActivity()
        {
#if DEBUG

            const float cameraMovementSpeed = 200;
            Camera.Main.XVelocity = cameraMovementSpeed * cameraControls.X;
            Camera.Main.YVelocity = cameraMovementSpeed * cameraControls.Y;
#endif

            var cursor = GuiManager.Cursor;
            if(cursor.MiddleDown)
            {
                //Minusequals - we want to pull the map in the direction of the cursor.
                Camera.Main.X -= cursor.WorldXChangeAt(0);
                Camera.Main.Y -= cursor.WorldYChangeAt(0);

                //Clamp to map bounds.
                ClampCameraToMapEdge();
            }
        }

        private void ClampCameraToMapEdge()
        {
            var camera = Camera.Main;

            if (camera.AbsoluteLeftXEdgeAt(0) < mapXMin)
            {
                camera.X = mapXMin + camera.OrthogonalWidth / 2;
            }
            else if (camera.AbsoluteRightXEdgeAt(0) > mapXMax)
            {
                camera.X = mapXMax - camera.OrthogonalWidth / 2;
            }

            if (camera.AbsoluteBottomYEdgeAt(0) < mapYMin)
            {
                camera.Y = mapYMin + camera.OrthogonalHeight / 2;
            }
            else if (camera.AbsoluteTopYEdgeAt(0) > mapYMax)
            {
                camera.Y = mapYMax - camera.OrthogonalHeight / 2;
            }
        }

        private void ClickActivity()
        {
            var cursor = GuiManager.Cursor;
            FlatRedBall.Debugging.Debugger.Write(GuiManager.Cursor.WindowOver);

            if(cursor.PrimaryClick && !GroupSelectorInstance.WasReleasedThisFrame)
            {
                if (GetIfCanClickInWorld())
                {
                    //Update: February 11, 2017
                    //Rick Blaylock
                    //After implementing hotkeys and proper unit/building data I ran into issues where the action mode would not update on a double click.
                    //For now, we will check the toggle state on clicks.
                    switch(ActionToolbarInstance.GetActionStateBaseOnUi())
                    {
                        case ActionMode.Build:
                            if (BuildingMarkerInstance.CurrentState == Entities.BuildingMarker.VariableState.Normal)
                            {
                                HandlePerformBuilding();
                                HandlePostClick();
                            }
                            break;
                        case ActionMode.Select:
                            HandlePerformSelection();
                            break;
                    }

#if DEBUG
                    DebugClickActivity();
#endif
                }
            }

            if(cursor.SecondaryClick)
            {
                HandleSecondaryClick();
            }
        }

        private bool GetIfCanClickInWorld()
        {
            var cursor = GuiManager.Cursor;

            return cursor.WindowOver == null || cursor.WindowOver == this.ResourceDisplayInstance;
        }

        private void DebugClickActivity()
        {
            var keyboard = InputManager.Keyboard;
            if(keyboard.KeyDown(Keys.D1) || GuiManager.Cursor.PrimaryDoubleClick)
            {
                DebugAddUnit(GlobalContent.UnitData[UnitData.Fighter]);
            }

            var cursor = GuiManager.Cursor;
            var worldX = cursor.WorldXAt(0);
            var worldY = cursor.WorldYAt(0);

            const int amountToAddPerClick = 8;

            if (goldResourceShapeCollection.GetTileAt(worldX, worldY) != null)
            {
                Gold += amountToAddPerClick;
                UpdateResourceDisplay();
            }
            if (woodResourceShapeCollection.GetTileAt(worldX, worldY) != null)
            {
                Lumber += amountToAddPerClick;
                UpdateResourceDisplay();
            }
            if (stoneResourceShapeCollection.GetTileAt(worldX, worldY) != null)
            {
                Stone += amountToAddPerClick;
                UpdateResourceDisplay();
            }
        }

        private void DebugAddUnit(UnitData unitData)
        {
            var newUnit = Factories.UnitFactory.CreateNew();
            newUnit.NodeNetwork = this.tileNodeNetwork;
            newUnit.AllUnits = UnitList;
            newUnit.AllBuildings = BuildingList;
            newUnit.X = GuiManager.Cursor.WorldXAt(0);
            newUnit.Y = GuiManager.Cursor.WorldYAt(0);
            newUnit.Z = 1;
            newUnit.UnitData = unitData;

        }

        private void HandlePostClick()
        {
            if(InputManager.Keyboard.KeyDown(Keys.LeftShift) == false && InputManager.Keyboard.KeyDown(Keys.RightShift) == false)
            {
                ActionToolbarInstance.SetActionMode(ActionMode.Select);
            }
        }

        private void HandleSecondaryClick()
        {
            HandleSelectedBuilding();
            HandleSelectedUnitsRightClick();
        }

        private void HandleSelectedBuilding()
        {
            //Create a rally point for units that are created.
            //If the selected building can train units.
            if(selectedBuilding != null && selectedBuilding.HasTrainableUnits)
            {
                Cursor cursor = GuiManager.Cursor;

                var worldX = cursor.WorldXAt(0);
                var worldY = cursor.WorldYAt(0);

                selectedBuilding.RallyPoint = new Vector3() { X = worldX, Y = worldY};
            }
        }

        private void HandleSelectedUnitsRightClick()
        {
            //Only do this work if we have selected units
            if (selectedUnits.Count > 0)
            {
                Cursor cursor = GuiManager.Cursor;

                var worldX = cursor.WorldXAt(0);
                var worldY = cursor.WorldYAt(0);

                // Are we right-clicking a resource?
                var woodResourceOver = woodResourceShapeCollection.GetTileAt(worldX, worldY);
                var stoneResourceOver = stoneResourceShapeCollection.GetTileAt(worldX, worldY);
                var goldResourceOver = goldResourceShapeCollection.GetTileAt(worldX, worldY);
                var resourceOver = woodResourceOver ?? stoneResourceOver ?? goldResourceOver;
                string resourceType = null;
                if (woodResourceOver != null) { resourceType = "Wood"; }
                else if (stoneResourceOver != null) { resourceType = "Stone"; }
                else if (goldResourceOver != null) { resourceType = "Gold"; }

                var enemyOver = UnitList.FirstOrDefault(item =>
                    item.UnitData.IsEnemy && item.HasCursorOver(cursor));

                foreach (var selectedUnit in selectedUnits)
                {
                    if (enemyOver != null)
                    {
                        selectedUnit.AssignAttackGoal(enemyOver);
                    }
                    else if (resourceOver != null)
                    {
                        var clickLocation = new Vector3(worldX, worldY, 0);
                        selectedUnit.AssignResourceCollectGoal(clickLocation, resourceOver, resourceType);
                    }
                    else
                    {
                        // todo: do we want to differentiate between move and move+attack?
                        const bool forceWalk = false;

                        if(selectedUnit.UnitData.InitiatesBattle == false || forceWalk)
                        {
                            selectedUnit.AssignMoveGoal(worldX, worldY);
                        }
                        else
                        {
                            // todo: this eventually will get removed, but it's here for testing:
                            if(selectedUnit.UnitData.IsEnemy)
                            {
                                selectedUnit.AssignAttackThenRetreat(worldX, worldY);
                            }
                            else
                            {
                                selectedUnit.AssignMoveAttackGoal(worldX, worldY);
                            }
                        }
                    }
                }
            }
        }

        private void HandlePerformSelection()
        {
            //Clear selected building and units
            selectedUnits.Clear();
            selectedBuilding = null;

            var cursor = GuiManager.Cursor;

            var unitOver = UnitList.FirstOrDefault(item => 
                item.UnitData.IsEnemy == false && item.HasCursorOver(cursor));

#if DEBUG
            if(DebuggingVariables.CanSelectEnemies)
            {
                // doubles the check but it's debug so who cares
                unitOver = UnitList.FirstOrDefault(item =>
                    item.HasCursorOver(cursor));
            }
#endif


            if(unitOver != null)
            {
                selectedUnits.Add(unitOver);

            }

            if(selectedUnits.Count == 0)
            {
                var buildingOver = BuildingList.FirstOrDefault(item => item.HasCursorOver(cursor));
                if (buildingOver != null)
                {
                    selectedBuilding = buildingOver;
                    if (selectedBuilding.IsConstructionComplete)
                    {
                        //ActionToolbarInstance.ShowAvailableUnits(selectedBuilding.TrainableUnits);
                        ActionToolbarInstance.SetViewFromEntity(selectedBuilding);
                    }
                }
            }


            UpdateSelectionMarker();
            CheckSelectionState();
        }

        public void CheckSelectionState()
        {
            if(selectedBuilding == null)
            {
                this.ActionToolbarInstance.SetViewFromEntity(null);
            }
        }

        private void UpdateSelectionMarker()
        {
            while(SelectionMarkerList.Count > selectedUnits.Count)
            {
                SelectionMarkerList.Last().Destroy();
            }
            while(SelectionMarkerList.Count < selectedUnits.Count)
            {
                var selectionMarker = new Entities.SelectionMarker();
                SelectionMarkerList.Add(selectionMarker);
            }

            for(int i = 0; i < SelectionMarkerList.Count; i++)
            {
                SelectionMarkerList[i].AttachTo(selectedUnits[i], false);
            }
        }
        
        private void HandlePerfromTrain(DataTypes.UnitData unitData)
        {
            if (unitData == null)
            {
                throw new Exception("Tried to train a null unit.");
            }
            
            //Only check gold first.
            bool hasEnoughGold = unitData.GoldCost <= this.Gold;

#if DEBUG
            if (Entities.DebuggingVariables.HasInfiniteResources)
            {
                hasEnoughGold = true;
            }
#endif

            //If we have enough gold, we will add the unit to the end of the queue.
            //The training will not occur until there is enough capacity, but units can be queued as long as there is gold.
            bool wasAddeToQueue = hasEnoughGold ? selectedBuilding.TryAddUnitToTrain(unitData.Name) : false;

            if (wasAddeToQueue)
            {
                bool shouldUpdateResources = true;
#if DEBUG

                shouldUpdateResources = Entities.DebuggingVariables.HasInfiniteResources == false;
#endif
                if (shouldUpdateResources)
                {
                    this.Gold -= unitData.GoldCost;
                }

                UpdateResourceDisplay();
            }
            else
            {
                //tell them?
            }

        } 

        private void HandlePerformBuilding()
        {

            DataTypes.BuildingData buildingType = ActionToolbarInstance.SelectedBuildingData;

            if(buildingType == null)
            {
                throw new Exception("Building Data is null.");
            }

            bool hasEnoughResources = this.Lumber >= buildingType.LumberCost && this.Stone >= buildingType.StoneCost;

            bool isOverOtherBuilding = false;
            if(hasEnoughResources)
            {
                var cursor = GuiManager.Cursor;
                isOverOtherBuilding = BuildingList.Any(item => item.HasCursorOver(cursor));
            }

            if(hasEnoughResources && !isOverOtherBuilding)
            {
                // do it!
                var building = Factories.BuildingFactory.CreateNew();
                float x, y;
                GetBuildLocationFromCursor(out x, out y);

                building.X = x;
                building.Y = y;
                building.Z = 1;

                building.StartBuilding();

                building.BuildingData = buildingType;

                bool shouldUpdateResources = true;
#if DEBUG

                shouldUpdateResources = Entities.DebuggingVariables.HasInfiniteResources == false;
#endif
                if (shouldUpdateResources)
                {
                    this.Lumber -= buildingType.LumberCost;
                    this.Stone -= buildingType.StoneCost;
                    this.MaxCapacity = BuildingList.Sum(item => item.BuildingData.Capacity);
                }

                UpdateResourceDisplay();
            }
            else
            {
                // tell them?
            }
        }

        private static void GetBuildLocationFromCursor(out float x, out float y)
        {
            var cursor = GuiManager.Cursor;

            x = cursor.WorldXAt(0);
            y = cursor.WorldYAt(0);
            const float tilesWide = 3;

            x = MathFunctions.RoundFloat(x, GridWidth * tilesWide, GridWidth * tilesWide / 2);
            y = MathFunctions.RoundFloat(y, GridWidth * tilesWide, GridWidth * tilesWide / 2);
        }

        public bool CheckIfCanStartTraining(string unit)
        {
            var unitCapacity = GlobalContent.UnitData[unit].Capacity;

            var potentialCapacityUsed = CurrentCapacityUsed + unitCapacity;

            bool canTrain = false;
            if (potentialCapacityUsed <= MaxCapacity)
            {
                canTrain = true;
                currentTrainingCapacity += unitCapacity;

                UpdateResourceDisplay();
            }
            return canTrain;
        }

        public void UpdateResourceDisplay()
        {
            this.ResourceDisplayInstance.CapacityText = $"{CurrentCapacityUsed}/{this.MaxCapacity.ToString()}";
            this.ResourceDisplayInstance.LumberText = this.Lumber.ToString();
            this.ResourceDisplayInstance.StoneText = this.Stone.ToString();
            this.ResourceDisplayInstance.GoldText = this.Gold.ToString();
        }

        #endregion

        public Entities.Unit SpawnNewUnit(string unitDataKey, Vector3 spawnPoint)
        {
            var newUnit = Factories.UnitFactory.CreateNew();

            newUnit.Position = spawnPoint;
            var unitData = GlobalContent.UnitData[unitDataKey];
            newUnit.UnitData = unitData;
            newUnit.AllUnits = this.UnitList;
            newUnit.AllBuildings = this.BuildingList;
            newUnit.NodeNetwork = this.tileNodeNetwork;

            //Capacity is, current units in training + the units which have been trained.
            //Do not count enemy units.
            if (unitData.IsEnemy == false)
            {
                currentTrainingCapacity -= unitData.Capacity;
                CurrentCapacityUsed = UnitList.Select(x => x.UnitData).Where(x => x.IsEnemy == false).Sum(x => x.Capacity); //makes sure we have the correct capacity when units are trained.
                UpdateResourceDisplay();
            }

            return newUnit;
        }

        void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
