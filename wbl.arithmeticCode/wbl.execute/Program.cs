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
using wbl.algorithmImplementation.Helper;
using wbl.workTest.Service;

namespace wbl.execute
{
    public class Program
    {
        static void Main(string[] args)
        {
            // 项目文件对比csproj版本与packages版本一致性();
            分析Redis内存key情况V1();
        }

        private static void 分析Redis内存key情况V1()
        {
            var userPermissionRedisKeys = FileHelperOperate.GetFileContent("UserPermission.csv");

            var beisencacheDic = new Dictionary<string, long>();
            var tenantBeisencacheSumData = new Dictionary<int, long>();
            int weizhil = 0;
            foreach (var userPermissionRedisKey in userPermissionRedisKeys)
            {
                // 第一行数据不管
                if (weizhil++ == 0)
                    continue;
                // type,keyspace,tenant_id,key,size_in_bytes,num_elements,client_provider,expire_datetime
                var fileDatas = userPermissionRedisKey.Split(',');
                int tenant_id = int.Parse(fileDatas[2]);
                int size_in_bytes = int.Parse(fileDatas[4]);
                if (tenantBeisencacheSumData.TryGetValue(tenant_id, out long tenant_idsize_in_bytes))
                    tenantBeisencacheSumData[tenant_id] = (size_in_bytes + tenant_idsize_in_bytes);
                else
                    tenantBeisencacheSumData[tenant_id] = size_in_bytes;

                var keySplit = fileDatas[3].Split('_');
                if (keySplit[0] == "0")
                {
                    // 0_611337_MenuDto_c3fe1c4e-58d7-449b-b237-4c20cda139a3
                    if (beisencacheDic.TryGetValue(keySplit[2], out long caheTypesize_in_bytes))
                        beisencacheDic[keySplit[2]] = caheTypesize_in_bytes + size_in_bytes;
                    else
                        beisencacheDic[keySplit[2]] = size_in_bytes;
                }
                else
                {
                    var keySplitV2 = fileDatas[3].Split(':');
                    if (beisencacheDic.TryGetValue(keySplitV2[0], out long caheTypesize_in_bytesV2))
                        beisencacheDic[keySplitV2[0]] = caheTypesize_in_bytesV2 + size_in_bytes;
                    else
                        beisencacheDic[keySplitV2[0]] = size_in_bytes;
                }

                if (weizhil % 1000 == 0)
                {
                    // 一百条数据打一次日志
                    SyncHandlerBase.DebugLog(userPermissionRedisKey);
                }
            }

            // 排序
            var tenantBeisencacheSumDataList = tenantBeisencacheSumData.OrderByDescending(x => x.Value).Select(_ => string.Format("CacheType: {0}    size_in_bytes:{1}", _.Key, _.Value)).ToList();
            var beisencacheDicList = beisencacheDic.OrderByDescending(x => x.Value).Select(_ => string.Format("CacheType: {0}    size_in_bytes:{1}", _.Key, _.Value)).ToList();
            SyncHandlerBase.DebugLog("写入文件");
            FileHelperOperate.WriteFileContent("tenantBeisencacheSumDataList.txt", tenantBeisencacheSumDataList);
            FileHelperOperate.WriteFileContent("beisencacheDicList.txt", beisencacheDicList);

            SyncHandlerBase.DebugLog("执行完毕查看数据");
        }

        private static void 项目文件对比csproj版本与packages版本一致性()
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
