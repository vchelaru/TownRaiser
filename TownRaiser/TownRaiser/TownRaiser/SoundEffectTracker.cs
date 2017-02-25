using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TownRaiser
{
    
    public static class SoundEffectTracker
    {
        private struct SoundTrackerData
        {
            public double LastPlayTime;
            public double SoundDuration;
        }

        private static Dictionary<string, SoundTrackerData> m_LastSoundPlayTimes;
        public static void Initialize()
        {
            m_LastSoundPlayTimes = new Dictionary<string, SoundTrackerData>();
        }

        public static void Destroy()
        {
            m_LastSoundPlayTimes.Clear();
            m_LastSoundPlayTimes = null;
        }

        private static double LastPlayTime(string soundToCheck)
        {
            //If a sound has not been played yet, we will return -1
            double lastPlayTime = -1;

            if(m_LastSoundPlayTimes.ContainsKey(soundToCheck))
            {
                lastPlayTime = m_LastSoundPlayTimes[soundToCheck].LastPlayTime;
            }

            return lastPlayTime;
        }

        private static void RegisterTimeLastPlayed(string soundEffectName, double soundEffectDuration, double screenTimePlayed)
        {
            if (m_LastSoundPlayTimes.ContainsKey(soundEffectName))
            {
                var data = m_LastSoundPlayTimes[soundEffectName];
                data.LastPlayTime = screenTimePlayed;
                m_LastSoundPlayTimes[soundEffectName] = data;
            }
            else
            {
                m_LastSoundPlayTimes.Add(soundEffectName, new SoundTrackerData() { LastPlayTime = screenTimePlayed, SoundDuration = soundEffectDuration});
            }
        }

        public static void TryPlaySound(SoundEffect soundEffect, string soundEffectName)
        {
#if DEBUG
            if (soundEffect == null)
            {
                throw new Exception($"The sound effect: {soundEffectName}, does not exist.");
            }
#endif

            var currentScreen = FlatRedBall.Screens.ScreenManager.CurrentScreen;
            var lastSoundPlayTime = LastPlayTime(soundEffectName);

            if (currentScreen.PauseAdjustedSecondsSince(lastSoundPlayTime) >= soundEffect.Duration.TotalSeconds)
            {
                soundEffect.Play();
                RegisterTimeLastPlayed(soundEffectName, soundEffect.Duration.TotalSeconds, currentScreen.PauseAdjustedCurrentTime);
            }
        }
    }
}
