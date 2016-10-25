using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;

namespace MathematicalProgramming
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build
    public class LPTask: ITask
    {
        public enum OptimizeTypes
        {
            Minimize,
            Maximize
        }

        public enum OptimizationResults
        {
            NotOptimized,
            CantOptimize,
            Optimized
        }

        public OptimizeTypes OptimizeType { get; protected set; }
        public int VariablesCount { get; protected set; }
        public int RestrictionsCount { get; protected set; }
        public int InfoVariablesCount { get; protected set; }
        public float[] Coefficients { get; protected set; }
        public float[,] Restrictions { get; protected set; }
        public int[] StartBasis { get; protected set; }
        public float M { get; protected set; }
        public float[,] SimplexTable { get; protected set; }
        public OptimizationResults OptimizationResult { get; protected set; }
        public float[] Solution { get; private set; }
        public float[] DualSolution { get; private set; }

        public void Resolve()
        {
            FillSimplexTable();

            for(int l=0; l<100; l++)
            { 
                OptimizationResult = OptimizationResults.Optimized;
                SimplexTable[RestrictionsCount + 1, 0] = SimplexTable[RestrictionsCount + 1, 1] = 0;
                for (int i = 0; i < InfoVariablesCount + RestrictionsCount + 1; i++)
                {
                    float S = 0;
                    for (int j = 0; j < RestrictionsCount; j++)
                    {
                        int index = Convert.ToInt32(SimplexTable[j + 1, 0]);
                        S += SimplexTable[0, index+1] * SimplexTable[j+1, i+1];
                    }
                    float D = S - SimplexTable[0, i + 1];
                    if (OptimizeType == OptimizeTypes.Maximize && D < 0)
                        OptimizationResult = OptimizationResults.NotOptimized;
                    if (OptimizeType == OptimizeTypes.Minimize && D > 0)
                        OptimizationResult = OptimizationResults.NotOptimized;
                    SimplexTable[RestrictionsCount + 1, i + 1] = D;
                }
                if (OptimizationResult == OptimizationResults.Optimized)
                    break;

                int colIndex = -1;
                int rowIndex = -1;
                /*float minR = 0, minC = 0;
                for (int j = 0; j < RestrictionsCount; j++)
                {
                    if (SimplexTable[j + 1, 1] < minR)
                    {
                        minR = SimplexTable[j + 1, 1];
                        rowIndex = j;
                    }
                }
                if (rowIndex != -1)
                {
                    for (int i = 0; i < VariablesCount; i++)
                    {
                        if (SimplexTable[rowIndex + 1, i + 2] < minC)
                        {
                            minC = SimplexTable[rowIndex + 1, i + 2];
                            colIndex = i;
                        }
                    }

                    if (colIndex == -1)
                    {
                        OptimizationResult = OptimizationResults.CantOptimize;
                        break;
                    }
                }
                else
                {*/
                float extremeD = OptimizeType == OptimizeTypes.Minimize ? int.MinValue : int.MaxValue;
                for (int i = 0; i < VariablesCount; i++)
                {
                    bool allNegPos;
                    if (OptimizeType == OptimizeTypes.Minimize)
                    {
                        if (SimplexTable[RestrictionsCount + 1, i + 2] >= extremeD)
                        {
                            allNegPos = true;
                            for (int j = 0; j < RestrictionsCount; j++)
                            {
                                if (SimplexTable[j + 1, i + 2] > 0)
                                {
                                    allNegPos = false;
                                    break;
                                }
                            }
                            if (allNegPos)
                            {
                                OptimizationResult = OptimizationResults.CantOptimize;
                                break;
                            }
                            extremeD = SimplexTable[RestrictionsCount + 1, i + 2];
                            colIndex = i;
                        }
                    }
                    else
                    {
                        if (SimplexTable[RestrictionsCount + 1, i + 2] <= extremeD)
                        {
                            allNegPos = true;
                            for (int j = 0; j < RestrictionsCount; j++)
                            {
                                if (SimplexTable[j + 1, i + 2] > 0)
                                {
                                    allNegPos = false;
                                    break;
                                }
                            }
                            if (allNegPos)
                            {
                                OptimizationResult = OptimizationResults.CantOptimize;
                                break;
                            }
                            extremeD = SimplexTable[RestrictionsCount + 1, i + 2];
                            colIndex = i;
                        }
                    }
                }

                if (OptimizationResult == OptimizationResults.CantOptimize) break;

                float ratio = float.MaxValue;
                OptimizationResult = OptimizationResults.CantOptimize;
                for (int i = 0; i < RestrictionsCount; i++)
                {
                    if (SimplexTable[i + 1, colIndex + 2] > 0)
                    {
                        OptimizationResult = OptimizationResults.NotOptimized;
                        float temp = SimplexTable[i + 1, 1]/SimplexTable[i + 1, colIndex + 2];
                        if (temp <= ratio && temp >= 0)
                        {
                            ratio = temp;
                            rowIndex = i;
                        }
                    }
                }
                if (OptimizationResult == OptimizationResults.CantOptimize) break;
                //}

                for (int i = 0; i < RestrictionsCount; i++)
                {
                    for (int j = 0; j < VariablesCount+1; j++)
                    {
                        if (i != rowIndex && j != colIndex + 1)
                        {
                            SimplexTable[i + 1, j + 1] -= (SimplexTable[rowIndex + 1, j + 1]*
                                                           SimplexTable[i + 1, colIndex + 2])/
                                                          SimplexTable[rowIndex + 1, colIndex + 2];
                        }
                    }
                }

                SimplexTable[rowIndex + 1, 1] /= SimplexTable[rowIndex + 1, colIndex + 2];
                float temporary = SimplexTable[rowIndex + 1, colIndex + 2];
                SimplexTable[rowIndex + 1, 0] = colIndex+1;
                for (int i = 0; i < VariablesCount; i++)
                {
                    SimplexTable[rowIndex + 1, i + 2] /= temporary;
                }
                for (int i = 0; i < RestrictionsCount; i++)
                {
                    SimplexTable[i + 1, colIndex + 2] = (i != rowIndex) ? 0 : 1;
                }
            }

            for (int i = 0; i < RestrictionsCount; i++)
            {
                if (SimplexTable[i + 1, 0] > InfoVariablesCount + RestrictionsCount)
                {
                    OptimizationResult = OptimizationResults.CantOptimize;
                    break;
                }
            }

            if (OptimizationResult == OptimizationResults.Optimized)
            {
                Solution = new float[InfoVariablesCount + 1];
                DualSolution = new float[RestrictionsCount];
                Solution[0] = SimplexTable[RestrictionsCount + 1, 1];
                for (int i = 0; i < RestrictionsCount; i++)
                {
                    if (SimplexTable[i + 1, 0] <= InfoVariablesCount && SimplexTable[i+1, 0] >= 1)
                    {
                        Solution[Convert.ToInt32(SimplexTable[i + 1, 0])] = SimplexTable[i + 1, 1];
                    }
                }

                for (int i = 0; i < DualSolution.Length; i++)
                {
                    DualSolution[i] = SimplexTable[RestrictionsCount + 1, InfoVariablesCount + 2 + i];
                }
            }
            else
            {
                Solution = DualSolution = null;
            }
        }

        public void ReadDataFromTxt(string path)
        {
            string[] lines = File.ReadAllLines(path);
            if (Int32.Parse(lines[0]) == 0) OptimizeType = OptimizeTypes.Minimize;
            else OptimizeType = OptimizeTypes.Maximize;
            VariablesCount = Int32.Parse(lines[1]);
            RestrictionsCount = Int32.Parse(lines[2]);
            InfoVariablesCount = Int32.Parse(lines[3]);
            string[] coefsString = lines[4].Split(new[] { ' ' }, VariablesCount);
            
            M = 0;
            Restrictions = new float[RestrictionsCount, VariablesCount + 1];
            for (int i = 0; i < RestrictionsCount; i++)
            {
                Restrictions[i, 0] = float.Parse(lines[5 + 2 * i]);
                string[] restrString = lines[6 + 2 * i].Split(new[] { ' ' }, VariablesCount);
                for (int j = 0; j < VariablesCount; j++)
                {
                    Restrictions[i, j + 1] = float.Parse(restrString[j].Trim());
                    if (Math.Abs(Restrictions[i, j + 1]) > M)
                    {
                        M = Restrictions[i, j + 1];
                    }
                }
            }
            M *= 1000;

            Coefficients = new float[VariablesCount];
            for (int i = 0; i < VariablesCount; i++)
            {
                string subStr = coefsString[i].Trim().ToLower();
                if (subStr == "m")
                {
                    Coefficients[i] = M;
                }
                else if (subStr == "-m")
                {
                    Coefficients[i] = -M;
                }
                else
                {
                    Coefficients[i] = float.Parse(subStr);
                }
            }

            string[] startBasisString = lines[5 + 2 * RestrictionsCount].Split(new[] { ' ' }, RestrictionsCount);
            StartBasis = new int[RestrictionsCount];
            for (int i = 0; i < RestrictionsCount; i++)
            {
                StartBasis[i] = int.Parse(startBasisString[i].Trim());
            }
        }

        public void WriteResultToTxt(string path)
        {
            List<string> outputStr = new List<string>();

            if (Solution == null)
            {
                outputStr.Add("Restrictions inconsistent, LP-task has no solution");
            }
            else
            {
                outputStr.Add(Solution[0].ToString("F2"));
                for (int i = 0; i < InfoVariablesCount; i++)
                {
                    if (Solution[i + 1] != 0)
                    {
                        outputStr.Add("X" + (i + 1) + " " + Solution[i + 1].ToString("F2"));
                    }
                }
                for (int i = 0; i < RestrictionsCount; i++)
                {
                    outputStr.Add("Y" + (i + 1) + " " + DualSolution[i].ToString("F2"));
                }
            }
            File.WriteAllLines(path,outputStr.ToArray());
        }

        private void FillSimplexTable()
        {

            int strCount = RestrictionsCount + 2;
            int colCount = VariablesCount + 2;
            SimplexTable = new float[strCount, colCount];
            SimplexTable[0, 0] = SimplexTable[0, 1] = 0;

            for (int i = 0; i < Coefficients.Length; i++)
            {
                SimplexTable[0, i + 2] = Coefficients[i];
            }

            for (int i = 0; i < StartBasis.Length; i++)
            {
                SimplexTable[i + 1, 0] = StartBasis[i];
            }

            for (int i = 0; i < RestrictionsCount; i++)
            {
                SimplexTable[i + 1, 1] = Restrictions[i, 0];
                for (int j = 0; j < VariablesCount; j++)
                {
                    SimplexTable[i + 1, j + 2] = Restrictions[i, j + 1];
                }
            }
        }
    }
}