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
        public event EventHandler TrainClicked;
        public event EventHandler BuildClicked;

        public ActionMode GetActionModeBasedOnToggleState()
        {
            if (TrainButtonInstance.IsOn) return ActionMode.Train;
            else if (BuildButtonInstance.IsOn) return ActionMode.Build;
            else return ActionMode.Select;
        }

        partial void CustomInitialize()
        {
            this.TrainButtonInstance.Click += (notused) => this.TrainClicked(this, null);
            this.BuildButtonInstance.Click += (notused) => this.BuildClicked(this, null);
        }

        internal void UntoggleAllExcept(ActionMode actionMode)
        {
            if(actionMode != ActionMode.Build)
            {
                BuildButtonInstance.IsOn = false;
            }
            if(actionMode != ActionMode.Train)
            {
                TrainButtonInstance.IsOn = false;
            }
        }
    }
}
