using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.Screens;

namespace TownRaiser.GumRuntimes
{
    public partial class ActionToolbarRuntime
    {
        public event EventHandler ModeChanged;

        public ActionMode GetActionModeBasedOnToggleState()
        {
            if (TrainButtonInstance.IsOn) return ActionMode.Train;
            else if (BuildButtonInstance.IsOn) return ActionMode.Build;
            else return ActionMode.Select;
        }

        partial void CustomInitialize()
        {
            this.TrainButtonInstance.Click += (notused) =>
            {
                AddUnitOptionsToActionPanel();
                UntoggleAllExcept(ActionMode.Train);
                this.ModeChanged(this, null);
            };
            this.BuildButtonInstance.Click += (notused) =>
            {
                AddBuildingOptionsToActionPanel();
                UntoggleAllExcept(ActionMode.Build);
                this.ModeChanged(this, null);
            };
        }

        private void UntoggleAllExcept(ActionMode actionMode)
        {
            if(actionMode != ActionMode.Build)
            {
                BuildButtonInstance.IsOn = false;
            }
            if(actionMode != ActionMode.Train)
            {
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
            bool addButtons = true;
#if DEBUG
            addButtons = Entities.DebuggingVariables.DoNotAddActionPanelButtons == false;
#endif
            if (addButtons)
            {
                ActionStackContainerInstance.AddBuildingToggleButtons();
            }
        }

        private void AddUnitOptionsToActionPanel()
        {
            bool addButtons = true;
#if DEBUG
            addButtons = Entities.DebuggingVariables.DoNotAddActionPanelButtons == false;
#endif
            if (addButtons)
            {
                ActionStackContainerInstance.AddUnitToggleButtons();
            }
        }
    }
}
