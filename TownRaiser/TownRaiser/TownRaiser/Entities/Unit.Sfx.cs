using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.DataTypes;

namespace TownRaiser.Entities
{
    public partial class Unit
    {
        private static readonly string UnitWoodChop = "unit_chop_wood";
        private static readonly string UnitMineGold = "unit_mine_gold";
        private static readonly string UnitMineStone = "unit_mine_stone";
        public static void TryToPlayResourceGatheringSfx(Screens.ResourceType resourceType)
        {
            switch (resourceType)
            {
                case Screens.ResourceType.Gold:
                    TryPlayRandomSound(UnitMineGold);
                    break;
                case Screens.ResourceType.Lumber:
                    TryPlayRandomSound(UnitWoodChop);
                    break;
                case Screens.ResourceType.Stone:
                    //ToDo: Rick - Uncomment when stone is implemented.
                    //TryPlayRandomSound(UnitMineStone);
                    break;
            }
        }

        private static void TryPlaySpawnSound(UnitData unit)
        {

        }

        private static void TryPlayObeySound(UnitData unit)
        {

        }

        private static void TryPlayAttackSound(UnitData unit)
        {

        }

        public static void TryPlayRandomSound(string soundName)
        {
            int count = 1;
            //Finds the number of sounds
            while (true)
            {
                bool soundExists = GetFile($"{soundName}_{count + 1}") != null;
                if (soundExists)
                {
                    count++;
                }
                else
                {
                    break;
                }
            }
            //Picks a random sound from a variant if it exists
            int randomInt = FlatRedBall.FlatRedBallServices.Random.Next(count) + 1;

            var soundEffect = (SoundEffect) GetFile($"{soundName}_{randomInt}");
                        
            //For now only track by the sound class, not the random numbered sound variation.
            //We can expand that later.
            SoundEffectTracker.TryPlaySound(soundEffect, soundName);
        }
    }
}
