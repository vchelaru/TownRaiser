
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

        //public ActionMode ActionMode { get; set; } //see comment in ClickActivity to see why this is commented out.

        public int Lumber { get; set; } = 10000;
        public int Stone { get; set; } = 10000;
        public int Gold { get; set; } = 10000;
        public int CurrentCapacityUsed { get; set; }
        public int MaxCapacity { get; set; }

        TileNodeNetwork tileNodeNetwork;

        const float gridWidth = 16;

        List<Entities.Unit> selectedUnits = new List<Entities.Unit>();

        private float mapXMin;
        private float mapXMax;
        private float mapYMin;
        private float mapYMax;

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

            InitializeUi();
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
            foreach(var unit in this.UnitList)
            {
                if(unit.UnitData.IsEnemy == false && unit.CollideAgainst(GroupSelectorInstance))
                {
                    selectedUnits.Add(unit);
                }
            }
            UpdateSelectionMarker();
        }

        private void InitializeNodeNetwork()
        {
            TileNodeNetwork.VisibleCoefficient = 3;

            tileNodeNetwork = new TileNodeNetwork(gridWidth / 2f,
                -WorldMap.Height + gridWidth / 2f,
                gridWidth,
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

                            var toRemove = tileNodeNetwork.TiledNodeAtWorld(x + gridWidth/2, y + gridWidth/2);

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
            
        }

        #endregion

        #region Activity Methods

        void CustomActivity(bool firstTimeCalled)
        {
            HotkeyActivity();

            ClickActivity();

            CameraMovementActivity();

            CollisionActivity();
        }

        private void CollisionActivity()
        {
            PerformUnitsVsTerrainCollision();

            PerformUnitsVsUnitsCollision();
        }

        private void PerformUnitsVsTerrainCollision()
        {
            // for adam to do this
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
            ActionToolbarInstance.UpdateButtonsOnMoney(Lumber, Stone, Gold, CurrentCapacityUsed, MaxCapacity);
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
                if (cursor.WindowOver == null || cursor.WindowOver == this.ResourceDisplayInstance)
                {
                    //Update: February 11, 2017
                    //Rick Blaylock
                    //After implementing hotkeys and proper unit/building data I ran into issues where the action mode would not update on a double click.
                    //For now, we will check the toggle state on clicks.
                    switch(ActionToolbarInstance.GetActionModeBasedOnToggleState())
                    {
                        case ActionMode.Build:
                            HandlePerformBuilding();
                            HandlePostClick();
                            break;
                        case ActionMode.Train:
                            HandlePerformTrain();
                            HandlePostClick();
                            break;
                        case ActionMode.Select:
                            HandlePerformSelection();
                            break;
                    }
                }
            }

            if(cursor.SecondaryClick)
            {
                HandleSecondaryClick();
            }
        }

        private void HandlePostClick()
        {
            if(InputManager.Keyboard.KeyDown(Keys.LeftShift) == false && InputManager.Keyboard.KeyDown(Keys.RightShift) == false)
            {
                ActionToolbarInstance.SetMode(ActionMode.Select);
            }
        }

        private void HandleSecondaryClick()
        {



            Cursor cursor = GuiManager.Cursor;

            var worldX = cursor.WorldXAt(0);
            var worldY = cursor.WorldYAt(0);

            var enemyOver = UnitList.FirstOrDefault(item =>
                item.UnitData.IsEnemy && item.HasCursorOver(cursor));

            foreach (var selectedUnit in selectedUnits)
            {
                if(enemyOver != null)
                {
                    selectedUnit.CreateAttackGoal(enemyOver);
                }
                else
                {
                    selectedUnit.HighLevelGoal = null;
                    selectedUnit.ImmediateGoal = new AI.ImmediateGoal
                    {
                        TargetPosition = new Vector3(worldX, worldY, 0)
                    };
                }
            }
        }

        private void HandlePerformSelection()
        {
            var cursor = GuiManager.Cursor;
            var unitOver = UnitList.FirstOrDefault(item => 
                item.UnitData.IsEnemy == false && item.HasCursorOver(cursor));

            selectedUnits.Clear();
            if(unitOver != null)
            {
                selectedUnits.Add(unitOver);

            }

            UpdateSelectionMarker();
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

        private void HandlePerformTrain()
        {
            // set the data?
            var unitData = ActionToolbarInstance.SelectedUnitData;
            bool hasEnoughGold = unitData.GoldCost <= this.Gold && (unitData.Capacity + CurrentCapacityUsed)<= MaxCapacity;

            if (hasEnoughGold)
            {
                var cursor = GuiManager.Cursor;
                var x = cursor.WorldXAt(0);
                var y = cursor.WorldYAt(0);
                var newUnit = Factories.UnitFactory.CreateNew();
                newUnit.NodeNetwork = this.tileNodeNetwork;
                newUnit.X = x;
                newUnit.Y = y;
                newUnit.Z = 1;



                if (unitData == null)
                {
                    throw new Exception("Unit data is null.");
                }

                newUnit.UnitData = ActionToolbarInstance.SelectedUnitData;

                bool shouldUpdateResources = true;
#if DEBUG

                shouldUpdateResources = Entities.DebuggingVariables.HasInfiniteResources == false;
#endif
                if (shouldUpdateResources)
                {
                    this.Gold -= unitData.GoldCost;
                    this.CurrentCapacityUsed = UnitList.Where(item => item.UnitData.IsEnemy == false).Sum(item => item.UnitData.Capacity);
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
                var cursor = GuiManager.Cursor;

                var building = Factories.BuildingFactory.CreateNew();
                var x = cursor.WorldXAt(0);
                var y = cursor.WorldYAt(0);

                const float tilesWide = 3;

                x = MathFunctions.RoundFloat(x, gridWidth * tilesWide, gridWidth * tilesWide/2);
                y = MathFunctions.RoundFloat(y, gridWidth * tilesWide, gridWidth * tilesWide/2);



                building.X = x;
                building.Y = y;
                building.Z = 1;

                building.BuildingData = buildingType;

                bool shouldUpdateResources = true;
#if DEBUG

                shouldUpdateResources = Entities.DebuggingVariables.HasInfiniteResources == false;
#endif
                if(shouldUpdateResources)
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

        private void UpdateResourceDisplay()
        {
            this.ResourceDisplayInstance.CapacityText = $"{CurrentCapacityUsed}/{this.MaxCapacity.ToString()}";
            this.ResourceDisplayInstance.LumberText = this.Lumber.ToString();
            this.ResourceDisplayInstance.StoneText = this.Stone.ToString();
            this.ResourceDisplayInstance.GoldText = this.Gold.ToString();
        }

#endregion

        void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
