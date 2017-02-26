using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownRaiser.DataTypes;

namespace TownRaiser.Entities
{
    public partial class Building
    {
        private static string BuildingConstructionComplete = "building_constuction_complete";
        private static string GenericBuildingComplete = "building_constuction_complete_generic_1";
        private static string BuildingContstructionStart = "building_construction_start_generic_1";

        private static void PlayConstructionCompleteSoundEffect(BuildingData building)
        {
            var soundEffectName = $"{BuildingConstructionComplete}_{building.SoundEffectName}_1";
            var soundEffect = (SoundEffect)GetFile(soundEffectName);

            if(soundEffect == null)
            {
                soundEffect = building_constuction_complete_generic_1;
                soundEffectName = GenericBuildingComplete;
            }

            SoundEffectTracker.TryPlaySound(soundEffect, soundEffectName);
        }

        public static void PlayConstructionStartSoundEffect()
        {
            //Right now we do not have building specific start sounds. But I want to track the when the sound is played.
            //Unless desired by the sound guys, we will keep it setup like this.
            SoundEffectTracker.TryPlaySound(building_constuction_start_generic_1, BuildingContstructionStart);
        }
    }
}
