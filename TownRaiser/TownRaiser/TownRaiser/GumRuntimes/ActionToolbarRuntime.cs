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
using TownRaiser.Entities;

namespace TownRaiser.GumRuntimes
{
    public partial class ActionToolbarRuntime
    {
        public event EventHandler TrainUnit;
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


            //if (this.CurrentVariableState == VariableState.BuildMenuSelected) return ActionMode.Build;
            ////else if (BuildButtonInstance.IsOn && anySubButtonSelected) return ActionMode.Build;
            //else return ActionMode.Select;
            return ActionMode.Select;
        }

        partial void CustomInitialize()
        {

            this.BuildMenuToggleButtonInstance.Click += (notused) =>
            {
                this.ShowAvailableBuildings();
            };
            this.ActionStackContainerInstance.TrainUnit += (unitData, notused) =>
            {
                this.TrainUnit(unitData, notused);
            };
            this.XButtonInstance.Click += (notused) =>
            {
                this.PerformCancelStep();
            };
            this.SetVariableState(VariableState.BuildMenuNotSelected);

        }

        private void SetVariableState(VariableState state)
        {
            if(state == VariableState.BuildMenuNotSelected)
            {
                this.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
                this.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
                this.Width = 32;
                this.Height = 32;
            }
            else if(state == VariableState.BuildMenuSelected)
            {
                this.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
                this.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
                this.Width = 8;
                this.Height = 8;
            }
            this.CurrentVariableState = state;
        }
        private void ShowAvailableBuildings()
        {
            UntoggleAllExcept(ActionMode.Build);
            AddBuildingOptionsToActionPanel();
        }

        private void ShowAvailableUnits()
        {
            UntoggleAllExcept(ActionMode.Train);
            AddUnitOptionsToActionPanel();
        }

        public void ShowAvailableUnits(IEnumerable<string> units)
        {
            UntoggleAllExcept(ActionMode.Train);
            AddUnitOptionsToActionPanel(units);
        }

        private void UntoggleAllExcept(ActionMode actionMode)
        {
            if(actionMode != ActionMode.Build)
            {
                //Clear the list of build buttons before adding train buttons
                if (this.CurrentVariableState == VariableState.BuildMenuSelected)
                {
                    RemoveStackContainerOptions();
                }
            }
            if(actionMode != ActionMode.Train)
            {
                //Clear the list of build buttons before adding train buttons
                //if(TrainButtonInstance.IsOn)
                //{
                //    RemoveStackContainerOptions();
                //}
                //TrainButtonInstance.IsOn = false;
            }
            if(actionMode == ActionMode.Select)
            {
                RemoveStackContainerOptions();
                this.CurrentVariableState = VariableState.BuildMenuNotSelected;
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
                this.SetVariableState(VariableState.BuildMenuSelected);

                ActionStackContainerInstance.AddBuildingToggleButtons();
            }
        }

        private void AddUnitOptionsToActionPanel(IEnumerable<string> units = null)
        {
            ActionStackContainerInstance.RefreshToggleButtonsTo(units);
        }

        public void ReactToKeyPress()
        {
            if(InputManager.Keyboard.KeyPushed(Keys.Escape)) //The escape case depends on the currently selected default button and if a sub button is selected.
            {
                PerformCancelStep();

            }
            else if(InputManager.Keyboard.KeyPushed(Keys.B))
            {
                if(this.CurrentVariableState == VariableState.BuildMenuNotSelected)
                {
                    ShowAvailableBuildings();
                }
            }
            else
            {
                foreach(var button in ActionStackContainerInstance.ToggleButtonList)
                {
                    var hotKey = button.HotkeyData.Hotkey;
                    if(InputManager.Keyboard.KeyPushed(hotKey) && button.HotKeyDataAsUnitData != null)
                    {
                        //ActionStackContainerInstance.UntoggleAllExcept(button);
                        this.TrainUnit(button.HotKeyDataAsUnitData, null);
                    }
                }
            }
        }

        private void PerformCancelStep()
        {
            if (ActionStackContainerInstance.AnyToggleButtonsActivated)
            {
                ActionStackContainerInstance.UntoggleAllExcept(null);
            }
            else
            {
                UntoggleAllExcept(ActionMode.Select);
            }
            //ToDo: Rick, escape case properly
            this.SetVariableState(VariableState.BuildMenuNotSelected);

        }

        public void UpdateButtonEnabledStates(int lumber, int stone, int gold, int currentCapacity, int maxCapacity, IEnumerable<Building> existingBuildings)
        {
            foreach (var button in ActionStackContainerInstance.ToggleButtonList)
            {
                button.UpdateButtonEnabledState(lumber, stone, gold, currentCapacity, maxCapacity, existingBuildings);
            }
        }
    }
}
