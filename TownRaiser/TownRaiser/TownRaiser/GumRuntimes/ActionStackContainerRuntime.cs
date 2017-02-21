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

        public List<IconButtonRuntime> ToggleButtonList;
        public event EventHandler<TrainUnitEventArgs> TrainUnit;
        public event EventHandler<UpdateUiEventArgs> UpdateUIDisplay;

        public event EventHandler<ConstructBuildingEventArgs> SelectBuildingToConstruct;

        partial void CustomInitialize()
        {
            ToggleButtonList = new List<IconButtonRuntime>();
        }

        private IconButtonRuntime CreateNewToggleButtonWithOffset(int stackIndex, IHotkeyData data)
        {
            IconButtonRuntime button = new IconButtonRuntime();
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

            return button;
        }

        public void AddBuildingToggleButtons()
        {
            int i = 0;
            foreach (var buildingData in GlobalContent.BuildingData)
            {
                IconButtonRuntime building = CreateNewToggleButtonWithOffset(i, buildingData.Value);

                building.Click += (notused) =>
                {
                    if (building.Enabled)
                    {
                        this.SelectBuildingToConstruct?.Invoke(building, new ConstructBuildingEventArgs { BuildingData = buildingData.Value });
                        RemoveToggleButtons();
                    }
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
                    IconButtonRuntime unit = CreateNewToggleButtonWithOffset(i, unitData.Value);

                    unit.Click += (notused) =>
                    {
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
                    IconButtonRuntime unitButton = CreateNewToggleButtonWithOffset(i, unitData);

                    unitButton.HotkeyData = unitData;
                
                    unitButton.Click += (notused) =>
                    {
                        this.TrainUnit(unitButton.HotKeyDataAsUnitData, null);
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
    }
}
