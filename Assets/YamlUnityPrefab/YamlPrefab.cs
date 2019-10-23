using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Assets.Util;
using UnityEngine;
using YamlDotNet.RepresentationModel;

namespace Assets.YamlUnityPrefab
{

    public class YamlObjectUtil
    {
        //m_ObjectHideFlags: 0  是YamlScalarNode
        //m_PrefabParentObject: {fileID: 0} 是mapping node 然后 映射一个 是YamlScalarNode

        public static string GetContentWithoutBrace(string content)
        {
            //实际上不是去花括号，而是提取内容
            string pattern = @"\b.*\b";
            Regex regex = new Regex(pattern);
            return regex.Match(content).Value;
        }

        public static YamlMappingNode GetMappingValueNode(YamlMappingNode parentNode = null)
        {
            return GetMappingValueNode<YamlMappingNode>(parentNode);
        }

        public static T GetMappingValueNode<T>(YamlMappingNode parentNode = null) where T : YamlNode
        {
            if (parentNode == null)
            {
                return null;
            }
            return parentNode.Children.First().Value as T;
        }

        public static YamlNode SearchMappingChildNode(YamlMappingNode parentNode, string key)
        {
            return SearchMappingChildNode<YamlNode>(parentNode, key);
        }

        /// <summary>
        /// mapping node中找到 kv对中的v并且作为一个节点返回
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parentNode"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T SearchMappingChildNode<T>(YamlMappingNode parentNode, string key) where T : YamlNode
        {
            YamlNode node = null;
            if (!parentNode.Children.TryGetValue(key, out node))
            {
                Debug.LogError("not find the node : " + key);
            }
            return node as T;
        }

    }

    public class YamlObjectFactory
    {
        public static YamlUnityObject Create(string type)
        {
            YamlUnityObject obj = null;
            switch (type)
            {
                case "GameObject":
                    obj = new YamlGameObject();
                    break;
                case "Transform":
                    obj = new YamlTransform();
                    break;
                case "ParticleSystem":
                    obj = new YamlParticleSystem();
                    break;
                //--其他的不管了
            }
            if (obj != null)
                obj.TypeName = type;
            return obj;
        }


    }

    public class YamlUnityObject
    {

        //Prefab GameObject ParticleSystem ...
        public string TypeName;
        public string TypeId;
        public string LocalId;
        public YamlMappingNode YamlNode { get; private set; }

        public void SetYamlNode(YamlMappingNode node)
        {
            YamlNode = node;
        }

        /// <summary>
        /// gameobject 初始化后会调用
        /// 组件初始化
        /// 此时还没有层级概念
        /// </summary>
        /// <param name="context"></param>
        public virtual void YamlInit(YamlDataContext context)
        {
        }

        public YamlGameObject AttachGameObject { get; private set; } //GameObject的话就是自己

        public string ObjectName
        {
            get { return AttachGameObject != null ? AttachGameObject.Name : "null"; }
        }

        public void SetAttachGameObject(YamlGameObject obj)
        {
            AttachGameObject = obj;
        }

        public string GetAttachId()
        {
            //解析出ATTACH ID
            //Debug.LogWarning(YamlNode.ToString());
            string attachId = YamlNode.Children.First().Value["m_GameObject"]["fileID"].ToString();
            attachId = YamlObjectUtil.GetContentWithoutBrace(attachId);
            //Debug.LogWarning(attachId);
            return attachId;
        }

        public void SetLocalInfo(string typeId, string localId)
        {
            TypeId = typeId;
            LocalId = localId;
        }

        public void ModifyNode(string kName, string v)
        {
            var p = YamlObjectUtil.GetMappingValueNode(YamlNode);
            if (p == null)
            {
                return;
            }
            YamlNode node = null;
            if (p.Children.TryGetValue(kName, out node))
            {
                YamlScalarNode scal = node as YamlScalarNode;
                if (scal == null) return;
                string ori = scal.Value;
                scal.Value = v;
                Debug.Log(string.Format("{0} : value modify : {1}->{2}", kName, ori, v));
            }
            else
            {
                Debug.LogError("modify node error: " + kName);
            }
        }

        public string GetNodeString(string kName)
        {
            var p = YamlObjectUtil.GetMappingValueNode(YamlNode);
            if (p == null)
            {
                return "";
            }
            YamlNode node = null;
            if (p.Children.TryGetValue(kName, out node))
            {
                YamlScalarNode scal = node as YamlScalarNode;
                if (scal == null) return "";
                return scal.Value;
            }
            return "";
        }

    }

    public class YamlTransform : YamlComponment
    {
        public YamlTransform Parent { get; set; }
        public List<YamlTransform> Children { get; private set; }

        public YamlSequenceNode ChildrenNode { get; private set; }

        public YamlScalarNode Father { get; private set; }

        public YamlGameObject GameObject
        {
            get { return AttachGameObject; }
        }

