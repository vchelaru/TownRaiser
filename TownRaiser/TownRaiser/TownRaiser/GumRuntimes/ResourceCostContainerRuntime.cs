using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.CustomEvents;

namespace TownRaiser.GumRuntimes
{
    public partial class ResourceCostContainerRuntime
    {
        public void UpadteResourceDisplayText(UpdateUiEventArgs args)
        {
            this.GoldCostText = $"{args.GoldCost}";
            this.LumberCostText = $"{args.LumberCost}";
            this.StoneCostText = $"{args.StoneCost}";
        }
    }
}
