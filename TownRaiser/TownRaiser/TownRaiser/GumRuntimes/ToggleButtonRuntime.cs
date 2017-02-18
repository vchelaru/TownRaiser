using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.DataTypes;
using TownRaiser.Entities;
using TownRaiser.Interfaces;

namespace TownRaiser.GumRuntimes
{
    public partial class ToggleButtonRuntime
    {
        private IHotkeyData m_HotKeyData;
        public IHotkeyData HotkeyData
        {
            get
            {
                return m_HotKeyData;
            }
            set
            {
                if(value != null)
                {
                    m_HotKeyData = value;
                    TextInstance.Text = m_HotKeyData.Hotkey.ToString().ToLower();
                    CurrentIconDisplayState = m_HotKeyData.ButtonIconDisplayState;
                }
            }
        }

        public BuildingData HotKeyDataAsBuildingData => m_HotKeyData as BuildingData;
        public UnitData HotKeyDataAsUnitData => m_HotKeyData as UnitData; 
        
        public void UpdateButtonEnabledState(int lumber, int stone, int gold, int currentCapacity, int maxCapacity, IEnumerable<Building> existingBuildings)
        {
            var isEnabled = m_HotKeyData.ShouldEnableButton(lumber, stone, gold, currentCapacity, maxCapacity, existingBuildings);

#if DEBUG
            if(Entities.DebuggingVariables.HasInfiniteResources)
            {
                isEnabled = true;
            }
#endif

            Enabled = isEnabled;


            //Switch off if the button is disabled.
            if(Enabled == false)
            {
                IsOn = false;
            }
        }
    }
}
