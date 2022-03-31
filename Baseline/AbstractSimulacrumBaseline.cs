using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace RedsSacrifice.Baseline
{
    public abstract class AbstractSimulacrumBaseline : IBaselineProvider
    {

        private InfiniteTowerWaveController WaveController { get; }

        private FieldInfo field_wavePeriodSeconds;
        private FieldInfo field_creditsPerSecond;

        /**
         * The total amount of credits we give the CombatDirector this wave.
         */
        private float TotalWaveCredits
        {
            get
            {
                return WaveController.totalWaveCredits;
            }
        }

        /**
         * The period (in seconds) over which we give the CombatDirector its credits.
         */
        private float WavePeriodSeconds
        {
            get
            {
                return (float) field_wavePeriodSeconds.GetValue(WaveController);
            }
        }

        /**
         * The normalized fraction of the total credits to give to the CombatDirector immediately
         */
        private float CreditsPerSecond
        {
            get
            {
                return (float) field_creditsPerSecond.GetValue(WaveController);
            }
        }

        public AbstractSimulacrumBaseline(InfiniteTowerWaveController infiniteTowerWaveController)
        {
            this.WaveController = infiniteTowerWaveController;

            this.field_wavePeriodSeconds = infiniteTowerWaveController.GetType()
                .GetField("wavePeriodSeconds", BindingFlags.Instance | BindingFlags.NonPublic);
            this.field_creditsPerSecond = infiniteTowerWaveController.GetType()
                .GetField("creditsPerSecond", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        protected double GetBaseline(double multiplier, MonsterTracker monsterTracker)
        {
            if (RedsSacrifice.Debugging)
            {
                RedsSacrifice.Logger.LogInfo($"TotalWaveCredits={TotalWaveCredits}, WavePeriodSeconds={WavePeriodSeconds}, CreditsPerSecond={CreditsPerSecond}, Multiplier={multiplier}");
            }
            return TotalWaveCredits / (multiplier / 100);
        }

        abstract public double GetBaseline(DirectorTracker directorTracker, MonsterTracker monsterTracker);

    }
}
