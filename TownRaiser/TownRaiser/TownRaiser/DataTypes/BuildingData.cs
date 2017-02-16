using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.Interfaces;
using static TownRaiser.GumRuntimes.ToggleButtonRuntime;

namespace TownRaiser.DataTypes
{
    public partial class BuildingData: IHotkeyData
    {
        public Keys Hotkey => HotkeyFieldButUseProperty;
        public IconDisplay ButtonIconDisplayState => ButtonIconDisplayStateButUseProperty;

        public bool ShouldEnableButton(int lumber, int stone, int gold, int currentCapacity, int maxCapacity)
        {
            //ToDo: Check lumber, stone, and capacity.
            return LumberCost <= lumber && StoneCost <= stone;
        }
    }
}
