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
using FlatRedBall.Math;
using FlatRedBall.Screens;
using System.Linq;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

#endif
#endregion

namespace TownRaiser.Entities
{
	public partial class EncounterSpawnPoint
	{
        #region Enums

        public enum LogicState
        {
            ActiveWaiting,
            Spawned,
            ReturningUnits,
            Dormant
        }

        #endregion

        #region Fields/Properties

        double lastTimeDestroyed;

        PositionedObjectList<Unit> UnitsCreatedByThis = new PositionedObjectList<Unit>();

        public LogicState CurrentLogicState { get; set; }

        #endregion

        /// <summary>
        /// Initialization logic which is execute only one time for this Entity (unless the Entity is pooled).
        /// This method is called when the Entity is added to managers. Entities which are instantiated but not
        /// added to managers will not have this method called.
        /// </summary>
        private void CustomInitialize()
		{

        }

		private void CustomActivity()
		{
            bool shouldReactivate = this.CurrentLogicState == LogicState.Dormant &&
                ScreenManager.CurrentScreen.PauseAdjustedSecondsSince(lastTimeDestroyed) > RegenerationTime;

            if(shouldReactivate)
            {
                this.CurrentLogicState = LogicState.ActiveWaiting;
            }

        }

		private void CustomDestroy()
		{


		}

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }

        internal void ReturnSpawnedUnits()
        {

        }

        internal void Attack(Unit playerUnit, Func<string, Vector3, Unit> spawnAction)
        {
            if(CurrentLogicState == LogicState.ReturningUnits)
            {
                // units already exist, so attack:
                foreach(var unit in UnitsCreatedByThis)
                {
                    unit.AssignMoveAttackGoal(playerUnit.X, playerUnit.Y);
                }
            }
            else
            {
                CreateAllNewUnits(spawnAction);

                foreach (var unit in UnitsCreatedByThis)
                {
                    unit.AssignMoveAttackGoal(playerUnit.X, playerUnit.Y);
                }
            }

            this.CurrentLogicState = LogicState.Spawned;
        }

        private void CreateAllNewUnits(Func<string, Vector3, Unit> spawnAction)
        {
            var data = GlobalContent.EncounterPointData.FirstOrDefault(item => item.Difficulty == this.Difficulty);

            foreach(var enemyName in data.Enemies)
            {
                var unit = spawnAction(enemyName, this.Position);

                unit.Died += HandleUnitDied;

                this.UnitsCreatedByThis.Add(unit);
            }
        }

        private void HandleUnitDied()
        {
            if(this.UnitsCreatedByThis.Count == 0)
            {
                HandleAllUnitsKilled();
            }
        }

        private void HandleAllUnitsKilled()
        {
            lastTimeDestroyed = ScreenManager.CurrentScreen.PauseAdjustedCurrentTime;
            this.CurrentLogicState = LogicState.Dormant;
        }
    }
}
