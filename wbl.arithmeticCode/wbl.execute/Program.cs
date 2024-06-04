using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using wbl.workTest.Service;

namespace wbl.execute
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("输入对比文件夹的绝对路径：");
            string basePath = Console.ReadLine();            
            List<Tuple<string, string, string>> tuples = Main1FileName(basePath);


            foreach (var item in tuples)
            {
                Console.WriteLine(item.Item1);
                string csprojPath = item.Item2;
                string packagesConfigPath = item.Item3;
                // 读取 csproj 文件中的 DLL 引用信息
                Dictionary<string, string> csprojDllReferences = GetDllReferencesFromCsproj(csprojPath);

                // 读取 packages.config 文件中的 DLL 引用信息
                Dictionary<string, string> packagesConfigDllReferences = GetDllReferencesFromPackagesConfig(packagesConfigPath);

                // 比较两个字典中的 DLL 版本是否一致
                foreach (var kvp in csprojDllReferences)
                {
                    string dllName = kvp.Key;
                    string csprojVersion = kvp.Value;

                    if (!packagesConfigDllReferences.ContainsKey(dllName))
                    {
                        Console.WriteLine($"警告：csproj 文件中引用的 DLL '{dllName}' 在 packages.config 文件中未找到。");
                        continue;
                    }

                    string packagesConfigVersion = packagesConfigDllReferences[dllName];

                    if (csprojVersion != packagesConfigVersion)
                    {
                        Console.WriteLine($"错误：csproj 文件中引用的 DLL '{dllName}' 版本 ({csprojVersion}) 与 packages.config 文件中的版本 ({packagesConfigVersion}) 不一致。");
                    }
                }

                // 比较两个字典中的 DLL 版本是否一致
                foreach (var kvp in packagesConfigDllReferences)
                {
                    string dllName = kvp.Key;
                    string packagesConfigVersion = kvp.Value;

                    if (!csprojDllReferences.ContainsKey(dllName))
                    {
                        Console.WriteLine($"警告：packages.config 文件中引用的 DLL '{dllName}' 在 csproj 文件中未找到。");
                        continue;
                    }

                    string csprojVersion = csprojDllReferences[dllName];

                    if (csprojVersion != packagesConfigVersion)
                    {
                        Console.WriteLine($"错误：packages.config 文件中引用的 DLL '{dllName}' 版本 ({packagesConfigVersion}) 与 csproj 文件中的版本 ({csprojVersion}) 不一致。");
                    }
                }
            }
            
        }

        public static List<Tuple<string,string, string>> Main1FileName(string basePath)
        {            
            string[] directories = Directory.GetDirectories(basePath);
            var result = new List<Tuple<string, string, string>>();
            foreach (string directory in directories)
            {
                var allFiles = Directory.GetFiles(directory);
                string csprojPath = "";
                string packagesConfigPath = "";
                foreach (var item in allFiles)
                {
                    if (item.Contains(".csproj"))
                    {
                        csprojPath = item;
                    }
                    if (item.Contains("packages.config"))
                    {
                        packagesConfigPath = item;
                    }
                }

                if (string.IsNullOrEmpty(csprojPath) || string.IsNullOrEmpty(packagesConfigPath))
                {
                    Console.WriteLine($"directory：{directory},csprojPath:{csprojPath},packagesConfigPath:{packagesConfigPath},一层中没有明确对比数据");
                }
                else
                {
                    Tuple<string, string, string> tuple = new Tuple<string, string, string>(directory, csprojPath, packagesConfigPath);
                    result.Add(tuple);
                }
            }
            return result;
        }


        private static Dictionary<string, string> GetDllReferencesFromCsproj(string csprojPath)
        {
            Dictionary<string, string> dllReferences = new Dictionary<string, string>();

            using (XmlReader reader = XmlReader.Create(csprojPath))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "HintPath")
                    {
                        //FileVersionInfo.GetVersionInfo
                        var hintPath = reader.ReadElementContentAsString().Trim();
                        if (hintPath.Contains("\\lib"))
                        {
                            Regex Regex = new Regex(@"packages\\(.*?)\\lib");
                            Match match = Regex.Match(hintPath);
                            var versionPath = match.Groups[1].Value;
                            Regex versionRegex = new Regex(@"(\.\d+){2,5}\b");
                            
                            Match versioMatch = versionRegex.Match(versionPath);
                            
                            string version = versioMatch.Value;
                            if (versionPath.Contains("-alpha"))
                            {
                                version = version + "-alpha";
                            }
                            string include = versionPath.Replace(version,"");
                            version = version.Substring(1);
                            if (!string.IsNullOrEmpty(include) && !string.IsNullOrEmpty(version))
                            {
                                dllReferences[include] = version;
                            }
                        }                                                                 
                    }
                }
            }

            return dllReferences;
        }

        private static Dictionary<string, string> GetDllReferencesFromPackagesConfig(string packagesConfigPath)
        {
            Dictionary<string, string> dllReferences = new Dictionary<string, string>();

            using (XmlReader reader = XmlReader.Create(packagesConfigPath))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "package")
                    {
                        string id = reader.GetAttribute("id");
                        string version = reader.GetAttribute("version");

                        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(version))
                        {
                            dllReferences[id] = version;
                        }
                    }
                }
            }

            return dllReferences;
        }

    }
}
