using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.DataTypes;

namespace TownRaiser.CustomEvents
{
    public class ConstructBuildingEventArgs : EventArgs
    {
        public BuildingData BuildingData;
    }
}
