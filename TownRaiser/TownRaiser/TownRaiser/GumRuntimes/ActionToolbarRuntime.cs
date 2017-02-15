using Gum.Wireframe;
using FlatRedBall.Input;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.DataTypes;
using TownRaiser.Screens;

namespace TownRaiser.GumRuntimes
{
    public partial class ActionToolbarRuntime
    {
        public UnitData SelectedUnitData
        {
            get
            {
                UnitData toReturn = null;
                foreach(var button in ActionStackContainerInstance.ToggleButtonList)
                {
                    if(button.IsOn)
                    {
                        toReturn = button.HotKeyDataAsUnitData;
                        break;
                    }
                }
                return toReturn;
            }
        }

        public BuildingData SelectedBuildingData
        {
            get
            {
                BuildingData toReturn = null;
                foreach (var button in ActionStackContainerInstance.ToggleButtonList)
                {
                    if (button.IsOn)
                    {
                        toReturn = button.HotKeyDataAsBuildingData;
                        break;
                    }
                }
                return toReturn;
            }
        }

        public ActionMode GetActionModeBasedOnToggleState()
        {
            //Stay in select mode until we have selected a toogle button attatched to unit or building data.
            bool anySubButtonSelected = ActionStackContainerInstance.AnyToggleButtonsActivated;

            if (TrainButtonInstance.IsOn && anySubButtonSelected) return ActionMode.Train;
            else if (BuildButtonInstance.IsOn && anySubButtonSelected) return ActionMode.Build;
            else return ActionMode.Select;
        }

        partial void CustomInitialize()
        {
            this.TrainButtonInstance.Click += (notused) =>
            {
                ShowAvailableUnits();
            };
            this.BuildButtonInstance.Click += (notused) =>
            {
                ShowAvailableBuildings();
            };

        }

        private void ShowAvailableBuildings()
        {
            BuildButtonInstance.IsOn = true;
            UntoggleAllExcept(ActionMode.Build);
            AddBuildingOptionsToActionPanel();
        }

        private void ShowAvailableUnits()
        {
            TrainButtonInstance.IsOn = true;
            UntoggleAllExcept(ActionMode.Train);
            AddUnitOptionsToActionPanel();
        }

        private void UntoggleAllExcept(ActionMode actionMode)
        {
            if(actionMode != ActionMode.Build)
            {
                //Clear the list of build buttons before adding train buttons
                if (BuildButtonInstance.IsOn)
                {
                    RemoveStackContainerOptions();
                }
                BuildButtonInstance.IsOn = false;
            }
            if(actionMode != ActionMode.Train)
            {
                //Clear the list of build buttons before adding train buttons
                if(TrainButtonInstance.IsOn)
                {
                    RemoveStackContainerOptions();
                }
                TrainButtonInstance.IsOn = false;
            }
            if(actionMode == ActionMode.Select)
            {
                RemoveStackContainerOptions();
            }
        }

        internal void SetMode(ActionMode actionMode)
        {
            UntoggleAllExcept(actionMode);
        }
        private void RemoveStackContainerOptions()
        {
            ActionStackContainerInstance.RemoveToggleButtons();
        }

        private void AddBuildingOptionsToActionPanel()
        {
            bool addButtons = ActionStackContainerInstance.ToggleButtonList.Count == 0;
#if DEBUG
            addButtons &= Entities.DebuggingVariables.DoNotAddActionPanelButtons == false;
#endif
            if (addButtons)
            {
                ActionStackContainerInstance.AddBuildingToggleButtons();
            }
        }

        private void AddUnitOptionsToActionPanel()
        {
            bool addButtons = ActionStackContainerInstance.ToggleButtonList.Count == 0;
#if DEBUG
            addButtons &= Entities.DebuggingVariables.DoNotAddActionPanelButtons == false;
#endif
            if (addButtons)
            {
                ActionStackContainerInstance.AddUnitToggleButtons();
            }
        }

        public void ReactToKeyPress()
        {
            if(InputManager.Keyboard.KeyPushed(Keys.Escape)) //The escape case depends on the currently selected default button and if a sub button is selected.
            {
                if(ActionStackContainerInstance.AnyToggleButtonsActivated)
                {
                    ActionStackContainerInstance.UntoggleAllExcept(null);
                }
                else
                {
                    UntoggleAllExcept(ActionMode.Select);
                }

            }
            else if(InputManager.Keyboard.KeyPushed(Keys.T))
            {
                if(TrainButtonInstance.IsOn == false)
                {
                    ShowAvailableUnits();
                }
            }
            else if(InputManager.Keyboard.KeyPushed(Keys.B))
            {
                if(BuildButtonInstance.IsOn == false)
                {
                    ShowAvailableBuildings();
                }
            }
            else
            {
                foreach(var button in ActionStackContainerInstance.ToggleButtonList)
                {
                    var hotKey = button.HotkeyData.Hotkey;
                    if(InputManager.Keyboard.KeyPushed(hotKey))
                    {
                        ActionStackContainerInstance.UntoggleAllExcept(button);
                    }
                }
            }
        }

        public void UpdateButtonsOnMoney(int lumber, int stone, int gold, int currentCapacity, int maxCapacity)
        {
            foreach (var button in ActionStackContainerInstance.ToggleButtonList)
            {
                button.UpdateButtonBasedOnMoney(lumber, stone, gold, currentCapacity, maxCapacity);
            }
        }
    }
}
