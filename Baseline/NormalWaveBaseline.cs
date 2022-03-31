using System;
using System.Collections.Generic;
using System.Text;

namespace RedsSacrifice.Baseline
{
    public class NormalWaveBaseline : AbstractWaveBaseline
    {

        public NormalWaveBaseline(DirectorTracker directorTracker) : base(directorTracker)
        {
        }

        public override double GetBaseline(DirectorTracker directorTracker, MonsterTracker monsterTracker)
        {
            return base.GetBaseline(RedsSacrifice.NormalWaveMultiplier, monsterTracker);
        }
    }
}
