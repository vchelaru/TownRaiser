using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using TownRaiser.Interfaces;
using static TownRaiser.GumRuntimes.ToggleButtonRuntime;

namespace TownRaiser.DataTypes
{
    public partial class UnitData : IHotkeyData
    {
        public Keys Hotkey => HotkeyFieldButUseProperty;
        public IconDisplay ButtonIconDisplayState => ButtonIconDisplayStateButUseProperty;

        public bool ShouldEnableButton(int lumber, int stone, int gold, int currentCapacity, int maxCapacity)
        {
            return GoldCost <= gold && (currentCapacity + Capacity) <= maxCapacity;
        }
    }
}
