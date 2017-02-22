using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.Entities;
using TownRaiser.Interfaces;
using static TownRaiser.GumRuntimes.IconButtonRuntime;

namespace TownRaiser.DataTypes
{
    public partial class BuildingData: ICommonEntityData
    {
        public Keys Hotkey => HotkeyFieldButUseProperty;
        public string DataName => Name;
        public string MenuTitleDisplay => this.NameDisplay;
        public int Gold => 0;
        public int Lumber => this.LumberCost;
        public int Stone => this.StoneCost;
        public bool ShouldEnableButton(int lumber, int stone, int gold, int currentCapacity, int maxCapacity, IEnumerable<Building> existingBuildings, IUpdatesStatus entityCreatedFrom)
        {

            //ToDo: do we care about capacity?
            if (lumber < LumberCost || stone < StoneCost)
            {
                return false;
            }

            foreach(var requirement in this.Requirements)
            {
                int numberRequired = requirement.Number;

                bool hasFulfilledRequirement = existingBuildings.Count(item => item.BuildingData.Name == requirement.Building) >= numberRequired;

                if(!hasFulfilledRequirement)
                {
                    // todo - do we want to show requirements to the user? Probably...
                    return false;
                }
            }

            return true;

        }
    }
}
