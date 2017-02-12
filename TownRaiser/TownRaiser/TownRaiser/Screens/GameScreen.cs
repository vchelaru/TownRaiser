
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

        TileNodeNetwork tileNodeNetwork;

        const float gridWidth = 16;

        I2DInput cameraControls;

        List<Entities.Unit> selectedUnits = new List<Entities.Unit>();

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

            InitializeUi();
        }

        private void InitializeUi()
        {
            this.GroupSelectorInstance.VisualRepresentation = GroupSelectorGumInstance;
            this.GroupSelectorInstance.IsInSelectionMode = true;

            this.GroupSelectorInstance.SelectionFinished += HandleGroupSelection;
        }

        private void HandleGroupSelection(object sender, EventArgs e)
        {
            selectedUnits.Clear();
            foreach(var unit in this.UnitList)
            {
                if(unit.CollideAgainst(GroupSelectorInstance))
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
            ActionToolbarInstance.ModeChanged += (not, used) => 
            {
                //this.ActionMode = ActionToolbarInstance.GetActionModeBasedOnToggleState();
            };
        }

        #endregion

        #region Activity Methods

        void CustomActivity(bool firstTimeCalled)
        {
            HotkeyActivity();

            ClickActivity();

            CameraMovementActivity();
        }
        
        private void HotkeyActivity()
        {
            //Rick Blaylock
            //Old implementation keeping around while I test hoteys.
            //if (InputManager.Keyboard.KeyPushed(Keys.Escape))
            //{
            //    ActionMode = ActionMode.Select;
            //    ActionToolbarInstance.SetMode(ActionMode);
            //}

            if(InputManager.Keyboard.AnyKeyPushed())
            {
                ActionToolbarInstance.ReactToKeyPress();
                //ActionMode = ActionToolbarInstance.GetActionModeBasedOnToggleState();
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

            foreach (var selectedUnit in selectedUnits)
            {

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
            var cursor = GuiManager.Cursor;
            var x = cursor.WorldXAt(0);
            var y = cursor.WorldYAt(0);
            var newUnit = Factories.UnitFactory.CreateNew();
            newUnit.NodeNetwork = this.tileNodeNetwork;
            newUnit.X = x;
            newUnit.Y = y;
            newUnit.Z = 1;

            // set the data?
            var unitData = ActionToolbarInstance.SelectedUnitData;

            if(unitData == null)
            {
                throw new Exception("Unit data is null.");
            }

            newUnit.UnitData = ActionToolbarInstance.SelectedUnitData;

            UpdateResourceDisplay();
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
