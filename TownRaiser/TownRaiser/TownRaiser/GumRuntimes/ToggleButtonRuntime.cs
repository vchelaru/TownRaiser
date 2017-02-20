﻿using FlatRedBall.Graphics.Animation;
using FlatRedBall.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.DataTypes;
using TownRaiser.Entities;
using TownRaiser.Interfaces;

namespace TownRaiser.GumRuntimes
{
    public partial class ToggleButtonRuntime
    {
        private IHotkeyData m_HotKeyData;
        public IHotkeyData HotkeyData
        {
            get
            {
                return m_HotKeyData;
            }
            set
            {
                if(value == null)
                {
                    throw new Exception("Something went wrong. Trying to set hotkey data with null value.");
                }
                if(value != null)
                {
                    m_HotKeyData = value;
                    SetIconFrom(GlobalContent.GumAnimationChains[m_HotKeyData.ChainName]);
                }
            }
        }

        public BuildingData HotKeyDataAsBuildingData => m_HotKeyData as BuildingData;
        public UnitData HotKeyDataAsUnitData => m_HotKeyData as UnitData; 
        
        public void SetIconFrom(AnimationChain animationChain)
        {
            // this assumes the chain only has 1 frame so we grab that frame and set it on the sprite:
            var frame = animationChain[0];
            var textureWidth = frame.Texture.Width;
            var textureHeight = frame.Texture.Height;

            this.SpriteInstance.TextureLeft = MathFunctions.RoundToInt( frame.LeftCoordinate * (float)textureWidth);
            this.SpriteInstance.TextureTop = MathFunctions.RoundToInt(frame.TopCoordinate * (float)textureHeight);
            this.SpriteInstance.TextureWidth = MathFunctions.RoundToInt((frame.RightCoordinate - frame.LeftCoordinate) * (float)textureWidth);
            this.SpriteInstance.TextureHeight = MathFunctions.RoundToInt((frame.BottomCoordinate - frame.TopCoordinate) * (float)textureHeight);
        }

        public void UpdateButtonEnabledState(int lumber, int stone, int gold, int currentCapacity, int maxCapacity, IEnumerable<Building> existingBuildings)
        {
            var isEnabled = m_HotKeyData.ShouldEnableButton(lumber, stone, gold, currentCapacity, maxCapacity, existingBuildings);

#if DEBUG
            if(Entities.DebuggingVariables.HasInfiniteResources)
            {
                isEnabled = true;
            }
#endif

            Enabled = isEnabled;


            //Switch off if the button is disabled.
            if(Enabled == false)
            {
                IsOn = false;
            }
        }
    }
}
