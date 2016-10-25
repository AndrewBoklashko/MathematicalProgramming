using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;   
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using static MathematicalProgramming.Helpers;
using System.Windows.Forms.DataVisualization.Charting;

namespace MathematicalProgramming
{
    public class OPTask : ITask
    {
        public int EquipmentCount { get; set; }
        public int DetailsCount { get; set; }
        public FMM[] Modules { get; set; }
        public Detail[] Details { get; set; }
        public int[,] TransportLengths { get; set; }
        public IRuleHandler RuleHandler { get; set; }
        public FMM Atm1, Atm2, Atm3;

        public float Tvz, Tpost, Tz, Tpid, Tr;

        public float TotalTime { get; private set; }

        public void ReadDataFromTxt(string path)
        {
            string[] lines = File.ReadAllLines(path);

            EquipmentCount = int.Parse(lines[0]);
            DetailsCount = int.Parse(lines[1]);

            int ruleNumber = int.Parse(lines[2]);
            switch (ruleNumber)
            {
                case 1:
                    RuleHandler = new ShortestOpHandler();
                    break;
                case 2:
                    RuleHandler = new MaxRestComplexityHandler();
                    break;
                case 3:
                    RuleHandler = new SmoothLoadHandler();
                    break;
                case 4:
                    RuleHandler = new MinRestComplexityHandler();
                    break;
                case 5:
                    RuleHandler = new LongestOpHandler();
                    break;
                case 6:
                    RuleHandler = new FIFOHandler();
                    break;
                case 7:
                    RuleHandler = new LIFOHandler();
                    break;
            }

            Modules = new FMM[EquipmentCount];
            for (int i = 0; i < EquipmentCount; i++)
            {
                Modules[i] = new FMM();
                Modules[i].Index = i + 1;
                Modules[i].Bag = new List<Detail>();
                Modules[i].DetailQueue = new List<DetailPos>();
                Modules[i].RuleHandler = RuleHandler;
            }

            Details = new Detail[DetailsCount];
            for (int i = 0; i < DetailsCount; i++)
            {
                Details[i] = new Detail();
                Details[i].Index = i + 1;
                string[] routeString = lines[i + 3].Split(new[] { ' ' });
                int routeLength = routeString.Length;
                string[] durationString = lines[i + 4 + DetailsCount].Split(new[] { ' ' }, routeLength);

                Details[i].Route = new FMM[routeLength];
                Details[i].Durations = new float[routeLength];
                Details[i].StartTimes = new float[routeLength];
                for (int j = 0; j < routeLength; j++)
                {
                    Details[i].Route[j] = Modules[ int.Parse(routeString[j].Trim()) - 1];
                    Details[i].Durations[j] = float.Parse(durationString[j].Trim());
                }
            }

            TransportLengths = new int[EquipmentCount + 1, EquipmentCount + 1];
            for (int i = 0; i < EquipmentCount + 1; i++)
            {
                string[] routeString = lines[i + 5 + 2*DetailsCount].Split(new[] {' '});
                int routeLength = routeString.Length;
               
                for (int j = 0; j < routeLength; j++)
                {
                    string elem = routeString[j].Trim();
                    TransportLengths[i, j] = elem == "-" ? -1 : int.Parse(elem);
                }
            }

            Tvz = float.Parse(lines[6 + 2 * DetailsCount + EquipmentCount]);
            Tpost = float.Parse(lines[7 + 2 * DetailsCount + EquipmentCount]);
            Tz = float.Parse(lines[8 + 2 * DetailsCount + EquipmentCount]);
            Tr = float.Parse(lines[9 + 2 * DetailsCount + EquipmentCount]);
            Tpid = float.Parse(lines[10 + 2 * DetailsCount + EquipmentCount]);
        }