        public bool IsContainsChild(string localId)
        {
            foreach (var yamlTransform in Children)
            {
                if (yamlTransform.LocalId == localId)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsRootTransform()
        {
            // m_Father 的引用是0的话，就是根了
            return Father.Value == "0";
        }

        public override void YamlInit(YamlDataContext context)
        {
            base.YamlInit(context);
            var p = YamlObjectUtil.GetMappingValueNode<YamlMappingNode>(YamlNode);
            if (p != null)
            {
                ChildrenNode = YamlObjectUtil.SearchMappingChildNode<YamlSequenceNode>(p, "m_Children");
            }
            var fatherNode = YamlObjectUtil.SearchMappingChildNode<YamlMappingNode>(p, "m_Father");
            //Debug.LogError(fatherNode);
            Father = YamlObjectUtil.SearchMappingChildNode<YamlScalarNode>(fatherNode, "fileID");

            Children = new List<YamlTransform>();
            foreach (YamlMappingNode cn in ChildrenNode)
            {
                var c = YamlObjectUtil.SearchMappingChildNode<YamlScalarNode>(cn, "fileID");
                YamlUnityObject yo = null;
                if (context.ObjectIdMap.TryGetValue(c.Value, out yo))
                {
                    //Debug.LogWarning("ADD CHILD :" + yo.ObjectName + " => " +ObjectName);
                    Children.Add(yo as YamlTransform);
                }
            }
        }

        /// <summary>
        /// 初始化父子关系
        /// </summary>
        /// <param name="context"></param>
        public void RootTransofrmInit(YamlDataContext context)
        {
            //--从root transform 开始 往下找
            List<YamlTransform> allTransforms = new List<YamlTransform>();
            foreach (var pair in context.ObjectIdMap)
            {
                var t = pair.Value as YamlTransform;
                if (t != null)
                    allTransforms.Add(t);
            }
            if (IsRootTransform())
            {
                FindChildren(this, allTransforms);
            }
        }

        void FindChildren(YamlTransform parent, List<YamlTransform> allTransforms)
        {
            //如果子还有子，则继续Find
            //算法效率有点低....
            foreach (var transform in allTransforms)
            {
                if (parent.IsContainsChild(transform.LocalId))
                {
                    transform.Parent = parent;
                    if (transform.HasAnyChild())
                    {
                        FindChildren(transform, allTransforms);
                    }
                }
            }
        }

        public YamlTransform FindTransoform(string childName)
        {
            foreach (var child in Children)
            {
                if (child.ObjectName == childName)
                {
                    return child;
                }
            }
            return null;
        }

        public bool HasAnyChild()
        {
            return Children.Count > 0;
        }


    }

    public class YamlGameObject : YamlUnityObject
    {
        public string Name
        {
            get { return NameNode.Value; }
            set { NameNode.Value = value; }
        }

        public List<YamlComponment> Componments { get; private set; }
        public YamlTransform Transofrm { get; private set; }

        public YamlScalarNode NameNode { get; private set; }

        public YamlSequenceNode ComponmentNode { get; private set; }

        /// <summary>
        /// GameObject初始化-然后再让所有的组件初始化
        /// </summary>
        /// <param name="context"></param>
        public void GameObjectInit(YamlDataContext context)
        {
            YamlInit(context);
            foreach (var componment in Componments)
            {
                componment.YamlInit(context);
            }
        }
        
        public T GetComponment<T>() where T : YamlComponment
        {
            foreach (var yamlComponment in Componments)
            {
                if (yamlComponment.GetType() == typeof(T))
                {
                    return yamlComponment as T;
                }
            }
            return null;
        }

        public override void YamlInit(YamlDataContext context)
        {
            base.YamlInit(context);

            var p = YamlObjectUtil.GetMappingValueNode(YamlNode);
            if (p == null)
            {
                return;
            }
            NameNode = YamlObjectUtil.SearchMappingChildNode<YamlScalarNode>(p, "m_Name");
            ComponmentNode = YamlObjectUtil.SearchMappingChildNode<YamlSequenceNode>(p, "m_Component");

            Componments = new List<YamlComponment>();

            foreach (var c in ComponmentNode.Children)
            {
                YamlMappingNode child = c as YamlMappingNode;
                if (child == null) continue;
                YamlKeyLocalRef r = new YamlKeyLocalRef(child);
                YamlUnityObject yo = null;
                if (context.ObjectIdMap.TryGetValue(r.RefId, out yo))
                {
                    Componments.Add(yo as YamlComponment);
                }
            }
            Transofrm = GetComponment<YamlTransform>();
        }
        

    }

    public class YamlComponment : YamlUnityObject
    {

    }

    public class YamlParticleSystem : YamlComponment
    {
        public YamlMappingNode InitialModuleNode; //已经是value node

        public YamlParticleInitialModule InitialModule;

        public YamlParticleInitialModule MainModule
        {
            get { return InitialModule; }
        }


        public YamlScalarNode PrewarmNode;
        public YamlScalarNode LengthInSecNode;

        public YamlMinMaxCurve StartDelay;


        public bool Prewarm
        {
            get { return PrewarmNode.Value == "1" ? true : false; }
            set { PrewarmNode.Value = value ? "1" : "0"; }
        }

        public float LengthInSec
        {
            get { return float.Parse(LengthInSecNode.Value); }
            set { LengthInSecNode.Value = value.ToString(); }
        }

        public override void YamlInit(YamlDataContext context)
        {
            base.YamlInit(context);
            var p = YamlObjectUtil.GetMappingValueNode(YamlNode);
            if (p == null)
            {
                return;
            }
            YamlNode node = null;
            if (p.Children.TryGetValue("InitialModule", out node))
            {
                InitialModuleNode = node as YamlMappingNode;
                InitialModule = new YamlParticleInitialModule(InitialModuleNode);
            }
            StartDelay = new YamlMinMaxCurve(YamlObjectUtil.SearchMappingChildNode<YamlMappingNode>(p, "startDelay"));
            PrewarmNode = YamlObjectUtil.SearchMappingChildNode<YamlScalarNode>(p, "prewarm");
            LengthInSecNode = YamlObjectUtil.SearchMappingChildNode<YamlScalarNode>(p, "lengthInSec");
        }
    }

    public class YamlParticleInitialModule
    {
        public YamlMappingNode InitialModuleNode;

        public YamlMinMaxCurve StartLifeTime;
        public YamlMinMaxCurve StartSpeed;
        public YamlMinMaxCurve StartSize;
        public YamlMinMaxCurve StartRotationX;
        public YamlMinMaxCurve StartRotationY;
        public YamlMinMaxCurve StartRotation;
        public YamlMinMaxGradient StartColor;

        public YamlScalarNode Rotation3DNode;
        public YamlScalarNode MaxNumParticlesNode;
        //randomizeRotationDirection
        //gravityModifier


        public bool Rotation3D
        {
            get { return Rotation3DNode.Value == "1" ? true : false; }
            set { Rotation3DNode.Value = value ? "1" : "0"; }
        }

        public int MaxNumParticles
        {
            get { return int.Parse(MaxNumParticlesNode.Value); }
            set { MaxNumParticlesNode.Value = value.ToString(); }
        }

        public YamlMappingNode GetNode(string key)
        {
            return YamlObjectUtil.SearchMappingChildNode<YamlMappingNode>(InitialModuleNode, key);
        }

        public YamlParticleInitialModule(YamlMappingNode mainNode)
        {
            InitialModuleNode = mainNode;
            StartLifeTime = new YamlMinMaxCurve(GetNode("startLifetime"));
            StartSpeed = new YamlMinMaxCurve(GetNode("startSpeed"));
            //color...
            StartColor = new YamlMinMaxGradient(GetNode("startColor"));
            StartSize = new YamlMinMaxCurve(GetNode("startSize"));
            StartRotationX = new YamlMinMaxCurve(GetNode("startRotationX"));
            StartRotationY = new YamlMinMaxCurve(GetNode("startRotationY"));
            StartRotation = new YamlMinMaxCurve(GetNode("startRotation"));
            Rotation3DNode = YamlObjectUtil.SearchMappingChildNode<YamlScalarNode>(InitialModuleNode, "rotation3D");
            MaxNumParticlesNode = YamlObjectUtil.SearchMappingChildNode<YamlScalarNode>(InitialModuleNode,"maxNumParticles");
        }
    }

    public class YamlKeyFrame : IComparable<YamlKeyFrame>
    {
        public YamlScalarNode Time { get; private set; }
        public YamlScalarNode Value { get; private set; }
        public YamlScalarNode InSlope { get; private set; }
        public YamlScalarNode OutSlope { get; private set; }
        public YamlScalarNode TangentMode { get; private set; }

        public YamlKeyFrame(YamlNode main)
        {
            var p = main as YamlMappingNode;
            YamlNode node = null;
            if (p.Children.TryGetValue("time", out node))
            {
                Time = node as YamlScalarNode;
            }
            if (p.Children.TryGetValue("value", out node))
            {
                Value = node as YamlScalarNode;
            }
            if (p.Children.TryGetValue("inSlope", out node))
            {
                InSlope = node as YamlScalarNode;
            }
            if (p.Children.TryGetValue("outSlope", out node))
            {
                OutSlope = node as YamlScalarNode;
            }
            if (p.Children.TryGetValue("tangentMode", out node))
            {
                TangentMode = node as YamlScalarNode;
            }
        }

        public YamlKeyFrame(Keyframe keyframe)
        {
            ApplyKeyFrameData(keyframe);
        }

        public void ApplyKeyFrameData(Keyframe keyframe)
        {
            Time = new YamlScalarNode(keyframe.time.ToString());
            Value = new YamlScalarNode(keyframe.value.ToString());
            InSlope = new YamlScalarNode(keyframe.inTangent.ToString());
            OutSlope = new YamlScalarNode(keyframe.outTangent.ToString());
            TangentMode = new YamlScalarNode(keyframe.tangentMode.ToString());
        }

        public YamlNode ToYamlNode()
        {
            List<KeyValuePair<YamlNode, YamlNode>> list = new List<KeyValuePair<YamlNode, YamlNode>>();
            list.Add(new KeyValuePair<YamlNode, YamlNode>("time", Time));
            list.Add(new KeyValuePair<YamlNode, YamlNode>("value", Value));
            list.Add(new KeyValuePair<YamlNode, YamlNode>("inSlope", InSlope));
            list.Add(new KeyValuePair<YamlNode, YamlNode>("outSlope", OutSlope));
            list.Add(new KeyValuePair<YamlNode, YamlNode>("tangentMode", TangentMode));
            foreach (var pair in list)
            {
                if (pair.Value == null)
                {
                    Debug.LogError(pair.Key + " Value is null");
                }
            }
            var node = new YamlMappingNode(list);
            //Debug.LogWarning("KEY FRAME NODE : " + node);
            return node;
        }

        public float GetTime()
        {
            return float.Parse(Time.Value.ToString());
        }

        public int CompareTo(YamlKeyFrame other)
        {
            float diff = this.GetTime() - other.GetTime();
            if (diff > 0)
            {
                return 1;
            }
            else if (diff < 0)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }

    public class YamlAnimationCurve
    {
        //--数据源--
        public YamlSequenceNode CurveFrames { get; private set; }

        //---方便操作修改Yaml的辅助类--
        public List<YamlKeyFrame> EditorKeyFrames { get; private set; }

        public YamlAnimationCurve(YamlMappingNode mainNode)
        {
            YamlNode node = null;
            if (mainNode.Children.TryGetValue("m_Curve", out node))
            {
                CurveFrames = node as YamlSequenceNode;
            }
            EditorKeyFrames = new List<YamlKeyFrame>();
            foreach (var keyFrame in CurveFrames.Children)
            {
                EditorKeyFrames.Add(new YamlKeyFrame(keyFrame));
            }
            EditorKeyFrames.Sort();
            UpdateCurveFrames();
        }

        //更新YAML真实数据
        void UpdateCurveFrames()
        {
            if(stopUpdate)return;
            CurveFrames.Children.Clear();
            foreach (var key in EditorKeyFrames)
            {
                CurveFrames.Add(key.ToYamlNode());
            }
        }
        
        public void AddKeyFrame(Keyframe keyframe)
        {
            float timeKey = keyframe.time;
            YamlKeyFrame existYamlKeyFrame = null;
            foreach (var editorKeyFrame in EditorKeyFrames)
            {
                if (Math.Abs(editorKeyFrame.GetTime() - timeKey) < 0.001f)
                {
                    existYamlKeyFrame = editorKeyFrame;
                    break;
                }
            }
            if (existYamlKeyFrame == null)
            {
                EditorKeyFrames.Add(new YamlKeyFrame(keyframe));
                EditorKeyFrames.Sort();
            }
            else
            {
                existYamlKeyFrame.ApplyKeyFrameData(keyframe);
            }
            UpdateCurveFrames();
        }

        private bool stopUpdate = false;
        public void Apply(AnimationCurve curve)
        {
            stopUpdate = true;
            foreach (var keyframe in curve.keys)
            {
                AddKeyFrame(keyframe);
            }
            stopUpdate = false;
            UpdateCurveFrames();
        }

    }

    public class YamlColor
    {
        public Color ColorHelper
        {
            get { return YamlColorString2ColorRGBA(ColorNode.Value); }
            set { ColorNode.Value = GetYamlColorString(value); }
        }

        public float Alpha
        {
            get { return ColorHelper.a; }
            set { ColorHelper = new Color(ColorHelper.r, ColorHelper.g, ColorHelper.b,value); }
        }

        public Color Color
        {
            get {return new Color(ColorHelper.r,ColorHelper.g,ColorHelper.b,1);}
            set { ColorHelper = new Color(value.r,value.g,value.b,ColorHelper.a);}
        }

        public static Color YamlColorString2ColorRGBA(string colorStr)
        {
            Color color = Color.white;
            Int64 c = Int64.Parse(colorStr);
            string csr = Convert.ToString(c, 16);
            int size = 8 - csr.Length;
            for (int i = 0; i < size; i++)
            {
                csr = "0" + csr;
            }
            //...ABGR -> RGBA顺序反转...可以优化字符串
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
            csr = "#" + csr;
            ColorUtility.TryParseHtmlString(csr, out color);
            return color;
        }

        public static string GetYamlColorString(Color color)
        {
            string csr = ColorUtility.ToHtmlStringRGBA(color);
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
            return csr;
        }

        public YamlScalarNode ColorNode;
        //16进制转成10进制 RGBA
        public YamlColor(YamlMappingNode mainNode)
        {
            ColorNode = YamlObjectUtil.SearchMappingChildNode<YamlScalarNode>(mainNode, "rgba");
        }

        public YamlColor(Color color)
        {
            ColorNode = new YamlScalarNode(GetYamlColorString(color));

        }

        public YamlNode ToYamlNode()
        {
            List<KeyValuePair<YamlNode, YamlNode>> list = new List<KeyValuePair<YamlNode, YamlNode>>();
            list.Add(new KeyValuePair<YamlNode, YamlNode>("serializedVersion", "2"));
            list.Add(new KeyValuePair<YamlNode, YamlNode>("rgba", ColorNode));
            foreach (var pair in list)
            {
                if (pair.Value == null)
                {
                    Debug.LogError(pair.Key + " Value is null");
                }
            }
            var node = new YamlMappingNode(list);
            return node;
        }

    }
    public class YamlGradient
    {
        public YamlMappingNode MainNode;
        public YamlScalarNode NumColorKeysNode;
        public YamlScalarNode NumAlphaKeysNode;
        
        public List<YamlColor> ColorList;
        public List<YamlScalarNode> CtimeKeyListNode;
        public List<YamlScalarNode> AtimeKeyListNode;


        public int NumColorKeys
        {
            get { return int.Parse(NumColorKeysNode.Value); }
            set { NumColorKeysNode.Value = value.ToString(); }
        }

        public int NumAlphaKeys
        {
            get { return int.Parse(NumAlphaKeysNode.Value); }
            set { NumAlphaKeysNode.Value = value.ToString(); }
        }

        const int MaxKeyLength = 8;

        public void SetAlphaKey(GradientAlphaKey colorKey)
        {
            int time = Mathf.CeilToInt(colorKey.time * 65535);
            int length = NumAlphaKeys;
            if(length>=MaxKeyLength)return;

            YamlScalarNode existNode = null;
            int existIndex = -1;
            int insertIndex = length;
            for (int i = 0; i < length; i++)
            {
                var node = AtimeKeyListNode[i];
                int t = int.Parse(node.Value);
                if (t == time)
                {
                    existNode = node;
                    existIndex = i;
                }
                else if (t < time)
                {
                    insertIndex = i + 1;
                }
            }
            if (existNode != null)
            {
                existNode.Value = time.ToString();
            }
            else
            {
                //add new
                int newIndex = insertIndex;
                if (newIndex < length)
                {
                    //insert往后挪
                    for (int i = newIndex; i < length; i++)
                    {
                        AtimeKeyListNode[newIndex + 1].Value = AtimeKeyListNode[newIndex].Value;
                    }
                }
                //Debug.Log("SetAlphaKey : " + time +" At : "+newIndex +" Length:"+length);
                AtimeKeyListNode[newIndex].Value = time.ToString();
                NumAlphaKeys++;
                existIndex = newIndex;
            }
            //。。。只更新颜色里的ALPHA...
            ColorList[existIndex].Alpha = colorKey.alpha;
            UpdateRealNode();
        }
        public void SetColorKey(GradientColorKey colorKey)
        {
            int time = Mathf.CeilToInt(colorKey.time * 65535);
            //Debug.Log("SetColorKey : " + time );

            int length = NumColorKeys;
            if(length>=MaxKeyLength)return;
            
            YamlScalarNode existNode = null;
            int existIndex = -1;
            int insertIndex = length;
            for (int i = 0; i < length; i++)
            {
                var node = CtimeKeyListNode[i];
                int t = int.Parse(node.Value);
                if (t == time)
                {
                    existNode = node;
                    existIndex = i;
                }else if (t <time)
                {
                    insertIndex = i+1;
                }
            }
            if (existNode != null)
            {
                existNode.Value = time.ToString();
            }
            else
            {
                //add new
                int newIndex = insertIndex;
                if (newIndex < length)
                {
                    //insert往后挪
                    for (int i = newIndex; i < length; i++)
                    {
                        CtimeKeyListNode[newIndex + 1].Value = CtimeKeyListNode[newIndex].Value;
                    }
                }
                CtimeKeyListNode[newIndex].Value = time.ToString();
                NumColorKeys++;
                existIndex = newIndex;
            }
            //。。。只更新颜色里的rgb
            ColorList[existIndex].Color = colorKey.color;
            UpdateRealNode();
        }

        public void UpdateRealNode()
        {
            if(stopUpdate)return;
            MainNode = ToYamlNode();
        }

        public YamlMappingNode ToYamlNode()
        {
            List<KeyValuePair<YamlNode, YamlNode>> list = new List<KeyValuePair<YamlNode, YamlNode>>();
            for (int i = 0; i < MaxKeyLength; i++)
            {
                string key = "key" + i;
                //string akey = "atime" + i;
                //string ckey = "ctime" + i;
                list.Add(new KeyValuePair<YamlNode, YamlNode>(key,ColorList[i].ToYamlNode()));
            }
            for (int i = 0; i < MaxKeyLength; i++)
            {
                //string key = "key" + i;
                //string akey = "atime" + i;
                string ckey = "ctime" + i;
                list.Add(new KeyValuePair<YamlNode, YamlNode>(ckey, CtimeKeyListNode[i].Value));
            }
            for (int i = 0; i < MaxKeyLength; i++)
            {
                //string key = "key" + i;
                string akey = "atime" + i;
                //string ckey = "ctime" + i;
                list.Add(new KeyValuePair<YamlNode, YamlNode>(akey, AtimeKeyListNode[i].Value));
            }
            foreach (var pair in list)
            {
                if (pair.Value == null)
                {
                    Debug.LogError(pair.Key + " Value is null");
                }
            }
            var node = new YamlMappingNode(list);
            return node;
        }

        public YamlGradient(YamlMappingNode mainNode)
        {
            /*
            foreach (var pair in mainNode.Children)
            {
                Debug.LogWarning(pair.Key + " @ " + pair.Value);
            }
            */
            MainNode = mainNode;
            NumColorKeysNode = YamlObjectUtil.SearchMappingChildNode<YamlScalarNode>(mainNode, "m_NumColorKeys");
            NumAlphaKeysNode = YamlObjectUtil.SearchMappingChildNode<YamlScalarNode>(mainNode, "m_NumAlphaKeys");
            int length = MaxKeyLength;
            ColorList = new List<YamlColor>(length);
            CtimeKeyListNode = new List<YamlScalarNode>(length);
            AtimeKeyListNode = new List<YamlScalarNode>(length);
            for (int i = 0; i < length; i++)
            {
                string key = "key" + i;
                string akey = "atime" + i;
                string ckey = "ctime" + i;
                var kcNode = YamlObjectUtil.SearchMappingChildNode<YamlMappingNode>(mainNode, key);
                var cNode = YamlObjectUtil.SearchMappingChildNode<YamlScalarNode>(mainNode, ckey);
                var aNode = YamlObjectUtil.SearchMappingChildNode<YamlScalarNode>(mainNode, akey);
                ColorList.Add(new YamlColor(kcNode));
                CtimeKeyListNode.Add(cNode);
                AtimeKeyListNode.Add(aNode);
            }
        }

        private bool stopUpdate = false;
        public void Apply(Gradient gradient)
        {
            stopUpdate = true;
            //--=0相当于在SetKey的时候都在新增key--
            NumAlphaKeys = 0;
            NumColorKeys = 0;
            foreach (var alphaKey in gradient.alphaKeys)
            {
                SetAlphaKey(alphaKey);
            }
            foreach (var colorKey in gradient.colorKeys)
            {
                SetColorKey(colorKey);
            }
            stopUpdate = false;
            UpdateRealNode();
        }
    }

    public class YamlMinMaxGradient
    {
        public YamlMappingNode MainNode { get; private set; }

        public YamlGradient MaxGradient;
        public YamlGradient MinGradient;
        public YamlColor MinColor;
        public YamlColor MaxColor;
        public YamlScalarNode MinMaxStateNode;

        public void Apply(ParticleSystem.MinMaxGradient gradient)
        {
            MinMaxState = gradient.mode;
            MaxGradient.Apply(gradient.gradientMax);
            MinGradient.Apply(gradient.gradientMin);
            MaxColor.ColorHelper = gradient.colorMax;
            MinColor.ColorHelper = gradient.colorMin;
        }

        public YamlMinMaxGradient(YamlMappingNode mainNode)
        {
            /*
            foreach (var pair in mainNode.Children)
            {
                Debug.LogWarning(pair.Key + " @ " + pair.Value);
            }
            Debug.LogWarning("=================");
            */
            MainNode = mainNode;
            MaxGradient = new YamlGradient(YamlObjectUtil.SearchMappingChildNode<YamlMappingNode>(mainNode, "maxGradient"));
            MinGradient = new YamlGradient(YamlObjectUtil.SearchMappingChildNode<YamlMappingNode>(mainNode, "minGradient"));
            MinColor = new YamlColor(YamlObjectUtil.SearchMappingChildNode<YamlMappingNode>(mainNode, "minColor"));
            MaxColor = new YamlColor(YamlObjectUtil.SearchMappingChildNode<YamlMappingNode>(mainNode, "maxColor"));
            MinMaxStateNode = YamlObjectUtil.SearchMappingChildNode<YamlScalarNode>(mainNode, "minMaxState");
        }

        public ParticleSystemGradientMode MinMaxState
        {
            get { return (ParticleSystemGradientMode) int.Parse(MinMaxStateNode.Value); }
            set { MinMaxStateNode.Value = ((int)value).ToString(); }
        }
    }

    public class YamlMinMaxCurve
    {
        public YamlMappingNode MainNode { get; private set; }

        public YamlMinMaxCurve(YamlMappingNode mainNode)
        {
            MainNode = mainNode;
            var p = mainNode;
            YamlNode node = null;
            if (p.Children.TryGetValue("scalar", out node))
            {
                ScalarNode = node as YamlScalarNode;
            }
            if (p.Children.TryGetValue("maxCurve", out node))
            {
                MaxCurveNode = node as YamlMappingNode;
                MaxCurve = new YamlAnimationCurve(MaxCurveNode);
            }
            if (p.Children.TryGetValue("minCurve", out node))
            {
                MinCurveNode = node as YamlMappingNode;
                MinCurve = new YamlAnimationCurve(MinCurveNode);
            }
            if (p.Children.TryGetValue("minMaxState", out node))
            {
                MinMaxStateNode = node as YamlScalarNode;
            }
        }

        public void Apply(ParticleSystem.MinMaxCurve curve)
        {
            MinMaxMode = curve.mode;
            Scalar = curve.curveScalar;
            MaxCurve.Apply(curve.curveMax);
            MinCurve.Apply(curve.curveMin);
        }


        #region 数据源
        public YamlScalarNode ScalarNode;
        public YamlScalarNode MinMaxStateNode;//对应ParticleSystemCurveMode
        public YamlMappingNode MaxCurveNode;
        public YamlMappingNode MinCurveNode;
        #endregion

        #region 中间辅助 --方便直接去操作yaml数据节点--
        public YamlAnimationCurve MaxCurve { get; private set; }
        public YamlAnimationCurve MinCurve { get; private set; }
        public float Scalar
        {
            get { return float.Parse(ScalarNode.Value); }
            set { ScalarNode.Value = value.ToString(); }
        }
        public ParticleSystemCurveMode MinMaxMode
        {
            get { return (ParticleSystemCurveMode)int.Parse(MinMaxStateNode.Value); }
            set { MinMaxStateNode.Value = ((int)value).ToString(); }
        }

        public void AddKeyFrame(YamlAnimationCurve curve, Keyframe frame)
        {
            if (MinMaxMode == ParticleSystemCurveMode.Constant || MinMaxMode == ParticleSystemCurveMode.TwoConstants)
            {
                Debug.LogError("不是曲线模式 加个P的帧 Mode :" + MinMaxMode);
                return;
            }
            curve.AddKeyFrame(frame);
        }
        #endregion
        
    }

    public class YamlKeyLocalRef
    {
        public string RefId { get { return RefIdNode.Value; } }
        public YamlScalarNode RefIdNode { get; private set; }
        public YamlKeyLocalRef(YamlMappingNode node)
        {
            var p = YamlObjectUtil.GetMappingValueNode<YamlMappingNode>(node);
            if (p != null)
            {
                RefIdNode = YamlObjectUtil.SearchMappingChildNode<YamlScalarNode>(p,"fileID");
            }
        }
    }

    public class YamlDataContext
    {
        public Dictionary<string, YamlUnityObject> ObjectIdMap { get; set; }
    }

    public class YamlPrefab : YamlUnityObject
    {
        protected Dictionary<string, YamlUnityObject> ObjectIdMap = new Dictionary<string, YamlUnityObject>();

        protected List<YamlObjectInfo> InfoList;
        protected List<YamlMappingNode> NodeList;
        protected YamlStream Yaml;
        protected YamlDataContext DataContext;
        public YamlPrefab(string prefabPath)
        {
            var infoList = CollectBaseInfo(prefabPath);
            var input = new StreamReader(prefabPath, Encoding.UTF8);
            var yaml = new YamlStream();
            yaml.Load(input);
            List<YamlMappingNode> nodeList = new List<YamlMappingNode>();
            for (int i = 0; i < yaml.Documents.Count; i++)
            {
                nodeList.Add(yaml.Documents[i].RootNode as YamlMappingNode);
            }
            Parse(yaml, infoList, nodeList);
        }
        

        /// <summary>
        /// 在整个预制体里找对象-如果有同名则返回第一个搜索到的单位
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public YamlGameObject FinGameObject(string name)
        {
            if (RootGameObject.ObjectName == name)
            {
                return RootGameObject;
            }
            return SearchChild(RootGameObject,name);
        }

        YamlGameObject SearchChild(YamlGameObject node, string name)
        {
            //Debug.Log("search .. "+ node.NameNode + " filter : " + name);
            foreach (var yamlTransform in node.Transofrm.Children)
            {
                //Debug.Log("child search : " +yamlTransform.ObjectName);
                if (yamlTransform.ObjectName == name) return yamlTransform.GameObject;
            }
            foreach (var yamlTransform in node.Transofrm.Children)
            {
                var res = SearchChild(yamlTransform.GameObject, name);
                if (res != null) return res;
            }
            return null;
        }

        #region BaseINFO
        public class YamlObjectInfo
        {
            public string TypeId;
            public string LocalId;
            public string OriContent;
            public YamlObjectInfo(string content)
            {
                //---!u!1 & 149962 这种形式
                OriContent = content;
                content = content.Replace("--- !u!", "");
                var srr = content.Split('&');
                TypeId = srr[0].Trim();
                LocalId = srr[1].Trim();
            }
        }
        public static List<YamlObjectInfo> CollectBaseInfo(string prefabPath)
        {
            prefabPath = PathEx.ConvertAssetPathToAbstractPath(prefabPath);
            //读取文本
            string content = File.ReadAllText(prefabPath);//文本内容
            string pattern = @"---.*\b";
            Regex regex = new Regex(pattern);
            var lines = regex.Matches(content);
            List<YamlObjectInfo> infoList = new List<YamlObjectInfo>();
            foreach (Match match in lines)
            {
                infoList.Add(new YamlObjectInfo(match.Value));
            }
            return infoList;
        }

        #endregion

        private void Parse(YamlStream yaml, List<YamlObjectInfo> infoList, List<YamlMappingNode> nodeList)
        {
            string rootGameObjectId = "";
            DataContext = new YamlDataContext();
            //--解析 YAML 文件-- 缓存所有对象--
            Yaml = yaml;
            InfoList = infoList;
            NodeList = nodeList;
            int length = Mathf.Min(infoList.Count, nodeList.Count);
            for (int i = 0; i < length; i++)
            {
                string typeStr;
                try
                {
                    typeStr = nodeList[i].Children.First().Key.ToString();
                }
                catch (Exception)
                {
                    typeStr = "error parse type -- list node";
                    foreach (var pair in nodeList[i].Children)
                    {
                        Debug.LogWarning(pair.Key + " : " + pair.Value);
                    }
                }
                YamlUnityObject yo = YamlObjectFactory.Create(typeStr);
                if (yo != null)
                {
                    yo.SetLocalInfo(infoList[i].TypeId, infoList[i].LocalId);
                    yo.SetYamlNode(nodeList[i]);
                    ObjectIdMap.Add(yo.LocalId, yo);
                }
                if (typeStr == "Prefab")
                {
                    //是prefab!记录一下ID
                    var p = YamlObjectUtil.GetMappingValueNode(nodeList[i]);
                    YamlMappingNode r = YamlObjectUtil.SearchMappingChildNode(p, "m_RootGameObject") as YamlMappingNode;
                    if (r != null)
                    {
                        rootGameObjectId = YamlObjectUtil.SearchMappingChildNode(r, "fileID").ToString();
                    }
                    //Debug.LogWarning("root gameobject = " + rootGameObjectId);
                }
            }
            //---重新关联组件对象---
            foreach (var pair in ObjectIdMap)
            {
                //Debug.Log(string.Format("id:{0}, object:{1}", pair.Key, pair.Value.TypeName));
                var type = pair.Value.GetType();
                var obj = pair.Value;
                if (type != typeof(YamlGameObject))
                {
                    string attachId = obj.GetAttachId(); //找到attachId
                    try
                    {
                        obj.SetAttachGameObject(ObjectIdMap[attachId] as YamlGameObject);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
                else
                {
                    obj.SetAttachGameObject(obj as YamlGameObject);
                }
            }
            DataContext.ObjectIdMap = ObjectIdMap;
            //--关联结束后，初始化GOMEOBJECT--
            foreach (var pair in ObjectIdMap)
            {
                var go = pair.Value as YamlGameObject;
                if(go!=null)
                    go.GameObjectInit(DataContext);
            }

            //--设置prefab的根gameobject
            if (!string.IsNullOrEmpty(rootGameObjectId))
                RootGameObject = ObjectIdMap[rootGameObjectId] as YamlGameObject;
            RootGameObject.Transofrm.RootTransofrmInit(DataContext);
        }

        public void Save()
        {
            //---直接导出的格式Unity是不支持的，需要做一定的修改
            string filePath = "Assets/YamlOutputPrefab/test.prefab";
            using (TextWriter writer = File.CreateText(filePath))
            {
                writer.WriteLine("%YAML 1.1");
                writer.WriteLine("%TAG !u! tag:unity3d.com,2011:");
                Yaml.Save(writer,false);
            }

            //去掉结尾...
            //每个yaml document的 开头 &149962 编号前面没有内容 !u!, 不是unity的格式 替换掉
            string content = File.ReadAllText(filePath);
            content = content.Replace("...", "");
            string pattern = @".*&.*";
            var matches =Regex.Matches(content, pattern);
            for (int i = 0; i < matches.Count; i++)
            {
                string ori = matches[i].Value;
                string toReplaced = InfoList[i].OriContent;
                content = content.Replace(ori, toReplaced);
            }
            File.WriteAllText(filePath,content);
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log("======SAVE FINISH======");
        }

        public string PrefabName
        {
            get { return RootGameObject.Name; }
        }
        public YamlGameObject RootGameObject { get; private set; }
    }
}
