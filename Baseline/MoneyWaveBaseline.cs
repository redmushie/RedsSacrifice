using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace RedsSacrifice
{
    public class MoneyWaveBaseline : IBaselineProvider
    {

        private List<MoneyWaveTracker> WaveTrackers;

        public MoneyWaveBaseline(DirectorTracker directorTracker)
        {
            CombatDirector director = directorTracker.Director;

            // Director type and fields.
            Type tDirector = typeof(CombatDirector);

            FieldInfo fMoneyWaves = tDirector.GetField("moneyWaves", BindingFlags.Instance | BindingFlags.NonPublic);

            // Money wave type and fields.
            Type tMoneyWave = tDirector.GetNestedType("DirectorMoneyWave", BindingFlags.NonPublic | BindingFlags.Static);
            FieldInfo fInterval = tMoneyWave.GetField("interval");
            FieldInfo fMultiplier = tMoneyWave.GetField("multiplier");

            // Get the money wave list from the director.
            object[] moneyWaves = (object[])fMoneyWaves.GetValue(director);

            // Map each money wave to a MoneyWaveTracker.
            WaveTrackers = moneyWaves.Select(wave =>
            {
                double multiplier = (float)fMultiplier.GetValue(wave);
                double interval = (float)fInterval.GetValue(wave);
                return new MoneyWaveTracker(multiplier, interval);
            }).ToList();
        }

        public double GetBaseline(DirectorTracker directorTracker, MonsterTracker monsterTracker)
        {
            return WaveTrackers
                .Select(wave => wave.CalculateForTimeFrame(RedsSacrifice.WaveBaselineMult, monsterTracker.DifficultyCoefficient))
                .Sum();
        }

        private class MoneyWaveTracker
        {
            public double Multiplier { get; }
            public double Interval { get; }

            public MoneyWaveTracker(double multiplier, double interval)
            {
                Multiplier = multiplier;
                Interval = interval;
            }

            public double CalculateForTimeFrame(double timeFrame, double difficultyCoefficient)
            {
                Debug.Log($"Interval: {Interval}");
                double times = timeFrame / Interval;

                // Logic taken from CombatDirector.DirectorMoneyWave.Update(deltaTime, difficultyCoefficient).
                // It seems that since it's a private class it's really difficult to access, so we're not going
                // to bother *actually* calling the original method.
                double playerModifier = 0.5d + Run.instance.participatingPlayerCount * 0.5d;
                double difficultyModifier = 1d + 0.4d * difficultyCoefficient;

                double result = Interval * Multiplier * difficultyModifier * playerModifier;

                return times * result;
            }

        }
    }
}
