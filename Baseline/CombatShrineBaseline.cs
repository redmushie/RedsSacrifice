using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedsSacrifice
{
    public class CombatShrineBaseline : IBaselineProvider
    {

        private double Credits { get; }

        public CombatShrineBaseline(double credits)
        {
            Credits = credits;
        }

        public double GetBaseline(DirectorTracker directorTracker, MonsterTracker monsterTracker)
        {
            return Credits * RedsSacrifice.ShrineBaselineMult;
        }
    }
}
