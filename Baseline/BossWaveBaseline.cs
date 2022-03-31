using System;
using System.Collections.Generic;
using System.Text;

namespace RedsSacrifice
{
    public class BossWaveBaseline : AbstractWaveBaseline
    {

        public BossWaveBaseline(DirectorTracker directorTracker) : base(directorTracker)
        {
        }

        public override double GetBaseline(DirectorTracker directorTracker, MonsterTracker monsterTracker)
        {
            return base.GetBaseline(RedsSacrifice.BossWaveMultiplier, monsterTracker);
        }
    }
}
