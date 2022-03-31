using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace RedsSacrifice.Baseline
{
    public class SimulacrumBossWaveBaseline : AbstractSimulacrumBaseline
    {

        public SimulacrumBossWaveBaseline(InfiniteTowerWaveController infiniteTowerWaveController) : base(infiniteTowerWaveController)
        {
        }

        public override double GetBaseline(DirectorTracker directorTracker, MonsterTracker monsterTracker)
        {
            return base.GetBaseline(RedsSacrifice.SimulacrumBossWaveMultiplier, monsterTracker);
        }
    }
}