        public void WriteResultToTxt(string path)
        {
            List<string> outputStr = new List<string>();

            for (int i = 0; i < DetailsCount; i++)
            {
                if (Details[i].StartTimes != null)
                {                   
                    StringBuilder str = new StringBuilder("D" + (i + 1));
                    for (int j = 0; j < Details[i].StartTimes.Length; j++)
                    {
                        str.Append(" " + Details[i].StartTimes[j].ToString("F2"));
                    }
                    outputStr.Add(str.ToString());
                }

            }

            outputStr.Add(string.Empty);

            for (int i = 0; i < Modules.Length; i++)
            {
                if (Modules[i].DetailQueue != null)
                {
                    StringBuilder str = new StringBuilder(/*"M" + (i+1).ToString() + ":"*/);
                    for (int j = 0; j < Modules[i].DetailQueue.Count; j++)
                    {
                        int index = Modules[i].DetailQueue[j].Index;
                        if (index != 0)
                        {
                            string indexStr = index == 0 ? "-" : index.ToString();
                            str.Append(/*"  " + */indexStr + "\t"/* + " ( " +
                                             Modules[i].DetailQueue[j].Duration.ToString("F2") + " )"*/);
                        }
                    }
                    outputStr.Add(str.ToString().Trim());
                }

            }

            outputStr.Add(string.Empty);
            outputStr.Add("Простої: кількість та сумарна тривалість");

            float sumPlainsAttitudes = 0;
            for (int i = 0; i < Modules.Length; i++)
            {
                if (Modules[i].DetailQueue != null)
                {
                    int plainsCount = 0;
                    float plainsTotalDuration = 0;
                    for (int j = 0; j < Modules[i].DetailQueue.Count; j++)
                    {
                        if (Modules[i].DetailQueue[j].Index == 0)
                        {
                            plainsCount++;
                            plainsTotalDuration += Modules[i].DetailQueue[j].Duration;
                        }
                    }
                    float plainAttitude = plainsCount != 0 ? plainsTotalDuration/plainsCount : 0;
                    outputStr.Add("M" + (i + 1).ToString() + ":" + " " + plainsCount + " " + plainsTotalDuration.ToString("f2") + " " +
                                  plainAttitude.ToString("f2"));
                    sumPlainsAttitudes += plainAttitude;
                }
            }
            outputStr.Add(sumPlainsAttitudes.ToString("f2"));

            File.WriteAllLines(path, outputStr.ToArray());

            Color[] colors = new Color[]
            {
                Color.Transparent,
                Color.Blue, 
                Color.Brown, 
                Color.DeepSkyBlue,
                Color.ForestGreen, 
                Color.Gold, 
                Color.Lime, 
                Color.Red, 
                Color.BlueViolet, 
                Color.OrangeRed, 
                Color.Orange, 
                Color.Firebrick, 
                Color.Magenta, 
                Color.Navy, 
                Color.AntiqueWhite,
                Color.Olive
            };

            
            ChartArea chartArea1 = new ChartArea
            {
                Name = "Default",
                AxisX = {Title = "Номер ГВМ"},
                AxisY =
                {
                    Title = "Час",
                    Interval = 25
                }
            };
            
            Legend legend1 = new Legend {Name = "Legend1"};
            

            Series[] legendSeries = new Series[DetailsCount + 1];
            for (int i = 0; i < legendSeries.Length; i++)
            {
                legendSeries[i] = new Series
                {
                    ChartArea = "Default",
                    Legend = "Legend1",
                    Name = "Деталь" + i.ToString(),
                    ChartType = SeriesChartType.RangeBar,
                    //YValuesPerPoint = EquipmentCount,
                    Color = colors[i]
                };
            }
            legendSeries[0].IsVisibleInLegend = false;
            
            Series equipmentSeries = new Series()
            {
                ChartArea = "Default",
                Legend = "Legend1",
                Name = "M",
                ChartType = SeriesChartType.RangeBar,
                //YValuesPerPoint = EquipmentCount,
                IsVisibleInLegend = false
            };

            for (int i = 0; i < Modules.Length; i++)
            {
                if (Modules[i].DetailQueue != null)
                {
                    foreach (DetailPos current in Modules[i].DetailQueue)
                    {
                        DataPoint dp = new DataPoint(Modules[i].Index, new double[]
                        {
                            current.StartTime,
                            current.StartTime + current.Duration
                        });
                        dp.Color = colors[current.Index];
                        equipmentSeries.Points.Add(dp);
                    }
                }
            }
/*
            foreach (var pos in Atm1.DetailQueue)
            {
                DataPoint dp = new DataPoint(-1, new double[]
                        {
                            pos.StartTime,
                            pos.StartTime + pos.Duration
                        });
                dp.Color = colors[pos.Index];
                equipmentSeries.Points.Add(dp);
            }
            foreach (var pos in Atm2.DetailQueue)
            {
                DataPoint dp = new DataPoint(-2, new double[]
                        {
                            pos.StartTime,
                            pos.StartTime + pos.Duration
                        });
                dp.Color = colors[pos.Index];
                equipmentSeries.Points.Add(dp);
            }
            foreach (var pos in Atm3.DetailQueue)
            {
                DataPoint dp = new DataPoint(-3, new double[]
                        {
                            pos.StartTime,
                            pos.StartTime + pos.Duration
                        });
                dp.Color = colors[pos.Index];
                equipmentSeries.Points.Add(dp);
            }
*/

            Form newForm = new Form();
            newForm.Load += (sender, args) =>
            {
                Chart chart1 = new Chart
                {
                    Width = 1300,
                    Height = 400
                };

                chart1.ChartAreas.Clear();
                chart1.ChartAreas.Add(chartArea1);
                chart1.Legends.Clear();
                chart1.Legends.Add(legend1);
                chart1.Series.Clear();
                chart1.Series.Add(equipmentSeries);
                chart1.Left = 0;
                chart1.Top = 80;
                for (int i = 1; i < legendSeries.Length; i++)
                {
                    chart1.Series.Add(legendSeries[i]);
                }

                newForm.Controls.Add(chart1);
                newForm.Width = chart1.Width;
                newForm.Height = chart1.Height + 200;
                newForm.Text = "Діагарама Ганта";
                Label ruleLabel = new Label();
                ruleLabel.Font = new Font(FontFamily.GenericSerif, 14);
                ruleLabel.AutoSize = true;
                if (RuleHandler is ShortestOpHandler)
                {
                    ruleLabel.Text = "Правило найкоротшої операції";
                } else
                if (RuleHandler is MaxRestComplexityHandler)
                {
                    ruleLabel.Text = "Правило максимальної залишкової трудомісткості";
                } else
                if (RuleHandler is SmoothLoadHandler)
                {
                    ruleLabel.Text = "Правило вирівнювання завантаження верстатів";
                }
                else
                if (RuleHandler is MinRestComplexityHandler)
                {
                    ruleLabel.Text = "Правило мінімальної залишкової трудомісткості";
                }
                else
                if (RuleHandler is LongestOpHandler)
                {
                    ruleLabel.Text = "Правило найдовшої операції";
                }
                else
                if (RuleHandler is LIFOHandler)
                {
                    ruleLabel.Text = "Правило LIFO";
                }
                else
                if (RuleHandler is FIFOHandler)
                {
                    ruleLabel.Text = "Правило FIFO";
                }
                else throw new NotImplementedException("Такого правила не існує: " + RuleHandler.GetType().ToString());

                ruleLabel.Top = 50;
                ruleLabel.Left = 500;
                newForm.Left = newForm.Top = 0;
                newForm.Controls.Add(ruleLabel);

                Label totalTimeLabel = new Label();
                totalTimeLabel.Font = new Font(FontFamily.GenericSerif, 14);
                totalTimeLabel.Top = 500;
                totalTimeLabel.Left = 100;
                totalTimeLabel.Text = "Тривалість виробничого циклу: " + TotalTime.ToString("F2");
                totalTimeLabel.AutoSize = true;
                newForm.Controls.Add(totalTimeLabel);
            };
            Application.Run(newForm);
        }

