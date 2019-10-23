using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.TestScript
{
    public static class ScriptExtend
    {
        public static void setRateOverTime(this ParticleSystem.EmissionModule emission, float constant)
        {
            emission.rate = new ParticleSystem.MinMaxCurve(constant);
        }
    }
}
