using System;
using System.Collections.Generic;
using System.Text;

namespace RedsSacrifice
{
    public interface IChanceProvider
    {

        double Calculate(double credits, double window);

    }
}
