using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using TownRaiser.Interfaces;
using static TownRaiser.GumRuntimes.IconButtonRuntime;
using TownRaiser.Entities;

namespace TownRaiser.DataTypes
{
    public partial class UnitData : IHotkeyData
    {
        public Keys Hotkey => HotkeyFieldButUseProperty;
        public string ChainName => Name;
        public string MenuTitleDisplay => this.NameDisplay;
        public int Gold => this.Gold;
        //At this time, units do not have a stone or lumber requirement
        public int Lumber => 0;
        public int Stone => 0;
        public bool ShouldEnableButton(int lumber, int stone, int gold, int currentCapacity, int maxCapacity, IEnumerable<Building> existingBuildings)
        {
            return GoldCost <= gold && (currentCapacity + Capacity) <= maxCapacity;
        }
    }
}
