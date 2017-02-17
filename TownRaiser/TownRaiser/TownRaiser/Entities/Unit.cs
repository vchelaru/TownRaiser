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

        public Vector3? RallyPoint { get; set; }

        #endregion

        #region Initialize

        /// <summary>
        /// Initialization logic which is execute only one time for this Entity (unless the Entity is pooled).
        /// This method is called when the Entity is added to managers. Entities which are instantiated but not
        /// added to managers will not have this method called.
        /// </summary>
        private void CustomInitialize()
		{


		}

        #endregion

        #region Activity

        private void CustomActivity()
        {
            HighLevelActivity();

            ImmediateAiActivity();
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

        private void CreateMoveGoalFromCurrentRallyPoint()
        {
            if(RallyPoint.HasValue)
            {
                var x = RallyPoint.Value.X;
                var y = RallyPoint.Value.Y;
                CreatMoveGoal(x, y);
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

        #endregion

        private void CustomDestroy()
		{


		}

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }
	}
}
