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
            ImmediateAiActivity();

		}

        private void ImmediateAiActivity()
        {
            if(ImmediateGoal != null)
            {
                if(ImmediateGoal.TargetPosition != null)
                {
                    MoveTowardTargetPosition();
                }
            }
        }

        private void MoveTowardTargetPosition()
        {
            // need to obtain a path, but for now we'll just move directly
            var direction = ImmediateGoal.TargetPosition.Value - this.Position;
            direction.Z = 0;

            const float epsilonSquared = 1 * 1;
            bool hasArrived = direction.LengthSquared() < epsilonSquared;

            if (hasArrived)
            {
                Velocity = Vector3.Zero;
            }
            else
            {
                direction.Normalize();
                this.Velocity = direction * this.UnitData.MovementSpeed;
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
