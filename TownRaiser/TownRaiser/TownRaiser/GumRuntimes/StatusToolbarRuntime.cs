using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.Interfaces;

namespace TownRaiser.GumRuntimes
{
    public partial class StatusToolbarRuntime
    {
        public List<TrainingUnitSpriteRuntime> TrainingUnits;
        private IUpdatesStatus m_LastSelectedEntity;

        partial void CustomInitialize()
        {
            TrainingUnits = new List<TrainingUnitSpriteRuntime>();
            CurrentVariableState = VariableState.NothingSelected;
        }

        public void SetViewFromEntity(IUpdatesStatus selectedEntity)
        {
            if (m_LastSelectedEntity != null)
            {
                m_LastSelectedEntity.UpdateStatus -= ReactToBuilidingStatusChange;
                ClearTrainingUnitsList();
            }


            if(selectedEntity == null)
            {
                CurrentVariableState = VariableState.NothingSelected;
            }
            else if(selectedEntity is Entities.Building)
            {
                UpdateBuildingStatus(selectedEntity as Entities.Building);
                this.CurrentVariableState = VariableState.BuildingSelected;
                selectedEntity.UpdateStatus += ReactToBuilidingStatusChange;
            }
            m_LastSelectedEntity = selectedEntity;
            if (m_LastSelectedEntity != null)
            {
                this.SelectedObjectText = selectedEntity.NameDisplay;
            }
        }

        private void ReactToBuilidingStatusChange(object sender, UpdateStatusEventArgs args)
        {
            var building = sender as Entities.Building;
            if(building != null)
            {
                if(args.WasEntityDestroyed == false)
                {
                    UpdateBuildingStatus(building);

                }
                else
                {
                    ClearTrainingUnitsList();
                }
            }
        }

        private void ClearTrainingUnitsList()
        {
            CurrentTrainingUnitVisible = false;
            for (int i = TrainingUnits.Count - 1; i > -1; i--)
            {
                var unit = TrainingUnits[i];
                TrainingUnits.Remove(unit);
                TrainingQueueContainer.Children.Remove(unit);
                unit.Destroy();
            }
        }

        private void UpdateBuildingStatus(Entities.Building building)
        {
            UpdateHealthState(building);
            UpdateBuilidngTrainingStatus(building);
        }
        private void UpdateHealthState(IUpdatesStatus entity)
        {
            var currentHealthPercentage = entity.GetHealthRatio();

            if(currentHealthPercentage <= .25)
            {
                CurrentHealthStateState = HealthState.Quarter;
            }
            else if(currentHealthPercentage <= .5)
            {
                CurrentHealthStateState = HealthState.Half;
            }
            else if(currentHealthPercentage <= .75)
            {
                CurrentHealthStateState = HealthState.ThreeQuarter;
            }
            else
            {
                CurrentHealthStateState = HealthState.Full;
            }
        }
        private void UpdateBuilidngTrainingStatus(Entities.Building building)
        {
            //Handle Current Unit first
            if(building.CurrentTrainingUnit != null)
            {
                CurrentTrainingUnitVisible = true;
                SetCurrentUnitTrainingState(building.TrainingProgressPercent);
            }
            else
            {
                CurrentTrainingUnitVisible = false;
                CurrentUnitTrainingProgressState = TrainingUnitSpriteRuntime.TrainingProgress.Waiting;
            }



            for (int i = 1; i < building.TrainingQueue.Count; i ++)
            {
                //Only add enough units to fill the TrainingUnits list.
                //If i is greater than the count, then we have to add 1 to the list.
                if (i > TrainingUnits.Count)
                {
                    var unitVisual = new TrainingUnitSpriteRuntime();
                    unitVisual.AddToManagers(TrainingQueueContainer.Managers, null);
                    unitVisual.CurrentTrainingProgressState = TrainingUnitSpriteRuntime.TrainingProgress.Waiting;
                    unitVisual.Parent = TrainingQueueContainer;
                    unitVisual.X = i > 1 ? 1 : 0;
                    TrainingUnits.Add(unitVisual);
                }
            }
            //1 minus the training queue should match number of visuals waiting to be trained.
            //Remove any units we may need.
            if (building.TrainingQueue.Count > 0)
            {
                while (building.TrainingQueue.Count - 1 != TrainingUnits.Count)
                {
                    var unitVisual = TrainingUnits[TrainingUnits.Count - 1];
                    TrainingQueueContainer.Children.Remove(unitVisual);
                    unitVisual.Parent = null;
                    TrainingUnits.Remove(unitVisual);
                    unitVisual.Destroy();
                }
            }
        }
        private void SetCurrentUnitTrainingState(double progress)
        {
            if(progress >= 1)
            {
                CurrentUnitTrainingProgressState = TrainingUnitSpriteRuntime.TrainingProgress.Finished;
            }
            else if (progress >= .75)
            {
                CurrentUnitTrainingProgressState = TrainingUnitSpriteRuntime.TrainingProgress.ThreeQuarter;
            }
            else if (progress >= .5)
            {
                CurrentUnitTrainingProgressState = TrainingUnitSpriteRuntime.TrainingProgress.Half;
            }
            else if (progress >= .25)
            {
                CurrentUnitTrainingProgressState = TrainingUnitSpriteRuntime.TrainingProgress.Quarter;
            }
            else
            {
                CurrentUnitTrainingProgressState = TrainingUnitSpriteRuntime.TrainingProgress.Waiting;
            }
        }
    }
}
