using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using Assets.Util;
using Assets.YamlObject;

public class TestYaml : MonoBehaviour {

    public string PrefabPath = "Prefabs/GameObject";
    [ContextMenu("ReadPrefab")]
    public void TestPrefab()
    {
        ReadPrefab(PrefabPath);
    }
    
    /*
    public Color CheckColor;
    [ContextMenu("DeColor")]
    public void DeColor()
    {
        string csr = ColorUtility.ToHtmlStringRGBA(CheckColor);
        string[] srr = new string[4];
        for (int i = 0; i < srr.Length; i++)
        {
            int index = i * 2;
            srr[i] = csr[index].ToString() + csr[index + 1];
        }
        csr = "";
        for (int i = 0; i < srr.Length; i++)
        {
            csr += srr[srr.Length - i - 1];
        }
        csr = Convert.ToInt64(csr, 16).ToString();
        Debug.Log(csr);
    }
    */
    public void ColorTest()
    {
        //什么傻逼COLOR序列化
        Int64 c = 4278190337;
        
        string csr = Convert.ToString(c, 16);
        int size = 8 - csr.Length;
        for (int i = 0; i < size; i++)
        {
            csr = "0" + csr;
        }
        char[] cr = csr.ToCharArray();
        Array.Reverse(cr);
        csr = new string(cr);
        csr = "#" + csr;
        Debug.Log(csr);
        Color color = Color.black;
        ColorUtility.TryParseHtmlString(csr, out color);
        Debug.Log(color);

    }

    public static void ReadMeta(string metaFilePath)
    {
        string content = File.ReadAllText(metaFilePath);
        var input = new StringReader(content);
        var deserializer = new DeserializerBuilder().Build();
        var yamlObject = deserializer.Deserialize(input);
        Debug.Log(yamlObject);
    }


    public YoooMinMaxGradient StartColor;

    public YoooMinMaxCurve StartSize;
    /// <summary>
    /// Unity5.3直接读取Yaml来操作预制体
    /// 绕了十万八千里就是为了
    /// 给粒子系统写入一些参数。
    /// </summary>
    /// <param name="prefabPath">Prefab预制体资源路径</param>
    public void ReadPrefab(string prefabPath)
    {
        var yamlPrefab = new YamlPrefab(prefabPath);
        //yamlPrefab.RootGameObject.Transofrm.FindTransoform("p2");//找到p2的子节点 transform
        //Debug.Log(yamlPrefab.FinGameObject("O3").ObjectName);
        foreach (var child in yamlPrefab.RootGameObject.Transofrm.Children)
        {
            var p = child.GameObject.GetComponment<YamlParticleSystem>();
            if (p != null)
            {
                p.InitialModule.StartColor.Apply(StartColor.GetData());
                p.InitialModule.StartSize.Apply(StartSize.GetData());
                /*
                p.LengthInSec = 1;//修改持续时间
                p.Prewarm = true;//修改预热
                p.InitialModule.StartColor.MinMaxState = ParticleSystemGradientMode.Gradient;//修改StartColor参数
                p.InitialModule.StartColor.MaxGradient.SetColorKey(new GradientColorKey(Color.red, 0.5f));//加一个颜色Key
                */
                
            }
        }
        yamlPrefab.Save();
    }
}
[Serializable]
public class YoooMinMaxCurve
{
    public ParticleSystem.MinMaxCurve Data { get; private set; }

    public float constantMax;
    public float constantMin;
    public AnimationCurve curveMax;
    public AnimationCurve curveMin;
    public float curveScalar;
    public ParticleSystemCurveMode mode;


    public YoooMinMaxCurve(ParticleSystem.MinMaxCurve curve)
    {
        Data = curve;
        constantMax = Data.constantMax;
        constantMin = Data.constantMin;
        curveMax = Data.curveMax;
        curveMin = Data.curveMin;
        curveScalar = Data.curveScalar;
        mode = Data.mode;
    }

    public ParticleSystem.MinMaxCurve GetData()
    {
        var data = new ParticleSystem.MinMaxCurve();
        data.constantMax = constantMax;
        data.constantMin = constantMin;
        data.curveMax = curveMax;
        data.curveMin = curveMin;
        data.curveScalar = curveScalar;
        data.mode = mode;
        return data;
    }

}

[Serializable]
public class YoooMinMaxGradient
{
    public ParticleSystem.MinMaxGradient Data { get; private set; }

    public Color colorMax;
    public Color colorMin;
    public Gradient gradientMax;
    public Gradient gradientMin;
    public ParticleSystemGradientMode mode;

    public YoooMinMaxGradient(ParticleSystem.MinMaxGradient gradient)
    {
        Data = gradient;
        colorMax = Data.colorMax;
        colorMin = Data.colorMin;
        gradientMax = Data.gradientMax;
        gradientMin = Data.gradientMin;
        mode = Data.mode;
    }

    public ParticleSystem.MinMaxGradient GetData()
    {
        var mmg= new ParticleSystem.MinMaxGradient();
        mmg.colorMax = colorMax;
        mmg.colorMin = colorMin;
        mmg.gradientMax = gradientMax;
        mmg.gradientMin = gradientMin;
        mmg.mode = mode;
        return mmg;
    }
}
