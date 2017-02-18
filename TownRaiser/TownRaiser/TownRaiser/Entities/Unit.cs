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
using TownRaiser.AI;
using FlatRedBall.Math;
using FlatRedBall.Screens;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

#endif
#endregion

namespace TownRaiser.Entities
{
	public partial class Unit
	{
        #region Properties

        public ImmediateGoal ImmediateGoal { get; set; }

        public HighLevelGoal HighLevelGoal { get; set; }

        public TileNodeNetwork NodeNetwork { get; set; }

        public PositionedObjectList<Unit> AllUnits { get; set; }

        public int CurrentHealth { get; set; }

        #endregion

        #region Private Fields/Properties
        private double m_TraningStartTime;

        private bool IsTrainingComplete
        {
            get
            {
                var currentScreen = ScreenManager.CurrentScreen;

                return m_TraningStartTime > 0 && currentScreen.PauseAdjustedSecondsSince(m_TraningStartTime) >= UnitData.TrainTime;
            }
        }
        public double TrainingProgressPercent => ScreenManager.CurrentScreen.PauseAdjustedSecondsSince(m_TraningStartTime) / UnitData.TrainTime;
        #endregion

        #region Initialize

        /// <summary>
        /// Initialization logic which is execute only one time for this Entity (unless the Entity is pooled).
        /// This method is called when the Entity is added to managers. Entities which are instantiated but not
        /// added to managers will not have this method called.
        /// </summary>
        private void CustomInitialize()
		{
            //// This should prob be done in Glue instead, but I don't think Glue currently supports this:
            this.HealthBarRuntimeInstance.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.HealthBarRuntimeInstance.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Bottom;
            m_TraningStartTime = -1;

        }

        #endregion

        #region Activity

        private void CustomActivity()
        {
            //We will only perform high and immediateAi activities if the unit has completed training.

            if (CurrentTrainingStatusState == TrainingStatus.TrainingComplete)
            {
                HighLevelActivity();
                ImmediateAiActivity();
                HealthBarActivity();
            }
            else
            {
                TrainingActivity();
            }

        }
        private void HealthBarActivity()
        {
            int screenX = 0;
            int screenY = 0;

            MathFunctions.AbsoluteToWindow(this.X, this.Y, this.Z, ref screenX, ref screenY, Camera.Main);

            var zoom = HealthBarRuntimeInstance.Managers.Renderer.Camera.Zoom;

            var healthPercentage = 100 * this.CurrentHealth / (float)UnitData.Health;

            this.HealthBarRuntimeInstance.HealthPercentage = healthPercentage;

            const float offset = 6;

            this.HealthBarRuntimeInstance.X = screenX / zoom;
            this.HealthBarRuntimeInstance.Y = -offset + screenY / zoom;
        }

        private void TrainingActivity()
        {
            if(IsTrainingComplete)
            {
                CurrentTrainingStatusState = TrainingStatus.TrainingComplete;
            }
        }

        private void HighLevelActivity()
        {
            if(HighLevelGoal?.GetIfDone() == true)
            {
                HighLevelGoal = null;
            }
            HighLevelGoal?.DecideWhatToDo();

            if(HighLevelGoal == null)
            {
                TryStartFindingTarget();
            }
        }

        private void ImmediateAiActivity()
        {
            if(ImmediateGoal?.Path?.Count > 0)
            {
                MoveAlongPath();
            }
        }

        internal void CreatMoveGoal(float worldX, float worldY)
        {
            var goal = new WalkToHighLevelGoal();

            goal.Owner = this;
            goal.TargetPosition =
                new Microsoft.Xna.Framework.Vector3(worldX, worldY, 0);

            this.HighLevelGoal = goal;
            this.ImmediateGoal = null;
        }

        private void MoveAlongPath()
        {
            PositionedNode node = ImmediateGoal.Path[0];

            var amountMovedIn2Frames = UnitData.MovementSpeed * 2 / 60.0f;

            if ((Position - node.Position).Length() < amountMovedIn2Frames)
            {
                ImmediateGoal.Path.RemoveAt(0);

                if(ImmediateGoal.Path.Count == 0)
                {
                    ImmediateGoal.Path = null;
                    Velocity = Vector3.Zero;
                }

            }

            if(ImmediateGoal.Path != null)
            {
                var direction = node.Position - Position;
                direction.Normalize();

                direction.Z = 0;
                Velocity = direction * UnitData.MovementSpeed;
            }
        }

        public List<PositionedNode> GetPathTo(Vector3 position)
        {
            var toReturn = NodeNetwork.GetPath(ref Position, ref position);

            // remove node 0 if there's more than 1 node, because otherwise the user backtracks:
            if(toReturn.Count > 1)
            {
                toReturn.RemoveAt(0);
            }
            return toReturn;
        }

        internal void TryStartFindingTarget()
        {
            if(this.UnitData.InitiatesBattle)
            {
                var goal = new FindTargetToAttackHighLevelGoal();
                goal.Owner = this;
                goal.AllUnits = AllUnits;

                HighLevelGoal = goal;
            }
        }

        public void CreateAttackGoal(Unit enemy)
        {
            var attackGoal = new AttackUnitHighLevelGoal();
            attackGoal.TargetUnit = enemy;
            attackGoal.Owner = this;
            attackGoal.NodeNetwork = this.NodeNetwork;

            HighLevelGoal = attackGoal;
        }

        public void TakeDamage(int attackDamage)
        {
            CurrentHealth -= attackDamage;
            if(CurrentHealth <= 0)
            {
                Destroy();
            }
        }

        public void StartTraining()
        {
            m_TraningStartTime = ScreenManager.CurrentScreen.PauseAdjustedCurrentTime;
        }
        #endregion

        private void CustomDestroy()
		{


		}

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }
	}
}
