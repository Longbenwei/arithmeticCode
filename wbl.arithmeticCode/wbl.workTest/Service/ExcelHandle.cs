using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wbl.workTest.Service
{
    public class ExcelHandle
    {
        public static Dictionary<string, string[][]> ExcelData = new Dictionary<string, string[][]>();

        public static string[][] ReadExcelToTable1(string fileName, int numberOfSheets)
        {
            string excelDataKey = $"{fileName}_{numberOfSheets}";
            if (ExcelData.ContainsKey(excelDataKey))            
                return ExcelData[excelDataKey];                        

            string directoryPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "ExcelFile");
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            string menuIdMapFilePath = $"{directoryPath}\\{fileName}";
            if (File.Exists(menuIdMapFilePath))
            {                                
                var result = ReadFromExcelFile(menuIdMapFilePath, numberOfSheets);
                ExcelData[excelDataKey] = result;
                return result;
            }
            else
            {
                Console.WriteLine("文件不存在");
            }


            return null;
        }


        public static string[][] ReadFromExcelFile(string filePath, int numberOfSheets)
        {
            string[][] result = null;
            //首先根据需要读取的文件创建一个文件流对象
            using (FileStream fs = File.OpenRead(filePath))
            {
                IWorkbook workbook = null;
                //这里需要根据文件名格式判断一下
                //HSSF只能读取xls的
                //XSSF只能读取xlsx格式的
                if (Path.GetExtension(fs.Name) == ".xls")
                {
                    workbook = new HSSFWorkbook(fs);
                }
                else if (Path.GetExtension(fs.Name) == ".xlsx")
                {
                    workbook = new XSSFWorkbook(fs);
                }
                //因为Excel表中可能不止一个工作表，这里为了演示，我们遍历所有工作表
                for (int i = numberOfSheets - 1; i < numberOfSheets; i++)
                {
                    //得到当前sheet
                    ISheet sheet = workbook.GetSheetAt(i);
                    //也可以通过GetSheet(name)得到
                    //遍历表中所有的行
                    //注意这里加1，这里得到的最后一个单元格的索引默认是从0开始的
                    result = new string[sheet.LastRowNum + 1][];
                    for (int j = 0; j < sheet.LastRowNum + 1; j++)
                    {                        
                        //得到当前的行
                        IRow row = sheet.GetRow(j);
                        result[j] = new string[row.LastCellNum];
                        //遍历每行所有的单元格
                        //注意这里不用加1，这里得到的最后一个单元格的索引默认是从1开始的
                        for (int k = 0; k < row.LastCellNum; k++)
                        {
                            //得到当前单元格
                            ICell cell = row.GetCell(k, MissingCellPolicy.CREATE_NULL_AS_BLANK);
                            var str = SwitchCellValue(cell);
                            result[j][k] = str;
                            //Console.Write(SwitchCellValue(cell)+ " ");
                        }                        
                    }
                }
            }
            return result;
        }


        public static string SwitchCellValue(ICell cell)
        {
            string str = "";
            switch (cell.CellType)
            {
                case CellType.Unknown:
                    break;
                case CellType.Numeric:
                    str = cell.NumericCellValue.ToString();
                    break;
                case CellType.String:
                    str = cell.StringCellValue;
                    break;
                case CellType.Formula:                    
                    break;
                case CellType.Blank:
                    break;
                case CellType.Boolean:
                    str = cell.BooleanCellValue.ToString();
                    break;
                case CellType.Error:
                    break;
                default:
                    break;
            }


            return str;
        }

        ///<summary>
        ///读取xls\xlsx格式的Excel文件的方法
        ///</ummary>
        ///<param name="path">待读取Excel的全路径</param>
        ///<returns></returns>
        public static System.Data.DataTable ReadExcelToTable(string path)
        {
            
            //连接字符串
            //string connstring = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Extended Properties='Excel 8.0;HDR=NO;IMEX=1';"; // Office 07及以上版本 不能出现多余的空格 而且分号注意
            string connstring = "Provider=Microsoft.JET.OLEDB.4.0;Data Source=" + path + ";Extended Properties='Excel 8.0;HDR=NO;IMEX=1';"; //Office 07以下版本 因为本人用Office2010 所以没有用到这个连接字符串 可根据自己的情况选择 或者程序判断要用哪一个连接字符串
            using (OleDbConnection conn = new OleDbConnection(connstring))
            {
                conn.Open();
                System.Data.DataTable sheetsName = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "Table" }); //得到所有sheet的名字
                string firstSheetName = sheetsName.Rows[0][2].ToString(); //得到第一个sheet的名字
                string sql = string.Format("SELECT * FROM [{0}],firstSheetName", firstSheetName); //查询字符串
                OleDbDataAdapter ada = new OleDbDataAdapter(sql, connstring);
                DataSet set = new DataSet();
                ada.Fill(set);
                return set.Tables[0];

            }
        }


        ///<summary>
        ///读取csv格式的Excel文件的方法
        ///</ummary>
        ///<param name="path">待读取Excel的全路径</param>
        ///<returns></returns>
        private DataTable ReadExcelWithStream(string path)
        {
            DataTable dt = new DataTable();
            bool isDtHasColumn = false; //标记DataTable 是否已经生成了列
            StreamReader reader = new StreamReader(path, System.Text.Encoding.Default); //数据流
            while (!reader.EndOfStream)
            {
                string message = reader.ReadLine();
                string[] splitResult = message.Split(new char[] { ',' }, StringSplitOptions.None); //读取一行 以逗号分隔 存入数组
                DataRow row = dt.NewRow();
                for (int i = 0; i < splitResult.Length; i++)
                {
                    if (!isDtHasColumn) //如果还没有生成列
                    {
                        dt.Columns.Add("column" + i, typeof(string));
                    }
                    row[i] = splitResult[i];
                }
                dt.Rows.Add(row); //添加行
                isDtHasColumn = true; //读取第一行后 就标记已经存在列 再读取以后的行时，就不再生成列
            }
            return dt;
        }

    }
}
