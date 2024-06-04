using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wbl.workTest.Service
{
    public class LessonPlanAnalyseHandle
    {
        public static LessonPlanAnalyseHandle Instance { get; } = new LessonPlanAnalyseHandle();

        public void LessonPlanAnalyseMain()
        {
            while (true)
            {
                try
                {
                    Console.WriteLine($"本工具为计算试题难度系数与区分度设置，请输入指令继续");
                    Console.WriteLine($"     输入\"1\",客观性试题难度P");
                    Console.WriteLine($"     输入\"2\",主观性试题难度P");
                    Console.WriteLine($"     输入\"3\",大群体标准化主、客观试题难度");
                    Console.WriteLine($"     输入\"4\",客观性试题区分度");
                    Console.WriteLine($"     输入\"5\",主观试题（非选择题）区分度");
                    Console.Write("请输入指令: ");
                    var command = Console.ReadLine();
                    Console.WriteLine("请输入所需计算列从0开始");
                    var line = int.Parse(Console.ReadLine());
                    Console.WriteLine("请输入所需计算文件名称带有后缀");
                    var fileName = Console.ReadLine();
                    Console.WriteLine("请输入所需计算文件为第几页从1开始");
                    var numberOfSheets = int.Parse(Console.ReadLine());
                    var data = GetScores(fileName, line, numberOfSheets);
                    Console.WriteLine("请输入此列对应数据正确时所获取分数");
                    var trueScore = int.Parse(Console.ReadLine());
                    double result = 0.00;
                    switch (command)
                    {
                        case "1":
                            Console.WriteLine("客观性试题难度P（这时也称通过率）计算公式：   P=k/N（k为答对该题的人数，N为参加测验的总人数） ");
                            result = ObjectivityDifficulty(data, trueScore);
                            break;
                        case "2":
                            Console.WriteLine("主观性试题难度P（P=X/M（X为试题平均得分；M为试题满分）    ");
                            result = SubjectivityDifficulty(data, trueScore);
                            break;
                        case "3":
                            Console.WriteLine("适用于主、客观试题的计算公式：   P=（PH+PL）/2（PH、PL分别为试题针对高分组和低分组考生的难度值） 在大群体标准化中，此法较为方便。具体步骤为:①将考生的总分由高至低排列；②从最高分开始向下取全部试卷的27%作为高分组；③从最低分开始向上取全部试卷的27%作为低分组；④按上面的公式计算。");
                            Console.WriteLine("请输入该题是true：主观题 false：客观题");
                            var subOrOb = bool.Parse(Console.ReadLine());
                            result = SubjectivityAndObjectivityDifficulty(data, trueScore, subOrOb);
                            break;
                        case "4":
                            Console.WriteLine("客观性试题区分度D的计算公式 D=PH-PL（PH、PL分别为试题高分组和低分组考生的难度值）");
                            result = ObjectivityDiscrimination(data, trueScore);
                            break;
                        case "5":
                            Console.WriteLine("D的计算公式 D=（XH-XL）/N（H-L） （XH表示接受测验的高分段学生的分数总和，XL表示接受测验的低分段学生的分数总和，N表示接受测验的学生总数，H表示该题的最高得分，L表示该题的最低得分。）");
                            result = SubjectivityDiscrimination(data);
                            break;
                        default:
                            break;
                    }
                    Console.WriteLine($"计算所得：{result}");
                }
                catch (Exception ex)
                {

                    Console.WriteLine($"输入有误导致问题：{ex.Message}");
                }
               
            }           
        }

        public List<int> GetScores(string fileName, int line, int numberOfSheets)
        {
            List<int> result = new List<int>();
            var data = ExcelHandle.ReadExcelToTable1(fileName, numberOfSheets);

            for (int i = 1; i < data.GetLength(0); i++)
            {
                result.Add(int.Parse(data[i][line]));
            }
            return result;
        }


        //客观性试题难度
        public double ObjectivityDifficulty(List<int> scores, int trueScore)
        {
            //var scoreOrd = scores.OrderBy(_ => _).ToList();

            var trueSores =  scores.Where(_ => _ == trueScore).ToList();

            // 客观性试题难度P（这时也称通过率）计算公式：   P=k/N（k为答对该题的人数，N为参加测验的总人数）  
            if (trueSores.Count > 0)
            {
                return (double)trueSores.Count / scores.Count;
            }

            return 0.00;
        }

        // 主观性试题难度P
        public double SubjectivityDifficulty(List<int> scores, int M)
        {
            //var scoreOrd = scores.OrderBy(_ => _).ToList();
            var scoreSum = scores.Sum();            
            // P=X/M（X为试题平均得分；M为试题满分）    
            var X = (double)(scoreSum / scores.Count);
            var P = (double)(X / M);
            return P;
        }

        // 适用于主、客观试题的计算公式：   P=（PH+PL）/2（PH、PL分别为试题针对高分组和低分组考生的难度值） 在大群体标准化中，此法较为方便。具体步骤为:①将考生的总分由高至低排列；②从最高分开始向下取全部试卷的27%作为高分组；③从最低分开始向上取全部试卷的27%作为低分组；④按上面的公式计算。
        public double SubjectivityAndObjectivityDifficulty(List<int> scores, int M, bool SubOrOb) // true 主观  false客观
        {
            double PL = 0.00;
            double PH = 0.00;
            var scoreOrd = scores.OrderBy(_ => _).ToList();
            var scoreOrdDescending = scores.OrderByDescending(_ => _).ToList();
            var index = (int)(scores.Count * 0.27);
            var PLList = scoreOrd.Take(index).ToList(); 
            var PHList = scoreOrdDescending.Take(index).ToList();
            if (SubOrOb)
            {
                Console.WriteLine("主观");
                PL = SubjectivityDifficulty(PLList, M);
                PH = SubjectivityDifficulty(PHList, M);
            }
            else
            {
                Console.WriteLine("false客观");
                PL = ObjectivityDifficulty(PLList, M);
                PH = ObjectivityDifficulty(PHList, M);
            }
            var P = (PL + PH) / 2;            
            return P;
        }

        // 客观性试题区分度
        public double ObjectivityDiscrimination(List<int> scores, int M)
        {
            double PL = 0.00;
            double PH = 0.00;
            var scoreOrd = scores.OrderBy(_ => _).ToList();
            var scoreOrdDescending = scores.OrderByDescending(_ => _).ToList();
            var index = (int)(scores.Count * 0.27);
            var PLList = scoreOrd.Take(index).ToList();
            var PHList = scoreOrdDescending.Take(index).ToList();
            Console.WriteLine("false客观");
            PL = ObjectivityDifficulty(PLList, M);
            PH = ObjectivityDifficulty(PHList, M);
            // 客观性试题区分度D的计算公式 D=PH-PL（PH、PL分别为试题高分组和低分组考生的难度值）
            var D = PH - PL;
            return D;
        }

        // 主观试题（非选择题）区分度
        public double SubjectivityDiscrimination(List<int> scores)
        {
            var N = scores.Count();
            var scoreOrd = scores.OrderBy(_ => _).ToList();
            var scoreOrdDescending = scores.OrderByDescending(_ => _).ToList();
            var index = (int)(scores.Count * 0.27);
            var XLSum = scoreOrd.Take(index).ToList();
            var XHSum = scoreOrdDescending.Take(index).ToList();
            var XL = XLSum.Sum();
            var XH = XHSum.Sum();
            var L = scoreOrd.First();
            var H = scoreOrdDescending.First();
            // D的计算公式 D=（XH-XL）/N（H-L） （XH表示接受测验的高分段学生的分数总和，XL表示接受测验的低分段学生的分数总和，N表示接受测验的学生总数，H表示该题的最高得分，L表示该题的最低得分。）
            var D = (double)((XH - XL) / (N *(H - L)));
            return D;
        }
    }
}
