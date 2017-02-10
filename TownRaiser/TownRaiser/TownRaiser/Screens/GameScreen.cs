
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

        public ActionMode ActionMode { get; set; }

        public int Lumber { get; set; } = 10000;
        public int Stone { get; set; } = 10000;
        public int Gold { get; set; } = 10000;

        TileNodeNetwork tileNodeNetwork;

        const float gridWidth = 16;

        I2DInput cameraControls;

        Entities.Unit selectedUnit;

        #endregion

        #region Initialize Methods

        void CustomInitialize()
		{
            Camera.Main.X = Camera.Main.RelativeXEdgeAt(0);
            Camera.Main.Y = -Camera.Main.RelativeYEdgeAt(0);

            cameraControls = InputManager.Keyboard.Get2DInput(Keys.A, Keys.D, Keys.W, Keys.S);

            FlatRedBall.Debugging.Debugger.TextCorner = FlatRedBall.Debugging.Debugger.Corner.TopRight;

            InitializeEvents();

            InitializeNodeNetwork();
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
            ActionToolbarInstance.BuildClicked += (not, used) => 
            {
                this.ActionMode = ActionToolbarInstance.GetActionModeBasedOnToggleState();
                ActionToolbarInstance.UntoggleAllExcept(ActionMode);
            };
            ActionToolbarInstance.TrainClicked += (not, used) => 
            {
                this.ActionMode = ActionToolbarInstance.GetActionModeBasedOnToggleState();
                ActionToolbarInstance.UntoggleAllExcept(ActionMode);

            };
        }

        #endregion

        #region Activity Methods

        void CustomActivity(bool firstTimeCalled)
        {
            EscapePressActivity();

            ClickActivity();

            CameraMovementActivity();
        }
        
        private void EscapePressActivity()
        {
            if (InputManager.Keyboard.KeyPushed(Keys.Escape))
            {
                ActionMode = ActionMode.Select;
            }

        }

        private void CameraMovementActivity()
        {
            const float cameraMovementSpeed = 200;
            Camera.Main.XVelocity = cameraMovementSpeed * cameraControls.X;
            Camera.Main.YVelocity = cameraMovementSpeed * cameraControls.Y;
        }

        private void ClickActivity()
        {
            var cursor = GuiManager.Cursor;

            FlatRedBall.Debugging.Debugger.Write(GuiManager.Cursor.WindowOver);

            if(cursor.PrimaryClick)
            {
                if (cursor.WindowOver == null || cursor.WindowOver == this.ResourceDisplayInstance)
                {
                    switch(ActionMode)
                    {
                        case ActionMode.Build:
                            HandlePerformBuilding();
                            break;
                        case ActionMode.Train:
                            HandlePerformTrain();
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

        private void HandleSecondaryClick()
        {
            Cursor cursor = GuiManager.Cursor;
            if(this.selectedUnit != null)
            {
                var worldX = cursor.WorldXAt(0);
                var worldY = cursor.WorldYAt(0);

                selectedUnit.ImmediateGoal = new AI.ImmediateGoal
                {
                    TargetPosition = new Vector3(worldX, worldY, 0)
                };
            }
        }

        private void HandlePerformSelection()
        {
            var cursor = GuiManager.Cursor;
            var unitOver = UnitList.FirstOrDefault(item => item.HasCursorOver(cursor));

            selectedUnit = unitOver;

            UpdateSelectionMarker();
        }

        private void UpdateSelectionMarker()
        {
            if(selectedUnit == null)
            {
                while(SelectionMarkerList.Count != 0)
                {
                    SelectionMarkerList.Last().Destroy();
                }
            }
            else
            {
                var neededCount = 1;
                while(SelectionMarkerList.Count() < neededCount)
                {
                    var selectionMarker = new Entities.SelectionMarker();
                    SelectionMarkerList.Add(selectionMarker);
                    // eventually make this consider multiple selected units
                }

                SelectionMarkerList.First().AttachTo(selectedUnit, false);
            }
        }

        private void HandlePerformTrain()
        {
            var cursor = GuiManager.Cursor;
            var x = cursor.WorldXAt(0);
            var y = cursor.WorldYAt(0);
            var newUnit = Factories.UnitFactory.CreateNew();
            newUnit.X = x;
            newUnit.Y = y;
            newUnit.Z = 1;

            // set the data?
            newUnit.UnitData = GlobalContent.UnitData[DataTypes.UnitData.Fighter];

            UpdateResourceDisplay();
        }

        private void HandlePerformBuilding()
        {

            DataTypes.BuildingData buildingType = GetSelectedBuildingType();

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

                bool shouldSubtract = true;
#if DEBUG

                shouldSubtract = Entities.DebuggingVariables.HasInfiniteResources == false;
#endif
                if(shouldSubtract)
                {
                    this.Lumber -= buildingType.LumberCost;
                    this.Stone -= buildingType.StoneCost;
                }

                UpdateResourceDisplay();
            }
            else
            {
                // tell them?
            }
        }

        private static DataTypes.BuildingData GetSelectedBuildingType()
        {
            DataTypes.BuildingData buildingType = GlobalContent.BuildingData[DataTypes.BuildingData.Tent];

            if (FlatRedBallServices.Random.Next(2) == 0)
            {
                buildingType = GlobalContent.BuildingData[DataTypes.BuildingData.House];
            }
            else
            {
                buildingType = GlobalContent.BuildingData[DataTypes.BuildingData.Tent];

            }

            return buildingType;
        }

        private void UpdateResourceDisplay()
        {
            this.ResourceDisplayInstance.CapacityText = BuildingList.Sum(item => item.BuildingData.Capacity).ToString();
            this.ResourceDisplayInstance.LumberText = this.Lumber.ToString();
            this.ResourceDisplayInstance.StoneText = this.Stone.ToString();
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
