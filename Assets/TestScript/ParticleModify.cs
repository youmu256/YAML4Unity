using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Assets.TestScript
{

    public class ParticleModify : MonoBehaviour
    {
        public ParticleSystem Particle { get { return GetComponent<ParticleSystem>(); } }


        [ContextMenu("TEST")]
        void TestModify()
        {
            //面板上有裁剪mode类型的，全部都会出问题...
            var a = Particle.sizeOverLifetime.size;
            var b = Particle.colorBySpeed.color;
            var c = Particle.sizeBySpeed.size;



            return;
            BindingFlags flag = BindingFlags.Static | BindingFlags.NonPublic;
            var m = Particle.sizeOverLifetime;
            /*
            var mts = m.GetType().GetMethods(flag);
            foreach (var methodInfo in mts)
            {
                Debug.Log(methodInfo.Name);
            }
            */
            //var rt = m.GetType().GetMethod("GetEnabled", flag).Invoke(null, new object[] {Particle});
            //Debug.Log((bool)rt);

            ParticleSystem.MinMaxCurve curve = new ParticleSystem.MinMaxCurve();
            var getSizeMt = m.GetType().GetMethod("GetSize",flag);
            object[] invokeArgs = new object[] { Particle, curve };
            getSizeMt.Invoke(null, invokeArgs);
            curve = (ParticleSystem.MinMaxCurve) invokeArgs[1];
            Debug.Log(curve.mode);//--mode 能取到--

            Debug.Log("max curve is null?" + (curve.curveMax == null));
            Debug.Log("min curve is null?" + (curve.curveMin == null));

            return;
            Debug.Log("max curve end : " + curve.curveMax.Evaluate(1));
            Debug.Log("min curve end : " + curve.curveMin.Evaluate(1));

            return;
            //new ParticleSystem.MinMaxCurve();
            var sm = Particle.sizeOverLifetime;
            var sizeP = sm.GetType().GetProperty("size");
            var minMaxCurve = sizeP.GetValue(sm, null);
            //Debug.Log(module.GetType()); --size module
            var modeP = minMaxCurve.GetType().GetProperty("mode");
            var mode = modeP.GetValue(minMaxCurve, null);
            Debug.LogWarning("mode:"+mode);

            var max = minMaxCurve.GetType().GetProperty("constantMax").GetValue(minMaxCurve, null);
            Debug.LogWarning("constantMax : " +max);
            var min = minMaxCurve.GetType().GetProperty("constantMin").GetValue(minMaxCurve, null);
            Debug.LogWarning("constantMin : " + min);

            var curveP = minMaxCurve.GetType().GetProperty("curveMax");
            //Debug.LogWarning(curveP.Name);//curveP是curveMax的属性
            var acurve = curveP.GetValue(minMaxCurve, null);//属性的值
            Debug.LogWarning("curveMax : " +acurve);
            //var cm = Particle.colorBySpeed.color;

            return;
            var gm = Particle.colorOverLifetime;
            var g = new Gradient();
            g.SetKeys(new[] {new GradientColorKey(Color.white, 0), new GradientColorKey(Color.red, 1)},new [] {new GradientAlphaKey(1,0),new GradientAlphaKey(1,1)  } );
            gm.color = new ParticleSystem.MinMaxGradient(g);
            var keys = g.colorKeys;
            foreach (var colorKey in keys)
            {
                Debug.Log(colorKey.color);
            }
        }
    }
}
