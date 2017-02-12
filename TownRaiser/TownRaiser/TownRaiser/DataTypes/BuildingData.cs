using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.Interfaces;

namespace TownRaiser.DataTypes
{
    public partial class BuildingData: IHotkeyData
    {
        Keys IHotkeyData.Hotkey => HotkeyFieldButUseProperty;
    }
}
