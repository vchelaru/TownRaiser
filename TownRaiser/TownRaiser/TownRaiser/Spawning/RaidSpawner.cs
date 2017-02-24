using FlatRedBall;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Math;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.DataTypes;
using TownRaiser.Entities;
using TownRaiser.Factories;

namespace TownRaiser.Spawning
{
    public class RaidSpawner
    {
        #region Fields/Properties

        const float SpawnFrequency = 30;

        double lastSpawn;

        public PositionedObjectList<Building> Buildings { get; set; }
        public TileNodeNetwork NodeNetwork { get; set; }

        public event Action<IEnumerable<UnitData>, Vector3> RequestSpawn;

        #endregion

        public void Activity()
        {
            var screen = ScreenManager.CurrentScreen;

            bool hasTownHall = GetTownHall() != null;

            if(hasTownHall)
            {
                if (screen.PauseAdjustedSecondsSince(lastSpawn) > SpawnFrequency)
                {
                    PerformSpawn();
                }
            }
            else
            {
                // If there's no town hall, reset the timer so that the user gets the full spawn period before the first attack comes.
                lastSpawn = screen.PauseAdjustedCurrentTime;
            }
        }

        private void PerformSpawn()
        {
            var spawnLocation = GetSpawnLocation();

            if(spawnLocation != null)
            {
                var unitDatas= GetUnitDatasToSpawn();

#if DEBUG
                Console.WriteLine($"Spawning with {unitDatas.Count()} enemies for threat level {GetThreatLevel()}");
#endif
                RequestSpawn?.Invoke(unitDatas, spawnLocation.Value);

            }
            lastSpawn = ScreenManager.CurrentScreen.PauseAdjustedCurrentTime;
        }

        private Vector3? GetSpawnLocation()
        {
            Vector3? toReturn = null;

            Building building = GetTownHall();

            // No building, no spawn. Sing it Bob!
            if (building != null)
            {
                // We proably do want it to be like 250, but we'll shorten it for debugging:
                //const float offsetDistance = 250;
                const float offsetDistance = 150;

                var offsetFromBuilding = FlatRedBallServices.Random.RadialVector2(offsetDistance, offsetDistance);

                var position = building.Position + new Vector3(offsetFromBuilding, 0);
                // so they don't spawn in a horizontal line:
                var closestNode = NodeNetwork.GetClosestNodeTo(ref position);

                var nodePosition = closestNode.Position;
                nodePosition.Y += FlatRedBallServices.Random.Between(-1, 1);
                toReturn = nodePosition;
            }

            return toReturn;
        }

        private Building GetTownHall()
        {
            return Buildings.FirstOrDefault(item => item.BuildingData.Name == BuildingData.TownHall);
        }

        private IEnumerable<UnitData> GetUnitDatasToSpawn()
        {
            var threatData = GetThreatLevel();

            int numberOfSpawns = GlobalContent.TimedSpawnData.Count;

            threatData = System.Math.Min(threatData, numberOfSpawns - 1);

            var spawnData = GlobalContent.TimedSpawnData[threatData];

            List<UnitData> toReturn = new List<UnitData>();

            foreach (var unitName in spawnData.Units)
            {
                toReturn.Add(GlobalContent.UnitData[unitName]);
            }

            return toReturn;
        }

        private int GetThreatLevel()
        {
            int coefficient = 3;
            // threat level is calculated using the number of non-town hall buildings
            return Buildings.Count(item => item.BuildingData.Name != BuildingData.TownHall) / coefficient;
        }


    }
}
