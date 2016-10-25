using System;
using System.Collections.Generic;
using System.IO;

namespace MathematicalProgramming
{
    public class ILPTask : LPTask, ITask
    {
        public LPTask LPSolution { get; private set; }

        public float[] IntegerSolution { get; private set; }

        public new void Resolve()
        {
            float record = OptimizeType == OptimizeTypes.Maximize ? float.MinValue : float.MaxValue;
            ResolveInteger(ref record);
        }

        public new void ReadDataFromTxt(string path)
        {
            base.ReadDataFromTxt(path);
        }

        public new void WriteResultToTxt(string path)
        {
            List<string> outputStr = new List<string>();

            if (IntegerSolution == null)
            {
                outputStr.Add("Restrictions inconsistent, ILP-task has no solution");
            }
            else
            {
                outputStr.Add(IntegerSolution[0].ToString("F2"));
                for (int i = 0; i < InfoVariablesCount; i++)
                {
                    if (IntegerSolution[i + 1] != 0)
                    {
                        outputStr.Add("X" + (i + 1) + " " + IntegerSolution[i + 1].ToString("F2"));
                    }
                }

                int LPpasteIndex = 1 + LPSolution.RestrictionsCount -
                                 (LPSolution.VariablesCount -
                                  (LPSolution.InfoVariablesCount + LPSolution.RestrictionsCount));
                int originPasteIndex = RestrictionsCount - (VariablesCount - (InfoVariablesCount + RestrictionsCount));

                for (int i = 0; i < originPasteIndex; i++)
                {
                    outputStr.Add("Y" + (i + 1) + " " + LPSolution.DualSolution[i].ToString("F2"));
                }
                for (int i = LPpasteIndex; i < LPSolution.RestrictionsCount; i++)
                {
                    outputStr.Add("Y" + (i + 1 - (LPpasteIndex - originPasteIndex)) + " " + LPSolution.DualSolution[i].ToString("F2"));
                }
            }
            File.WriteAllLines(path, outputStr.ToArray());
        }

        private void ResolveInteger(ref float record)
        {
            base.Resolve();

            if (Solution != null)
            {
                if (Solution[0] <= record && OptimizeType == OptimizeTypes.Maximize ||
                    Solution[0] >= record && OptimizeType == OptimizeTypes.Minimize) return;

                bool isInteger = true;
                List<ILPTask> setOfTasks = new List<ILPTask>();
                float[] intSolution = new float[Solution.Length];
                for (int i = 0; i < Solution.Length - 1; i++)
                {
                    if (Solution[i + 1] != Math.Floor(Solution[i + 1]))
                    {
                        isInteger = false;
                        float[] newRestriction = new float[VariablesCount + 2];
                        ILPTask node1 = Clone();
                        newRestriction[0] = (float)Math.Floor(Solution[i + 1]);
                        newRestriction[i + 1] = 1;
                        node1.AddRestriction(newRestriction);
                        newRestriction = new float[VariablesCount + 3];
                        ILPTask node2 = Clone();
                        newRestriction[0] = ((float)Math.Floor(Solution[i + 1]) + 1);
                        newRestriction[i + 1] = 1;
                        node2.AddRestriction(newRestriction);
                        setOfTasks.Add(node1);
                        setOfTasks.Add(node2);
                    }
                    else
                    {
                        intSolution[i + 1] = Solution[i + 1];
                    }
                }

                if (isInteger)
                {
                    intSolution[0] = Solution[0];
                    IntegerSolution = intSolution;
                    LPSolution = (LPTask) this;
                }
                else
                {
                    foreach (ILPTask task in setOfTasks)
                    {
                        task.ResolveInteger(ref record);

                        if (task.IntegerSolution != null)
                        {
                            if (task.IntegerSolution[0] >= record && OptimizeType == OptimizeTypes.Maximize ||
                                task.IntegerSolution[0] <= record && OptimizeType == OptimizeTypes.Minimize )
                            {
                                record = task.IntegerSolution[0];
                                IntegerSolution = task.IntegerSolution;
                                LPSolution = task.LPSolution;                        
                            }
                        }
                    }
                }
            }
        }

