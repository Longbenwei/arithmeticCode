using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wbl.algorithmImplementation.Helper
{
    public class FileHelperOperate
    {


        public static List<string> GetFileContent(string fileName, string pathData = "Mate_Data")
        {
            string directoryPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, pathData);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            string filePath = directoryPath + "\\" + fileName;
            if (!File.Exists(filePath))
            {
                string msg = $"{filePath} 文件不存在";
                SyncHandlerBase.DebugLog(msg);
                throw new FileNotFoundException(msg);
            }
            List<string> data = new List<string>();
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line = "";
                while ((line = reader.ReadLine()) != null)
                {
                    data.Add(line);
                }
            }

            return data;
        }

        public static void WriteFileContent(string fileName, List<string> datas, string filePathAdd = "", string pathData = "Mate_Data")
        {
            string directoryPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, pathData);
            directoryPath += filePathAdd;
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            string filePath = directoryPath + "\\" + fileName;
            if (!File.Exists(filePath))
            {
                using (File.Create(filePath))
                {

                }

                if (!File.Exists(filePath))
                {
                    string msg = $"BeisenTitaMenuTableMigrate 文件不存在";
                    SyncHandlerBase.DebugLog(msg);
                    throw new FileNotFoundException(msg);
                }
            }
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var data in datas)
                {
                    writer.WriteLine(data);
                }
            }

        }
    }
}
