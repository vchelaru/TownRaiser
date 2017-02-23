using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.CustomEvents;
using TownRaiser.Interfaces;

namespace TownRaiser.GumRuntimes
{
    public partial class ActionToolbarRuntime
    {
        private IUpdatesStatus m_LastSelectedEntity;
        

        public void SetViewFromEntity(IUpdatesStatus selectedEntity)
        {
            //Player selected a different entity
            if (m_LastSelectedEntity != null && m_LastSelectedEntity != selectedEntity)
            {
                m_LastSelectedEntity.UpdateStatus -= ReactToBuilidingStatusChange;
                ActionStackContainerInstance.RemoveIconButtons();
            }


            if (selectedEntity == null)
            {
                SetVariableState(VariableState.SelectModeView);
            }
            else if (selectedEntity is Entities.Building)
            {
                UpdateBuildingStatus(selectedEntity as Entities.Building);
                SetVariableState(VariableState.SelectedEntity);
                selectedEntity.UpdateStatus += ReactToBuilidingStatusChange;
                ShowAvailableUnits(selectedEntity);
            }
            m_LastSelectedEntity = selectedEntity;
            if (m_LastSelectedEntity != null)
            {
                this.ReactToUpdateUiChangeEvent(this, new UpdateUiEventArgs() { TitleDisplay = selectedEntity.EntityData.MenuTitleDisplay });
            }
        }

        private void ReactToBuilidingStatusChange(object sender, UpdateStatusEventArgs args)
        {
            var building = sender as Entities.Building;
            if (building != null)
            {
                if (args.WasEntityDestroyed == false)
                {
                    UpdateBuildingStatus(building);

                }
                else
                {
                    this.SetVariableState(VariableState.SelectModeView);
                }
            }
        }

        private void UpdateBuildingStatus(Entities.Building building)
        {
            UpdateHealthState(building);
            UpdateBuilidngTrainingStatus(building);
        }
        private void UpdateHealthState(IUpdatesStatus entity)
        {
            //ToDo: Discuss a health view option.
        }
        private void UpdateBuilidngTrainingStatus(Entities.Building building)
        {
            ActionStackContainerInstance.UpdateIconCoolDown(building);           
        }
    }
}
