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
        private const int PixelsBetweenButtons = 2;

        public List<ToggleButtonRuntime> ToggleButtonList;
        public event EventHandler TrainUnit;

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
                building.Parent = this;
                ToggleButtonList.Add(building);

                //building.X = i % 3 != 0 ? PixelsBetweenButtons : 0;
                //building.Y = i > 2 && i % 3 == 0 ? PixelsBetweenButtons : 0;

                building.HotkeyData = buildingData.Value;
                building.IsOn = false;

                building.Click += (notused) =>
                {
                    UntoggleAllExcept(building);
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
                bool shouldAddButton = unitData.Value.IsEnemy == false;
#if DEBUG
                shouldAddButton |= Entities.DebuggingVariables.ShouldAddEnemiesToActionToolbar;
#endif
                if (shouldAddButton)
                {
                    ToggleButtonRuntime unit = new ToggleButtonRuntime();
                    unit.Parent = this;
                    ToggleButtonList.Add(unit);

                    unit.X = i < 0 && i % 2 == 0 ? PixelsBetweenButtons : 0;
                    unit.Y = i % 2 == 1 ? PixelsBetweenButtons : 0;

                    unit.HotkeyData = unitData.Value;
                    unit.Click += (notused) =>
                    {
                        UntoggleAllExcept(unit);
                    };

                    i++;
                }
            }

            SetVariableState();
        }

        public void RefreshToggleButtonsTo(IEnumerable<string> units)
        {
            RemoveToggleButtons();

            if(units != null)
            {

                int i = 0;
                foreach (var unit in units)
                {
                    ToggleButtonRuntime unitButton = new ToggleButtonRuntime();
                    unitButton.Parent = this;
                    ToggleButtonList.Add(unitButton);

                    unitButton.X = i < 0 && i % 3 == 0 ? PixelsBetweenButtons : 0;
                    unitButton.Y = i % 2 == 1 ? PixelsBetweenButtons : 0;

                    unitButton.HotkeyData = GlobalContent.UnitData[unit];
                
                    unitButton.Click += (notused) =>
                    {
                        unitButton.IsOn = false;
                        this.TrainUnit(unitButton.HotKeyDataAsUnitData, null);
                    };
                    unitButton.Push += (notused) =>
                    {
                        unitButton.IsOn = true;
                    };
                    unitButton.RollOff += (notused) =>
                    {
                        unitButton.IsOn = false;
                    };

                    i++;
                }
            }
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
