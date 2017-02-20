using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.CustomEvents;
using TownRaiser.Interfaces;

namespace TownRaiser.GumRuntimes
{
    public partial class ActionStackContainerRuntime
    {
        private const int PixelsBetweenButtons = 2;

        public List<ToggleButtonRuntime> ToggleButtonList;
        public event EventHandler TrainUnit;
        public event EventHandler<UpdateUiEventArgs> UpdateUIDisplay;

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

        private ToggleButtonRuntime CreateNewToggleButtonWithOffset(int stackIndex, IHotkeyData data)
        {
            ToggleButtonRuntime button = new ToggleButtonRuntime();
            button.Parent = this;
            ToggleButtonList.Add(button);

            button.HotkeyData = data;

            button.X = stackIndex % 3 != 0 ? PixelsBetweenButtons : 0;
            button.Y = stackIndex > 2 && stackIndex % 3 == 0 ? PixelsBetweenButtons : 0;
            button.RollOn += (notused) =>
            {
                UpdateUIDisplay?.Invoke(this, new UpdateUiEventArgs(data));
            };
            button.RollOff += (notused) =>
            {
                UpdateUIDisplay?.Invoke(this, UpdateUiEventArgs.RollOffValue);
            };

            button.IsOn = false;

            return button;
        }

        public void AddBuildingToggleButtons()
        {
            int i = 0;
            foreach (var buildingData in GlobalContent.BuildingData)
            {
                ToggleButtonRuntime building = CreateNewToggleButtonWithOffset(i, buildingData.Value);

                building.Click += (notused) =>
                {
                    UntoggleAllExcept(building);
                };

                i++;
            }

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
                    ToggleButtonRuntime unit = CreateNewToggleButtonWithOffset(i, unitData.Value);

                    unit.Click += (notused) =>
                    {
                        UntoggleAllExcept(unit);
                    };
                    

                    i++;
                }
            }
        }

        public void RefreshToggleButtonsTo(IEnumerable<string> units)
        {
            RemoveToggleButtons();

            if(units != null)
            {

                int i = 0;
                foreach (var unit in units)
                {
                    var unitData = GlobalContent.UnitData[unit];
                    ToggleButtonRuntime unitButton = CreateNewToggleButtonWithOffset(i, unitData);

                    unitButton.HotkeyData = unitData;
                
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
