using System;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Specialized;
using FlatRedBall.Audio;
using FlatRedBall.Screens;
using TownRaiser.Entities;
using TownRaiser.Screens;
namespace TownRaiser.Entities
{
	public partial class Unit
	{
        void OnAfterUnitDataSet (object sender, EventArgs e)
        {
            var animationName = this.UnitData.Name;
            
            this.SpriteInstance.CurrentChainName = animationName;
            this.CurrentHealth = this.UnitData.Health;
        }
		
	}
}
