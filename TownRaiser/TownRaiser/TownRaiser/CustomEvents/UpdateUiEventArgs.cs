using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.Interfaces;

namespace TownRaiser.CustomEvents
{
    public class UpdateUiEventArgs: EventArgs
    {
        public int GoldCost;
        public int LumberCost;
        public int StoneCost;
        public string TitleDisplay;
        public bool ShouldCheckAffordability;
        public ICommonEntityData SelectedData;

        public UpdateUiEventArgs()
        {

        }
        public UpdateUiEventArgs(ICommonEntityData dataToSetFrom)
        {
            SelectedData = dataToSetFrom;
            TitleDisplay = dataToSetFrom.MenuTitleDisplay;
            GoldCost = dataToSetFrom.Gold;
            LumberCost = dataToSetFrom.Lumber;
            StoneCost = dataToSetFrom.Stone;
            ShouldCheckAffordability = true;
        }

        public static UpdateUiEventArgs RollOffValue = new UpdateUiEventArgs { GoldCost = 0, LumberCost = 0, StoneCost = 0, TitleDisplay = "Build Menu" };
    }
}
