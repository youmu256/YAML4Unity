using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.YamlUnityPrefab
{
    
    public class YamlPrefabTest : MonoBehaviour
    {
        public GameObject Target;
        public TestYaml DataSource;
        [ContextMenu("Apply Modify")]
        public void Modify()
        {
            var yamlPrefab = YamlPrefabUtil.CreateWithPrefab(Target);
            YamlParticleSystem ps = yamlPrefab.FinGameObject("P2").GetComponment<YamlParticleSystem>();
            ps.MainModule.StartColor.Apply(DataSource.StartColor.GetData());
            ps.MainModule.StartSize.Apply(DataSource.StartSize.GetData());
            string path = yamlPrefab.GetPrefabAssetPath();
            Debug.LogWarning("save to ===> " +path);
            yamlPrefab.Save(path);
        }
    }
}
