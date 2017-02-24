using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TownRaiser.Entities
{
    public partial class Unit
    {
        private static double m_TimeSinceLastLumberSound = -1;
        private static double m_TimeSinceLastStoneSound = -1;
        private static double m_TimeSinceLastGoldSound = -1;
        public static void TryToPlayResourceGatheringSfx(Screens.ResourceType resourceType)
        {
            switch (resourceType)
            {
                case Screens.ResourceType.Gold:
                    TryPlayGoldHarvestSound();
                    break;
                case Screens.ResourceType.Lumber:
                    TryPlayLumberHarvestSound();
                    break;
                case Screens.ResourceType.Stone:
                    TryPlayStoneHarvestSound();
                    break;
            }
        }

        private static void TryPlayStoneHarvestSound()
        {
            //Keepign this commented out till we get those sounds.
            //var gameScreen = FlatRedBall.Screens.ScreenManager.CurrentScreen as Screens.GameScreen;

            //if(gameScreen.PauseAdjustedSecondsSince(m_TimeSinceLastStoneSound) >= GlobalContent.SoundEffectData[DataTypes.SoundEffectData.GatherStone].SecondsBetweenPlays)
            //{
            //    int randomInt = FlatRedBall.FlatRedBallServices.Random.Next(1);
                
            //    m_TimeSinceLastStoneSound = gameScreen.PauseAdjustedCurrentTime;
            //}
        }

        private static void TryPlayLumberHarvestSound()
        {
            var gameScreen = FlatRedBall.Screens.ScreenManager.CurrentScreen as Screens.GameScreen;

            if (gameScreen.PauseAdjustedSecondsSince(m_TimeSinceLastLumberSound) >= GlobalContent.SoundEffectData[DataTypes.SoundEffectData.GatherLumber].SecondsBetweenPlays)
            {
                int randomInt = FlatRedBall.FlatRedBallServices.Random.Next(1);
                if (randomInt == 0)
                {
                    unit_chop_wood_1.Play();
                }
                else
                {
                    unit_chop_wood_2.Play();
                }
                m_TimeSinceLastLumberSound = gameScreen.PauseAdjustedCurrentTime;
            }
        }

        private static void TryPlayGoldHarvestSound()
        {
            //Keepign this commented out till we get those sounds.
            //var gameScreen = FlatRedBall.Screens.ScreenManager.CurrentScreen as Screens.GameScreen;

            //if (gameScreen.PauseAdjustedSecondsSince(m_TimeSinceLastGoldSound) >= GlobalContent.SoundEffectData[DataTypes.SoundEffectData.GatherGold].SecondsBetweenPlays)
            //{
            //    int randomInt = FlatRedBall.FlatRedBallServices.Random.Next(1);

            //    m_TimeSinceLastGoldSound = gameScreen.PauseAdjustedCurrentTime;
            //}
        }
    }
}
