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
namespace TownRaiser.Screens
{
    public partial class GameScreen
    {
        void OnMinimapButtonInstanceClickTunnel (FlatRedBall.Gui.IWindow window) 
        {
            if (this.MinimapButtonInstanceClick != null)
            {
                MinimapButtonInstanceClick(window);
            }
        }
    }
}
