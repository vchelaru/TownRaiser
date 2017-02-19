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

        public Stack<HighLevelGoal> HighLevelGoals { get; set; } = new Stack<HighLevelGoal>();

        public TileNodeNetwork NodeNetwork { get; set; }

        public PositionedObjectList<Unit> AllUnits { get; set; }

        public PositionedObjectList<Building> AllBuildings { get; set; }

        public int CurrentHealth { get; set; }

        #endregion

        #region Private Fields/Properties
        

        // The last time damage was dealt. Damage is dealt one time every X seconds
        // as defined by the DamageFrequency value;
        private double lastDamageDealt;
        const float DamageFrequency = 1;

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

        }

        #endregion

        #region Activity

        private void CustomActivity()
        {
            HighLevelActivity();
            ImmediateAiActivity();

            HealthBarActivity();
        }
        private void HealthBarActivity()
        {
            HealthBarRuntimeInstance.PositionTo(this, -6);

            var healthPercentage = 100 * this.CurrentHealth / (float)UnitData.Health;

            this.HealthBarRuntimeInstance.HealthPercentage = healthPercentage;
        }

        private void HighLevelActivity()
        {
            var currentGoal = HighLevelGoals.Peek();

            if(currentGoal?.GetIfDone() == true)
            {
                HighLevelGoals.Pop();
            }

            currentGoal = HighLevelGoals.Peek();

            currentGoal?.DecideWhatToDo();

            if(currentGoal == null)
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

            this.HighLevelGoals.Clear();
            this.HighLevelGoals.Push(goal);
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
                goal.AllBuildings = AllBuildings;

                HighLevelGoals.Clear();
                HighLevelGoals.Push(goal);
            }
        }

        public void AssignResourceCollectGoal(Vector3 clickPosition, AxisAlignedRectangle resourceGroupTile, string resourceType)
        {
            var collectResourceGoal = new ResourceCollectHighLevelGoal();
            collectResourceGoal.Owner = this;
            collectResourceGoal.NodeNetwork = this.NodeNetwork;
            collectResourceGoal.ClickPosition = clickPosition;
            collectResourceGoal.TargetResourceTile = resourceGroupTile;
            collectResourceGoal.TargetResourceType = resourceType;

            HighLevelGoal = collectResourceGoal;
        }

        public void AssignAttackGoal(Unit enemy)
        {
            var attackGoal = new AttackUnitHighLevelGoal();
            attackGoal.TargetUnit = enemy;
            attackGoal.Owner = this;
            attackGoal.NodeNetwork = this.NodeNetwork;

            HighLevelGoals.Clear();
            HighLevelGoals.Push(attackGoal);
        }

        public void AssignAttackGoal(Building building)
        {
            var attackGoal = new AttackBuildingHighLevelGoal();
            attackGoal.TargetBuilding = building;
            attackGoal.Owner = this;
            attackGoal.NodeNetwork = this.NodeNetwork;

            HighLevelGoals.Clear();
            HighLevelGoals.Push(attackGoal);
        }

        public void TakeDamage(int attackDamage)
        {
            CurrentHealth -= attackDamage;
            if(CurrentHealth <= 0)
            {
                Destroy();
            }
        }

        public void TryAttack(Unit targetUnit)
        {
            var screen = FlatRedBall.Screens.ScreenManager.CurrentScreen;
            bool canAttack = screen.PauseAdjustedSecondsSince(lastDamageDealt) >= DamageFrequency;

            if (canAttack)
            {
                lastDamageDealt = screen.PauseAdjustedCurrentTime;

                targetUnit.TakeDamage(UnitData.AttackDamage);
            }
        }


        public void TryAttack(Building targetBuilding)
        {
            var screen = FlatRedBall.Screens.ScreenManager.CurrentScreen;
            bool canAttack = screen.PauseAdjustedSecondsSince(lastDamageDealt) >= DamageFrequency;

            if (canAttack)
            {
                lastDamageDealt = screen.PauseAdjustedCurrentTime;

                targetBuilding.TakeDamage(UnitData.AttackDamage);
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
