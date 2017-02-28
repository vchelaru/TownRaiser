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
using System.Linq;
using StateInterpolationPlugin;

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

        private int m_CurrentHealth;
        public int CurrentHealth
        {
            get
            {
                return m_CurrentHealth;
            }
            set
            {
                m_CurrentHealth = value;
                UpdateHealthSprite();
            }
        }


        #endregion

        #region Private Fields/Properties

#if DEBUG
        PositionedObjectList<Line> pathLines = new PositionedObjectList<Line>();
#endif

        // The last time damage was dealt. Damage is dealt one time every X seconds
        // as defined by the DamageFrequency value;
        private double lastDamageDealt;
        const float DamageFrequency = 1;

        #endregion

        #region Events

        public event Action Died;

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
            this.HealthBarRuntimeInstance.CurrentHealthStatusState = GumRuntimes.HealthBarRuntime.HealthStatus.Full;
            this.HealthBarRuntimeInstance.Z = -2;
#if DEBUG
            this.ResourceCollectCircleInstance.Visible = DebuggingVariables.ShowResourceCollision;
#endif
        }

        #endregion

        #region Activity

        private void CustomActivity()
        {
            HighLevelActivity();

            ImmediateAiActivity();

#if DEBUG
            DebugActivity();
#endif
        }

        private void DebugActivity()
        {
            if(DebuggingVariables.ShowUnitPaths)
            {
                int numberOfLinesNeeded = this.ImmediateGoal?.Path?.Count ?? 0;

                while(this.pathLines.Count < numberOfLinesNeeded)
                {
                    var line = new Line();
                    line.Visible = true;
                    pathLines.Add(line);
                }
                while (this.pathLines.Count > numberOfLinesNeeded)
                {
                    ShapeManager.Remove(pathLines.Last());
                }

                for (int i = 0; i < numberOfLinesNeeded; i++)
                {
                    Vector3 pointBefore;
                    if (i == 0)
                    {
                        pointBefore = this.Position;
                    }
                    else
                    {
                        pointBefore = ImmediateGoal.Path[i - 1].Position;
                    }
                    Vector3 pointAfter = ImmediateGoal.Path[i].Position;

                    pathLines[i].SetFromAbsoluteEndpoints(pointBefore, pointAfter);
                }

            }
        }

        private void HealthBarActivity()
        {
            HealthBarRuntimeInstance.PositionTo(this, -SpriteInstance.Height * .85f);

            var healthPercentage = 100 * GetHealthRatio();

            this.HealthBarRuntimeInstance.HealthPercentage = healthPercentage;
        }

        private float GetHealthRatio()
        {
            return this.CurrentHealth / (float)UnitData.Health;
        }

        private void HighLevelActivity()
        {
            var currentGoal = HighLevelGoals.Count == 0 ? null : HighLevelGoals.Peek();

            if(currentGoal?.GetIfDone() == true)
            {
                HighLevelGoals.Pop();
            }

            currentGoal = HighLevelGoals.Count == 0 ? null : HighLevelGoals.Peek();

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

        internal void AssignAttackThenRetreat(float worldX, float worldY, bool replace = true)
        {
            // we'll just make a circle:
            var circle = new Circle();
            circle.Radius = AttackThenRetreat.BuildingAttackingRadius; ;
            circle.X = worldX;
            circle.Y = worldY;

            var buildingsToTarget = AllBuildings
                .Where(item => item.CollideAgainst(circle))
                .Take(3)
                .ToList();

            // if there's no buildings, then just do a regular attack move:
            if(buildingsToTarget.Count == 0)
            {
                AssignMoveAttackGoal(worldX, worldY, replace);
            }
            else
            {
            var goal = new AttackThenRetreat();
            goal.StartX = this.X;
            goal.StartY = this.Y;

            goal.TargetX = worldX;
            goal.TargetY = worldY;

            goal.AllUnits = AllUnits;
            goal.BuildingsToFocusOn.AddRange( buildingsToTarget);
            goal.Owner = this;

            if (replace)
            {
                this.HighLevelGoals.Clear();
            }
            this.HighLevelGoals.Push(goal);
            this.ImmediateGoal = null;
        }
        }

        public void AssignMoveAttackGoal(float worldX, float worldY, bool replace = true)
        {
            var goal = new AttackMoveHighLevelGoal();
            goal.TargetX = worldX;
            goal.TargetY = worldY;
            goal.Owner = this;
            goal.AllUnits = AllUnits;
            goal.AllBuildings = AllBuildings;

            if (replace)
            {
                this.HighLevelGoals.Clear();
            }
            this.HighLevelGoals.Push(goal);
            this.ImmediateGoal = null;
        }

        public void AssignMoveGoal(float worldX, float worldY, bool replace = true)
        {
            var goal = new WalkToHighLevelGoal();

            goal.Owner = this;
            goal.TargetPosition =
                new Microsoft.Xna.Framework.Vector3(worldX, worldY, 0);

            if(replace)
            {
                this.HighLevelGoals.Clear();
            }
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

                SpriteInstance.FlipHorizontal = Velocity.X > 0;
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

        public void ToggleResourceIndicator(bool isEnabled, Screens.ResourceType resourceType)
        {
            ResourceIndicatorSpriteInstance.Visible = isEnabled;
            string resourceAnimationChainName;
            switch (resourceType) {
                case Screens.ResourceType.Lumber:
                    resourceAnimationChainName = "ResourceLumber";
                    break;
                case Screens.ResourceType.Stone:
                    resourceAnimationChainName = "ResourceStone";
                    break;
                default:
                //case Screens.ResourceType.Gold:
                    resourceAnimationChainName = "ResourceGold";
                    break;
            }
            ResourceIndicatorSpriteInstance.CurrentChainName = resourceAnimationChainName;
        }

        public void AssignResourceCollectGoal(Vector3 clickPosition, AxisAlignedRectangle resourceGroupTile, Screens.ResourceType resourceType)
        {
            var collectResourceGoal = new ResourceCollectHighLevelGoal(
                owner: this,
                nodeNetwork: NodeNetwork,
                clickPosition: clickPosition,
                targetResourceTile: resourceGroupTile,
                targetResourceType: resourceType,
                allBuildings: AllBuildings
            );
            if (ImmediateGoal?.Path != null)
            {
                ImmediateGoal.Path.Clear();
            }
            HighLevelGoals.Clear();
            HighLevelGoals.Push(collectResourceGoal);
        }
        public void AssignAttackGoal(Unit enemy, bool replace = true)
        {
            var attackGoal = new AttackUnitHighLevelGoal();
            attackGoal.TargetUnit = enemy;
            attackGoal.Owner = this;
            attackGoal.NodeNetwork = this.NodeNetwork;

            if(replace)
            {
                HighLevelGoals.Clear();
            }
            HighLevelGoals.Push(attackGoal);
        }

        public void AssignAttackGoal(Building building, bool replace = true)
        {
            var attackGoal = new AttackBuildingHighLevelGoal();
            attackGoal.TargetBuilding = building;
            attackGoal.Owner = this;
            attackGoal.NodeNetwork = this.NodeNetwork;

            if(replace)
            {
                HighLevelGoals.Clear();
            }
            HighLevelGoals.Push(attackGoal);
        }

        public void TakeDamage(int attackDamage)
        {
            CurrentHealth -= attackDamage;
            if(CurrentHealth <= 0)
            {
                PerformDeath();
                Died?.Invoke();
            }
        }

        private void PerformDeath()
        {
            TryPlayDeathSound(this);
            CombatTracker.RemoveUnit(this);
            if (UnitData.IsEnemy == false)
            {
                var screen = ScreenManager.CurrentScreen as Screens.GameScreen;
                screen.CurrentCapacityUsed -= UnitData.Capacity;
                screen.UpdateResourceDisplay();
            }
            Destroy();
        }

        public void TryAttack(Unit targetUnit)
        {
            var screen = FlatRedBall.Screens.ScreenManager.CurrentScreen;
            bool canAttack = screen.PauseAdjustedSecondsSince(lastDamageDealt) >= DamageFrequency;

            if (canAttack)
            {
                CombatTracker.RegisterUnitForCombat(this);
                CombatTracker.RegisterUnitForCombat(targetUnit);

                lastDamageDealt = screen.PauseAdjustedCurrentTime;

                targetUnit.TakeDamage(UnitData.AttackDamage);
                TryPlayAttackSound(this);
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

                TryPlayAttackSound(this);
            }
        }

        public override void UpdateDependencies(double currentTime)
        {
            base.UpdateDependencies(currentTime);

            HealthBarActivity();

        }

        public bool IsInCameraBounds()
        {
            float left = Camera.Main.AbsoluteLeftXEdgeAt(Z);
            float right = Camera.Main.AbsoluteRightXEdgeAt(Z);
            float top = Camera.Main.AbsoluteTopYEdgeAt(Z);
            float bottom = Camera.Main.AbsoluteBottomYEdgeAt(Z);

            return left < X && right > X && bottom < Y && top > Y;
        }

        #endregion

        public void UpdateHealthSprite()
        {
            var healthRatio = GetHealthRatio();

            if (healthRatio > .66)
            {
                this.HealthBarRuntimeInstance.CurrentHealthStatusState = GumRuntimes.HealthBarRuntime.HealthStatus.Full;
            }
            else if (healthRatio > .33)
            {
                this.HealthBarRuntimeInstance.CurrentHealthStatusState = GumRuntimes.HealthBarRuntime.HealthStatus.TwoThird;
            }
            else
            {
                this.HealthBarRuntimeInstance.CurrentHealthStatusState = GumRuntimes.HealthBarRuntime.HealthStatus.OneThird;
            }

        }
        private void CustomDestroy()
		{
            while (this.pathLines.Count > 0)
            {
                ShapeManager.Remove(pathLines.Last());
            }
        }

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }
	}
}
