
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
    public enum ActionMode
    {
        Select,
        Train,
        Build
    }

	public partial class GameScreen
	{
        public ActionMode ActionMode { get; set; }

        public int Lumber { get; set; } = 10000;
        public int Stone { get; set; } = 10000;
        public int Gold { get; set; } = 10000;


		void CustomInitialize()
		{
            Camera.Main.X = Camera.Main.RelativeXEdgeAt(0);
            Camera.Main.Y = -Camera.Main.RelativeYEdgeAt(0);

            InitializeEvents();

        }

        private void InitializeEvents()
        {
            ActionToolbarInstance.BuildClicked += (not, used) => this.ActionMode = ActionMode.Build;
            ActionToolbarInstance.TrainClicked += (not, used) => this.ActionMode = ActionMode.Train;
        }

        void CustomActivity(bool firstTimeCalled)
        {
            ClickActivity();

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
                            HandleBuildClick();
                            break;
                        case ActionMode.Train:
                            HandleTrainClick();
                            break;
                    }
                }
            }
        }

        private void HandleTrainClick()
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

        private void HandleBuildClick()
        {
            const float gridWidth = 16;

            DataTypes.BuildingData buildingType = GetSelectedBuildingType();

            bool hasEnoughResources = this.Lumber >= buildingType.LumberCost && this.Stone >= buildingType.StoneCost;

            if(hasEnoughResources)
            {
                // do it!
                var cursor = GuiManager.Cursor;

                var building = Factories.BuildingFactory.CreateNew();
                var x = cursor.WorldXAt(0);
                var y = cursor.WorldYAt(0);

                x = MathFunctions.RoundFloat(x, gridWidth, gridWidth / 2.0f);
                y = MathFunctions.RoundFloat(y, gridWidth, gridWidth / 2.0f);



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

        void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
