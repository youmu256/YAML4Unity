
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Util
{

    public class PathEx
    {
        /// <summary>
        /// 工程根目录
        /// </summary>
        public static string projectPath
        {
            get
            {
                DirectoryInfo directory = new DirectoryInfo(Application.dataPath);
                return MakePathStandard(directory.Parent.FullName);
            }
        }

        /// <summary>
        /// 使目录存在,Path可以是目录名也可以是文件名
        /// </summary>
        /// <param name="path"></param>
        public static void MakeDirectoryExist(string path)
        {
            string root = Path.GetDirectoryName(path);
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }
        }
        /// <summary>
        /// 结合目录
        /// </summary>
        public static string Combine(string left, string right)
        {
            if (File.Exists(left))
            {
                Debug.LogError(string.Format("{0} error", left));
            }
            string result = left;
            if (left.EndsWith("/") || left.Trim() == "")
            {
                result += right;
            }
            else
            {
                result += "/" + right;
            }

            result = MakePathStandard(result);
            return result;
        }

        /// <summary>
        /// 获取父文件夹
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetPathParentFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return Path.GetDirectoryName(path);
        }

        /// <summary>
        /// 将绝对路径转换为相对于Asset的路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ConvertAbstractToAssetPath(string path)
        {
            path = MakePathStandard(path);
            return MakePathStandard(path.Replace(projectPath + "/", ""));
        }

        /// <summary>
        /// 将绝对路径转换为相对于Asset的路径且去除后缀
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ConvertAbstractToAssetPathWithoutExtention(string path)
        {
            return FileEx.GetFilePathWithoutExtention(ConvertAbstractToAssetPath(path));
        }

        /// <summary>
        /// 将相对于Asset的路径转换为绝对路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ConvertAssetPathToAbstractPath(string path)
        {
            path = MakePathStandard(path);
            return Combine(projectPath, path);
        }

        /// <summary>
        /// 将相对于Asset的路径转换为绝对路径且去除后缀
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ConvertAssetPathToAbstractPathWithoutExtention(string path)
        {
            return FileEx.GetFilePathWithoutExtention(ConvertAssetPathToAbstractPath(path));
        }

        /// <summary>
        /// 使路径标准化，去除空格并将所有'\'转换为'/'
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string MakePathStandard(string path)
        {
            return path.Trim().Replace("\\", "/");
        }


    }
}
