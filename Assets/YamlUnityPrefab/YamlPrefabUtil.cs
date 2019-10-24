using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Assets.YamlUnityPrefab
{
    public class YamlPrefabUtil
    {
        public static YamlPrefab CreateWithPrefab(GameObject go)
        {
            var pt = PrefabUtility.GetPrefabType(go);
            GameObject prefabAsset = null;
            switch (pt)
            {
                case PrefabType.None:
                    Debug.LogError("不是预制体");
                    break;
                case PrefabType.Prefab:
                    prefabAsset = go;
                    break;
                case PrefabType.ModelPrefab:
                    Debug.LogError("不支持");
                    break;
                case PrefabType.PrefabInstance:
                    prefabAsset = PrefabUtility.GetPrefabParent(go) as GameObject;
                    break;
                case PrefabType.ModelPrefabInstance:
                    Debug.LogError("不支持");
                    break;
                case PrefabType.MissingPrefabInstance:
                    Debug.LogError("不支持");
                    break;
                case PrefabType.DisconnectedPrefabInstance:
                    Debug.LogError("不支持");
                    break;
                case PrefabType.DisconnectedModelPrefabInstance:
                    Debug.LogError("不支持");
                    break;
                default:
                    return null;
            }
            //保证prefabAsset是根
            prefabAsset = PrefabUtility.FindPrefabRoot(prefabAsset);
            string prefabAssetPath = AssetDatabase.GetAssetPath(prefabAsset);
            //Debug.LogWarning(pt);
            //Debug.Log(string.Format("prefab name:{0}, path:{1}",prefab.name,prefabAssetPath));
            return new YamlPrefab(prefabAssetPath);
        }
    }
}
