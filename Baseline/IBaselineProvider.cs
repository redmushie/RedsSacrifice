using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedsSacrifice
{
    public interface IBaselineProvider
    {

        double GetBaseline(DirectorTracker directorTracker, MonsterTracker monsterTracker);

    }
}
