using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using YamlDotNet.Samples.Helpers;
using YamlDotNet.Serialization;

namespace Assets.TestScript
{
    public class Unity2Yaml : MonoBehaviour
    {
        //--进行一些类序列化的测试
        public AnimationCurve ACurve;
        public ParticleSystem.MinMaxCurve Curve;
        private class StringTestOutputHelper : ITestOutputHelper
        {
            private StringBuilder output = new StringBuilder();
            public void WriteLine()
            {
                output.AppendLine();
            }
            public void WriteLine(string value)
            {
                output.AppendLine(value);
            }
            public void WriteLine(string format, params object[] args)
            {
                output.AppendFormat(format, args);
                output.AppendLine();
            }

            public override string ToString() { return output.ToString(); }
            public void Clear() { output = new StringBuilder(); }
        }

        [ContextMenu("Convert")]
        public void Convert()
        {
            Curve = new ParticleSystem.MinMaxCurve(1,ACurve,ACurve);
            ITestOutputHelper output =new StringTestOutputHelper();
            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(Curve);
            output.WriteLine(yaml);
            Debug.Log(output);
        }
    }
}
