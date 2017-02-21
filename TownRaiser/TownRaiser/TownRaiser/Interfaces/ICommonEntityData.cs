using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.Entities;
using static TownRaiser.GumRuntimes.IconButtonRuntime;

namespace TownRaiser.Interfaces
{
    public interface ICommonEntityData
    {
        Microsoft.Xna.Framework.Input.Keys Hotkey { get; }
        string DataName { get; }
        string MenuTitleDisplay { get; }
        int Gold { get; }
        int Lumber { get; }
        int Stone { get; }
        bool ShouldEnableButton(int lumber, int stone, int gold, int currentCapacity, int maxCapacity, IEnumerable<Building> existingBuildings);
    }
}
