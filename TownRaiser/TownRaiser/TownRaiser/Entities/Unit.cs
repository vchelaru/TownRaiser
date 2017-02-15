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

        public int CurrentHealth { get; set; }

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
        }

        private void ImmediateAiActivity()
        {
            if(ImmediateGoal != null)
            {
                if(ImmediateGoal.TargetPosition != null)
                {
                    GetPath();
                }
                else if(ImmediateGoal.Path?.Count > 0)
                {
                    MoveAlongPath();
                }
            }
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

        private void GetPath()
        {
            var vector3 = ImmediateGoal.TargetPosition.Value;
            ImmediateGoal.Path = NodeNetwork.GetPath(ref this.Position, ref vector3);
            ImmediateGoal.TargetPosition = null;
            
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
