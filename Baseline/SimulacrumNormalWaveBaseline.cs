using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace RedsSacrifice.Baseline
{
    public class SimulacrumNormalWaveBaseline : AbstractSimulacrumBaseline
    {

        public SimulacrumNormalWaveBaseline(InfiniteTowerWaveController infiniteTowerWaveController) : base(infiniteTowerWaveController)
        {
        }

        public override double GetBaseline(DirectorTracker directorTracker, MonsterTracker monsterTracker)
        {
            return base.GetBaseline(RedsSacrifice.SimulacrumNormalWaveMultiplier, monsterTracker);
        }

    }
}