        public void Resolve()
        {
            FMM currentModule = null;

            for (int i = 0; i < DetailsCount; i++)
            {
                Details[i].CurrentModule.Bag.Add(Details[i]);
            }

            do
            {
                float currentTime = float.MaxValue;
                foreach (var module in Modules)
                {
                    if (module.Bag.Any())
                    {
                        if (module.CurrentDetail == null)
                        {
                            module.SetDetail();
                        }

                        if (module.CurrentDetail != null)
                        {
                            if (module.CurrentDetail.CurrentEndTime <= currentTime)
                            {
                                currentTime = module.CurrentDetail.CurrentEndTime;
                                currentModule = module;
                            }
                        }
                    }
                }

                if (currentModule == null || currentModule.CurrentDetail == null) break;
                currentModule.CurrentDetail.StartTimes[currentModule.CurrentDetail.OpNumber] = currentTime -
                                                                                                currentModule
                                                                                                    .CurrentDetail
                                                                                                    .CurrentDuration;
                currentModule.AddToQueue();
                currentModule.CurrentDetail.RemoveFromBag();
                currentModule.CurrentDetail.ToNextOperation();
                currentModule.CurrentDetail = null;
            } while (true);

            TotalTime = 0;
            foreach (var module in Modules)
            {
                var lastDetail = module.DetailQueue.LastOrDefault();
                if (lastDetail != null)
                {
                    var moduleFinishTime = lastDetail.StartTime + lastDetail.Duration;
                    if (moduleFinishTime > TotalTime)
                    {
                        TotalTime = moduleFinishTime;
                    }
                }
            }
        }


