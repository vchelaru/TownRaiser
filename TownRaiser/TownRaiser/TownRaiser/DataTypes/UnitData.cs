using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using TownRaiser.Interfaces;

namespace TownRaiser.DataTypes
{
    public partial class UnitData : IHotkeyData
    {
        Keys IHotkeyData.Hotkey => HotkeyFieldButUseProperty;
    }
}
