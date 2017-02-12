﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.Interfaces;

namespace TownRaiser.GumRuntimes
{
    public partial class ActionStackContainerRuntime
    {
        public event EventHandler ModeChanged;

        private const int PixelsBetweenButtons = 2;

        public List<ToggleButtonRuntime> ToggleButtonList;
        
        public bool AnyToggleButtonsActivated
        {
            get
            {
                bool toReturn = false;

                foreach(var button in ToggleButtonList)
                {
                    if(button.IsOn)
                    {
                        toReturn = true;
                        break;
                    }
                }

                return toReturn;
            }
        }

        partial void CustomInitialize()
        {
            ToggleButtonList = new List<ToggleButtonRuntime>();
        }

        public void AddBuildingToggleButtons()
        {
            int i = 0;
            foreach (var buildingData in GlobalContent.BuildingData)
            {
                ToggleButtonRuntime building = new ToggleButtonRuntime();
                building.AddToManagers(this.Managers, null);
                building.Parent = this;
                Children.Add(building);
                ToggleButtonList.Add(building);

                building.X = i < 0 && i % 2 == 0 ? PixelsBetweenButtons : 0;
                building.Y = i % 2 == 1 ? PixelsBetweenButtons : 0;

                building.HotkeyData = buildingData.Value;
                building.Click += (notused) =>
                {
                    UntoggleAllExcept(building);
                    this.ModeChanged(this, null);
                };

                i++;
            }

            SetVariableState();
        }

        public void AddUnitToggleButtons()
        {
            int i = 0;
            foreach (var unitData in GlobalContent.UnitData)
            {
                ToggleButtonRuntime unit = new ToggleButtonRuntime();
                unit.AddToManagers(this.Managers, null);
                unit.Parent = this;
                Children.Add(unit);
                ToggleButtonList.Add(unit);

                unit.X = i < 0 && i % 2 == 0 ? PixelsBetweenButtons : 0;
                unit.Y = i % 2 == 1 ? PixelsBetweenButtons : 0;

                unit.HotkeyData = unitData.Value;
                unit.Click += (notused) =>
                {
                    UntoggleAllExcept(unit);
                    this.ModeChanged(this, null);
                };

                i++;
            }

            SetVariableState();
        }

        public void RemoveToggleButtons()
        {
            for(int i = ToggleButtonList.Count -1; i > -1; i--)
            {
                var toggleButton = ToggleButtonList[i];

                Children.Remove(toggleButton);
                ToggleButtonList.Remove(toggleButton);

                toggleButton.Destroy();
                toggleButton = null;
            }

            SetVariableState();
        }

        private void SetVariableState()
        {
            CurrentVariableState = ToggleButtonList.Count > 0 ? VariableState.NotEmpty : VariableState.Empty;
        } 
        
        public void UntoggleAllExcept(ToggleButtonRuntime buttonToActivate)
        {
            if (buttonToActivate != null)
            {
                buttonToActivate.IsOn = true;
            }

            foreach(var button in ToggleButtonList)
            {
                if(button != buttonToActivate)
                {
                    button.IsOn = false;
                }
            }
        }
    }
}