        private void AddRestriction(float[] newRestriction)
        {
            float[,] oldRestrictions = Restrictions;
            int pasteIndex = RestrictionsCount - (VariablesCount - (InfoVariablesCount + RestrictionsCount));
            int[] oldBasis;
            float[] oldCoefficients;
            if (newRestriction.Length == VariablesCount + 2)
            {     
                Restrictions = new float[++RestrictionsCount, ++VariablesCount + 1];
                for (int i = 0; i < pasteIndex; i++)
                {
                    for (int j = 0; j < InfoVariablesCount + pasteIndex + 1; j++)
                    {
                        Restrictions[i, j] = oldRestrictions[i, j];
                    }
                    for (int j = InfoVariablesCount + pasteIndex + 2; j < VariablesCount + 1; j++)
                    {
                        Restrictions[i, j] = oldRestrictions[i, j - 1];
                    }
                }

                for (int i = pasteIndex + 1; i < RestrictionsCount; i++)
                {
                    for (int j = 0; j < InfoVariablesCount + pasteIndex + 1; j++)
                    {
                        Restrictions[i, j] = oldRestrictions[i - 1, j];
                    }
                    for (int j = InfoVariablesCount + pasteIndex + 2; j < VariablesCount + 1; j++)
                    {
                        Restrictions[i, j] = oldRestrictions[i - 1, j - 1];
                    }
                }

                for (int i = 0; i < VariablesCount + 1; i++)
                {
                    Restrictions[pasteIndex, i] = newRestriction[i];
                }
                Restrictions[pasteIndex, InfoVariablesCount + pasteIndex + 1] = 1;

                oldBasis = StartBasis;
                StartBasis = new int[RestrictionsCount];
                for (int i = 0; i < pasteIndex; i++)
                {
                    StartBasis[i] = oldBasis[i] > InfoVariablesCount + pasteIndex ? oldBasis[i] + 1 : oldBasis[i];
                }

                for (int i = pasteIndex + 1; i < RestrictionsCount; i++)
                {
                    StartBasis[i] = oldBasis[i - 1] > InfoVariablesCount + pasteIndex
                        ? oldBasis[i - 1] + 1
                        : oldBasis[i - 1];
                }               
                StartBasis[pasteIndex] = InfoVariablesCount + pasteIndex + 1;

                oldCoefficients = Coefficients;
                Coefficients = new float[VariablesCount];
                for (int i = 0; i < InfoVariablesCount + pasteIndex; i++)
                {
                    Coefficients[i] = oldCoefficients[i];
                }

                for (int i = InfoVariablesCount + pasteIndex + 1; i < VariablesCount; i++)
                {
                    Coefficients[i] = oldCoefficients[i - 1];
                }
                Coefficients[InfoVariablesCount + pasteIndex] = 0;
            }
            else if (newRestriction.Length == VariablesCount + 3)
            {
                int artificialIndex = InfoVariablesCount + RestrictionsCount + 1;
                VariablesCount+=2;
                Restrictions = new float[++RestrictionsCount, VariablesCount + 1];
                for (int i = 0; i < pasteIndex; i++)
                {
                    for (int j = 0; j < InfoVariablesCount + pasteIndex + 1; j++)
                    {
                        Restrictions[i, j] = oldRestrictions[i, j];
                    }
                    for (int j = InfoVariablesCount + pasteIndex + 2; j < artificialIndex + 1; j++)
                    {
                        Restrictions[i, j] = oldRestrictions[i, j - 1];
                    }
                    for (int j = artificialIndex + 2; j < VariablesCount + 1; j++)
                    {
                        Restrictions[i, j] = oldRestrictions[i, j - 2];
                    }
                }

                for (int i = pasteIndex + 1; i < RestrictionsCount; i++)
                {
                    for (int j = 0; j < InfoVariablesCount + pasteIndex + 1; j++)
                    {
                        Restrictions[i, j] = oldRestrictions[i - 1, j];
                    }
                    for (int j = InfoVariablesCount + pasteIndex + 2; j < artificialIndex + 1; j++)
                    {
                        Restrictions[i, j] = oldRestrictions[i - 1, j - 1];
                    }
                    for (int j = artificialIndex + 2; j < VariablesCount + 1; j++)
                    {
                        Restrictions[i, j] = oldRestrictions[i - 1, j - 2];
                    }
                }

                for (int i = 0; i < VariablesCount + 1; i++)
                {
                    Restrictions[pasteIndex, i] = newRestriction[i];
                }
                Restrictions[pasteIndex, InfoVariablesCount + pasteIndex + 1] = -1;
                Restrictions[pasteIndex, artificialIndex + 1] = 1;

                oldBasis = StartBasis;
                StartBasis = new int[RestrictionsCount];
                for (int i = 0; i < pasteIndex; i++)
                {
                    if (oldBasis[i] > artificialIndex - 1)
                    {
                        StartBasis[i] = oldBasis[i] + 2;
                    }
                    else if (oldBasis[i] > InfoVariablesCount + pasteIndex)
                    {
                        StartBasis[i] = oldBasis[i] + 1;
                    }
                    else
                    {
                        StartBasis[i] = oldBasis[i];
                    }
                }

                for (int i = pasteIndex + 1; i < RestrictionsCount; i++)
                {
                    if (oldBasis[i - 1] > artificialIndex - 1)
                    {
                        StartBasis[i] = oldBasis[i - 1] + 2;
                    }
                    else if (oldBasis[i - 1] > InfoVariablesCount + pasteIndex)
                    {
                        StartBasis[i] = oldBasis[i - 1] + 1;
                    }
                    else
                    {
                        StartBasis[i] = oldBasis[i - 1];
                    }
                }
                StartBasis[pasteIndex] = artificialIndex + 1;

                oldCoefficients = Coefficients;
                Coefficients = new float[VariablesCount];
                for (int i = 0; i < InfoVariablesCount + pasteIndex; i++)
                {
                    Coefficients[i] = oldCoefficients[i];
                }
                for (int i = InfoVariablesCount + pasteIndex + 1; i < artificialIndex; i++)
                {
                    Coefficients[i] = oldCoefficients[i - 1];
                }
                for (int i = artificialIndex + 1; i < VariablesCount; i++)
                {
                    Coefficients[i] = oldCoefficients[i - 2];
                }
                Coefficients[InfoVariablesCount + pasteIndex] = 0;
                Coefficients[artificialIndex] = OptimizeType == OptimizeTypes.Maximize ? -M : M;
            }
        }

        private ILPTask Clone()
        {
            return new ILPTask
            {
                OptimizeType = OptimizeType,
                VariablesCount = VariablesCount,
                RestrictionsCount = RestrictionsCount,
                InfoVariablesCount = InfoVariablesCount,
                Coefficients = Coefficients.Clone() as float[],
                Restrictions = Restrictions.Clone() as float[,],
                StartBasis = StartBasis.Clone() as int[],
                SimplexTable = SimplexTable.Clone() as float[,],
                OptimizationResult = OptimizationResult,
                M = M
            };

        }
    }
}