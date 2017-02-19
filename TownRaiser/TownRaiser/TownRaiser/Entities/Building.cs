#region Usings

using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;

using FlatRedBall.Math.Geometry;
using FlatRedBall.Math.Splines;
using BitmapFont = FlatRedBall.Graphics.BitmapFont;
using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using TownRaiser.Interfaces;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

#endif
#endregion

namespace TownRaiser.Entities
{
	public partial class Building: IUpdatesStatus
	{
        #region Fields/Properties

        private const int MaxTrainableUnits = 5;

        public event EventHandler OnDestroy;
        public event EventHandler<UpdateStatusEventArgs> UpdateStatus;
        public int CurrentHealth { get; set; }
        public IEnumerable<string> TrainableUnits => BuildingData.TrainableUnits.AsReadOnly();
        public Unit CurrentTrainingUnit => TrainingQueue.Count > 0 ? TrainingQueue[0] : null;

        public Vector3? RallyPoint;
        double constructionTimeStarted = 0;
        public bool IsConstructionComplete { get; private set; } = true;
        public bool HasTrainableUnits => BuildingData.TrainableUnits.Count > 0;

        //For now, we will spawn to the bottom right corner of the building's AAR.
        public float UnitSpawnX => X + AxisAlignedRectangleInstance.Width / 2;
        public float UnitSpawnY => Y -AxisAlignedRectangleInstance.Height / 2;

        //While it's a list, we will treat is as a queue.
        //Queues will not let us return it as readonly.
        public List<Unit> TrainingQueue;

        #endregion


        /// <summary>
        /// Initialization logic which is execute only one time for this Entity (unless the Entity is pooled).
        /// This method is called when the Entity is added to managers. Entities which are instantiated but not
        /// added to managers will not have this method called.
        /// </summary>
        private void CustomInitialize()
		{
            TrainingQueue = new List<Unit>();

            this.HealthBarRuntimeInstance.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.HealthBarRuntimeInstance.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Bottom;

#if DEBUG
            this.AxisAlignedRectangleInstance.Visible = DebuggingVariables.ShowBuildingOutline;
#endif
        }

		private void CustomActivity()
		{
            ConstructionActivity();
            HealthBarActivity();
            TrainingActivity();
		}

        private void ConstructionActivity()
        {
            if(!IsConstructionComplete)
            {
                var ratioComplete = 
                    FlatRedBall.Screens.ScreenManager.CurrentScreen.PauseAdjustedSecondsSince(constructionTimeStarted) / BuildingData.BuildTime;

                if(ratioComplete < .5)
                {
                    SpriteInstance.CurrentChainName = "BeingBuilt1";
                }
                else if(ratioComplete < 1)
                {
                    SpriteInstance.CurrentChainName = "BeingBuilt2";
                }
                else
                {
                    SpriteInstance.CurrentChainName = BuildingData.Name;
                    IsConstructionComplete = true;
                }
            }
        }


        internal void StartBuilding()
        {
            IsConstructionComplete = false;
            constructionTimeStarted = FlatRedBall.Screens.ScreenManager.CurrentScreen.PauseAdjustedCurrentTime;
            // to force an immediate update of visuals
            ConstructionActivity();
        }

        private void HealthBarActivity()
        {

            var healthPercentage = 100 * this.CurrentHealth / (float)BuildingData.Health;

            this.HealthBarRuntimeInstance.HealthPercentage = healthPercentage;
            this.HealthBarRuntimeInstance.PositionTo(this, -14);
        }

        private void TrainingActivity()
        {
            if (TrainingQueue.Count > 0)
            {
                var trainingUnit = TrainingQueue[0];
                if(trainingUnit.CurrentTrainingStatusState == Unit.TrainingStatus.TrainingComplete)
                {
                    if(RallyPoint.HasValue)
                    {
                        trainingUnit.CreatMoveGoal(RallyPoint.Value.X, RallyPoint.Value.Y);
                    }
                    TrainingQueue.Remove(trainingUnit);
                    if(TrainingQueue.Count > 0)
                    {
                        TrainingQueue[0].StartTraining();
                    }
                }
                this.UpdateStatus?.Invoke(this, new UpdateStatusEventArgs());
            }
        }
        public void AddUnitToTrain(Unit unit)
        {
            if(TrainingQueue.Count == 0)
            {
                unit.StartTraining();
            }

            TrainingQueue.Add(unit);
            
            this.UpdateStatus?.Invoke(this, new UpdateStatusEventArgs());
        }

        public float GetHealthRatio()
        {
            
            return CurrentHealth / BuildingData.Health;
        }

        private void CustomDestroy()
		{
            this.OnDestroy?.Invoke(this, null);
            this.UpdateStatus?.Invoke(this, new UpdateStatusEventArgs() { WasEntityDestroyed = true });
            foreach (var unit in TrainingQueue)
            {
                unit.Destroy();
            }
            TrainingQueue.Clear();
            TrainingQueue = null;

            this.OnDestroy = null;
            this.UpdateStatus = null;
		}

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }

        internal void TakeDamage(int attackDamage)
        {
            CurrentHealth -= attackDamage;
            if (CurrentHealth <= 0)
            {
                Destroy();
            }
        }

        internal void InterpolateToState(object buildComplete, double buildTime)
        {
            throw new NotImplementedException();
        }


    }
}
