using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace RedsSacrifice.Baseline
{
    public class SimulacrumBaseline : IBaselineProvider
    {

        private InfiniteTowerWaveController WaveController { get; }

        private FieldInfo field_wavePeriodSeconds;
        private FieldInfo field_immediateCreditsFraction;

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
        private float ImmediateCreditsFraction
        {
            get
            {
                return (float) field_immediateCreditsFraction.GetValue(WaveController);
            }
        }

        public SimulacrumBaseline(InfiniteTowerWaveController infiniteTowerWaveController)
        {
            this.WaveController = infiniteTowerWaveController;

            this.field_wavePeriodSeconds = infiniteTowerWaveController.GetType()
                .GetField("wavePeriodSeconds", BindingFlags.Instance | BindingFlags.NonPublic);
            this.field_immediateCreditsFraction = infiniteTowerWaveController.GetType()
                .GetField("immediateCreditsFraction", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public double GetBaseline(DirectorTracker directorTracker, MonsterTracker monsterTracker)
        {
            return (TotalWaveCredits / WavePeriodSeconds * 60) * (RedsSacrifice.SimulacrumBaselineMult / 100.0d);
        }

    }
}
