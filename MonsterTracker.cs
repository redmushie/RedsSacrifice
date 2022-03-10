using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RedsSacrifice
{
    public class MonsterTracker : MonoBehaviour
    {

        public double Cost { get; internal set; }
        public DirectorTracker DirectorTracker { get; internal set; }
        public double DifficultyCoefficient { get; internal set; }

        public MonsterTracker()
        {}

    }
}
