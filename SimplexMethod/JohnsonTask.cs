using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace MathematicalProgramming
{
    public class JohnsonTask: ITask
    {
        public int EquipmentCount { get; set; }
        public int DetailsCount { get; set; }
        public float[,] Durations;

        public float TotalTime { get; private set; }
        public float[,] StartTimes { get; private set; }

        public void ReadDataFromTxt(string path)
        {
            string[] lines = File.ReadAllLines(path);

            EquipmentCount = int.Parse(lines[0]);
            DetailsCount = int.Parse(lines[1]);

            Durations = new float[EquipmentCount, DetailsCount];
            for (int i = 0; i < EquipmentCount; i++)
            {
                string[] durationsString = lines[i + 2].Split(new[] {' '}, DetailsCount);
                for (int j = 0; j < DetailsCount; j++)
                {
                    Durations[i, j] = float.Parse(durationsString[j].Trim());
                }
            }
        }

        public void WriteResultToTxt(string path)
        {
            List<string> outputStr = new List<string>();

            for (int i = 0; i < EquipmentCount; i++)
            {
                StringBuilder str = new StringBuilder("");
                for (int j = 0; j < DetailsCount; j++)
                {
                    str.Append(StartTimes[i, j].ToString("f2") + " ");
                }
                outputStr.Add(str.ToString().Trim());
            }
            File.WriteAllLines(path, outputStr);
        }

        public void Resolve()
        {
            switch (EquipmentCount)
            {
                case 2:
                {
                    ResolveTwoEquip();
                    break;
                }
                case 3:
                {
                    ResolveThreeEquip();
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void ResolveThreeEquip()
        {
            List<int> FirstGroupIndexes = new List<int>();
            List<int> SecondGroupIndexes = new List<int>();

            for (int i = 0; i < DetailsCount; i++)
            {
                if (Durations[0, i] + Durations[1, i] < Durations[1, i] + Durations[2, i])
                {
                    FirstGroupIndexes.Add(i);
                }
                else
                {
                    SecondGroupIndexes.Add(i);
                }
            }

            float[,] FirstGroupDetails = new float[3, FirstGroupIndexes.Count];
            float[,] SecondGroupDetails = new float[3, SecondGroupIndexes.Count];

            for (int i = 0; i < FirstGroupIndexes.Count; i++)
            {
                FirstGroupDetails[0, i] = Durations[0, FirstGroupIndexes[i]] + Durations[1, FirstGroupIndexes[i]];
                FirstGroupDetails[1, i] = Durations[1, FirstGroupIndexes[i]] + Durations[2, FirstGroupIndexes[i]];
                FirstGroupDetails[2, i] = FirstGroupIndexes[i];
            }

            for (int i = 0; i < SecondGroupIndexes.Count; i++)
            {
                SecondGroupDetails[0, i] = Durations[0, SecondGroupIndexes[i]] + Durations[1, SecondGroupIndexes[i]];
                SecondGroupDetails[1, i] = Durations[1, SecondGroupIndexes[i]] + Durations[2, SecondGroupIndexes[i]];
                SecondGroupDetails[2, i] = SecondGroupIndexes[i];
            }

            FirstGroupDetails = FirstGroupDetails.OrderBy(t => t[0]);
            SecondGroupDetails = SecondGroupDetails.OrderByDescending(t => t[1]);

            float[,] Concated = new float[EquipmentCount + 1, DetailsCount];

            int firstGroupLength = FirstGroupDetails.GetLength(1);
            int index = 0;
            for (int i = 0; i < firstGroupLength; i++)
            {
                index = Convert.ToInt32(FirstGroupDetails[2, i]);
                Concated[0, i] = Durations[0, index];
                Concated[1, i] = Durations[1, index];
                Concated[2, i] = Durations[2, index];
                Concated[3, i] = index;
            }

            for (int i = 0; i < SecondGroupDetails.GetLength(1); i++)
            {
                index = Convert.ToInt32(SecondGroupDetails[2, i]);
                Concated[0, i + firstGroupLength] = Durations[0, index];
                Concated[1, i + firstGroupLength] = Durations[1, index];
                Concated[2, i + firstGroupLength] = Durations[2, index];
                Concated[3, i + firstGroupLength] = index;
            }

            StartTimes = new float[EquipmentCount + 1, DetailsCount + 1];
            
            float time = 0;
            for (int i = 0; i < DetailsCount; i++)
            {
                StartTimes[0, i] = time;
                time += Concated[0, i];
            }

            StartTimes[1, 0] = StartTimes[0, 0] + Concated[0, 0];
            for (int i = 1; i < DetailsCount; i++)
            {
                StartTimes[1, i] = Max(StartTimes[0, i] + Concated[0, i], StartTimes[1, i - 1] + Concated[1, i - 1]);
            }

            StartTimes[2, 0] = StartTimes[1, 0] + Concated[1, 0];
            for (int i = 1; i < DetailsCount; i++)
            {
                StartTimes[2, i] = Max(StartTimes[1, i] + Concated[1, i], StartTimes[2, i - 1] + Concated[2, i - 1]);
            }


            for (int i = 0; i < DetailsCount; i++)
            {
                StartTimes[3, i] = StartTimes[2, i] + Concated[2, i];
            }

            for (int i = 0; i < EquipmentCount; i++)
            {
                StartTimes[i, DetailsCount] = StartTimes[i, DetailsCount - 1] + Concated[i, DetailsCount - 1];
            }

            TotalTime = Max(StartTimes[0, DetailsCount], Max(StartTimes[1, DetailsCount], StartTimes[2, DetailsCount]));
        }

        private void ResolveTwoEquip()
        {
            List<int> FirstGroupIndexes = new List<int>();
            List<int> SecondGroupIndexes = new List<int>();

            for (int i = 0; i < DetailsCount; i++)
            {
                if (Durations[0,i] < Durations[1,i])
                {
                    FirstGroupIndexes.Add(i);
                }
                else
                {
                    SecondGroupIndexes.Add(i);
                }
            }

            float[,] FirstGroupDetails = new float[3, FirstGroupIndexes.Count];
            float[,] SecondGroupDetails = new float[3, SecondGroupIndexes.Count];

            for (int i = 0; i < FirstGroupIndexes.Count; i++)
            {
                FirstGroupDetails[0, i] = Durations[0, FirstGroupIndexes[i]];
                FirstGroupDetails[1, i] = Durations[1, FirstGroupIndexes[i]];
                FirstGroupDetails[2, i] = FirstGroupIndexes[i];
            }

            for (int i = 0; i < SecondGroupIndexes.Count; i++)
            {
                SecondGroupDetails[0, i] = Durations[0, SecondGroupIndexes[i]];
                SecondGroupDetails[1, i] = Durations[1, SecondGroupIndexes[i]];
                SecondGroupDetails[2, i] = SecondGroupIndexes[i];
            }

            FirstGroupDetails = FirstGroupDetails.OrderBy(t => t[0]);
            SecondGroupDetails = SecondGroupDetails.OrderByDescending(t => t[1]);

            float[,] Concated = new float[EquipmentCount + 1, DetailsCount];

            int firstGroupLength = FirstGroupDetails.GetLength(1);
            for (int i = 0; i < firstGroupLength; i++)
            {
                Concated[0, i] = FirstGroupDetails[0, i];
                Concated[1, i] = FirstGroupDetails[1, i];
                Concated[2, i] = FirstGroupDetails[2, i];
            }

            for (int i = 0; i < SecondGroupDetails.GetLength(1); i++)
            {
                Concated[0, i + firstGroupLength] = SecondGroupDetails[0, i];
                Concated[1, i + firstGroupLength] = SecondGroupDetails[1, i];
                Concated[2, i + firstGroupLength] = SecondGroupDetails[2, i];
            }

            StartTimes = new float[EquipmentCount, DetailsCount];

            float time = 0;
            for (int i = 0; i < DetailsCount; i++)
            {
                StartTimes[0, i] = time;
                time += Concated[0, i];
            }

            StartTimes[1, 0] = Concated[0, 0];
            for (int i = 1; i < DetailsCount; i++)
            {
                StartTimes[1, i] = Max(StartTimes[0, i] + Concated[0, i], StartTimes[1, i - 1] + Concated[1, i - 1]);
            }

            TotalTime = Max(StartTimes[1, DetailsCount - 1] + Concated[1, DetailsCount - 1],
                            StartTimes[0, DetailsCount - 1] + Concated[0, DetailsCount - 1]);
        }
    }
}
