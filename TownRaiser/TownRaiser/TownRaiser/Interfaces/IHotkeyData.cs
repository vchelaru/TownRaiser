using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TownRaiser.GumRuntimes.ToggleButtonRuntime;

namespace TownRaiser.Interfaces
{
    public interface IHotkeyData
    {
        Microsoft.Xna.Framework.Input.Keys Hotkey { get; }
        IconDisplay ButtonIconDisplayState { get; }
        bool ShouldEnableButton(int lumber, int stone, int gold, int currentCapacity, int maxCapacity);
    }
}
