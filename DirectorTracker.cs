using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace RedsSacrifice
{
    public class DirectorTracker
    {

        public CombatDirector Director { get; }
        public UnityAction<GameObject> Listener { get; internal set; }

        public IBaselineProvider Baseline { get; internal set; }
        
        public DirectorTracker(CombatDirector director)
        {
            Director = director;
        }

    }
}