        public float ResolveTransport(FMM module, float time, out float transportTime)
        {
            bool linearFlag = false;

            int to = module.Index,
                @from = module.CurrentDetail.OpNumber == 0
                ? 0
                : module.CurrentDetail.Route[module.CurrentDetail.OpNumber - 1].Index;
            int routeLength = TransportLengths[@from, to];

            if (to == 6 || to == 5)
            {
                linearFlag = true;
            }
            if (to == 4 || to == 3)
            {
                linearFlag = true;
            }

            transportTime = Tpid*routeLength + Tpost;
            if (to == 0)
            {
                transportTime += Tpost;
            }
            else
            {
                transportTime += Tz;
            }
            if (linearFlag)
            {
                transportTime += Tvz + Tpost;
            }
            if (@from == 0)
            {
                transportTime += Tvz;
            }
            else
            {
                transportTime += Tr;
            }

            DetailPos current, idle, linearPos, linearIdle;
            switch (to)
            {
                case 3:
                    current = new DetailPos
                    {
                        Index = module.CurrentDetail.Index,
                        StartTime = time,
                        Duration = transportTime - Tvz - Tz
                    };
                    //module.CurrentDetail.StartTimes[module.CurrentDetail.OpNumber] += transportTime;
                    Atm1.AddToQueue(current, module);

                    idle = new DetailPos
                    {
                        Index = 15,
                        StartTime = current.StartTime + current.Duration,
                        Duration = Tpid*routeLength
                    };
                    Atm1.AddToQueue(idle, module);

                    linearPos = new DetailPos
                    {
                        Index = module.CurrentDetail.Index,
                        StartTime = current.StartTime + current.Duration,
                        Duration = Tvz + Tz
                    };
                    Atm2.AddToQueue(linearPos, module);

                    break;

                case 4:
                    current = new DetailPos
                    {
                        Index = module.CurrentDetail.Index,
                        StartTime = time,
                        Duration = transportTime - Tvz - Tz
                    };
                    //module.CurrentDetail.StartTimes[module.CurrentDetail.OpNumber] += transportTime;
                    Atm1.AddToQueue(current, module);

                    idle = new DetailPos
                    {
                        Index = 15,
                        StartTime = current.StartTime + current.Duration,
                        Duration = Tpid * routeLength
                    };
                    Atm1.AddToQueue(idle, module);

                    linearPos = new DetailPos
                    {
                        Index = module.CurrentDetail.Index,
                        StartTime = current.StartTime + current.Duration,
                        Duration = Tvz + Tz
                    };
                    Atm3.AddToQueue(linearPos, module);

                    break;

                case 5:
                    current = new DetailPos
                    {
                        Index = module.CurrentDetail.Index,
                        StartTime = time,
                        Duration = transportTime - Tvz - Tz - Tpid
                    };
                    //module.CurrentDetail.StartTimes[module.CurrentDetail.OpNumber] += transportTime - Tvz - Tz - Tpid;
                    Atm1.AddToQueue(current, module);

                    idle = new DetailPos
                    {
                        Index = 15,
                        StartTime = current.StartTime + current.Duration,
                        Duration = Tpid * (routeLength - 1)
                    };
                    Atm1.AddToQueue(idle, module);

                    linearPos = new DetailPos
                    {
                        Index = module.CurrentDetail.Index,
                        StartTime = current.StartTime + current.Duration,
                        Duration = Tvz + Tz + Tpid
                    };
                    Atm3.AddToQueue(linearPos, module);

                    linearIdle = new DetailPos
                    {
                        Index = 15,
                        StartTime = linearPos.StartTime + linearPos.Duration,
                        Duration = Tpid
                    };
                    Atm3.AddToQueue(linearIdle, module);
                    break;

                case 6:
                    current = new DetailPos
                    {
                        Index = module.CurrentDetail.Index,
                        StartTime = time,
                        Duration = transportTime - Tvz - Tz - Tpid
                    };
                    //module.CurrentDetail.StartTimes[module.CurrentDetail.OpNumber] += transportTime - Tvz - Tz - Tpid;
                    Atm1.AddToQueue(current, module);

                    idle = new DetailPos
                    {
                        Index = 15,
                        StartTime = current.StartTime + current.Duration,
                        Duration = Tpid * (routeLength - 1)
                    };
                    Atm1.AddToQueue(idle, module);

                    linearPos = new DetailPos
                    {
                        Index = module.CurrentDetail.Index,
                        StartTime = current.StartTime + current.Duration,
                        Duration = Tvz + Tz + Tpid
                    };
                    Atm2.AddToQueue(linearPos, module);

                    linearIdle = new DetailPos
                    {
                        Index = 15,
                        StartTime = linearPos.StartTime + linearPos.Duration,
                        Duration = Tpid
                    };
                    Atm2.AddToQueue(linearIdle, module);

                    break;
                case 0:
                    break;
                default:
                    current = new DetailPos
                    {
                        Index = module.CurrentDetail.Index,
                        StartTime = time,
                        Duration = transportTime
                    };
                    //module.CurrentDetail.StartTimes[module.CurrentDetail.OpNumber] += transportTime - Tvz - Tz - Tpid;
                    Atm1.AddToQueue(current, module);
                    idle = new DetailPos
                    {
                        Index = 15,
                        StartTime = current.StartTime + current.Duration,
                        Duration = Tpid * routeLength
                    };
                    Atm1.AddToQueue(idle, module);

                    break;
            }

            return module.CurrentDetail.CurrentEndTime - module.CurrentDetail.CurrentDuration;
        }
    }
}
