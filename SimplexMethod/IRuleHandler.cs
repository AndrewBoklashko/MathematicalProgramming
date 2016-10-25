using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MathematicalProgramming
{
    public interface IRuleHandler
    {
        void SetDetail(FMM module);
        string Name { get; }
    }

    public class ShortestOpHandler : IRuleHandler
    {
        public void SetDetail(FMM module)
        {
            module.CurrentDetail =
                (from detail in module.Bag
                    where detail.CurrentDuration == module.Bag.Min(d => d.CurrentDuration)
                    select detail)
                    .FirstOrDefault();
        }

        public string Name => "Правило найкоротшої операції";
    }

    public class LongestOpHandler : IRuleHandler
    {
        public void SetDetail(FMM module)
        {
            module.CurrentDetail =
                (from detail in module.Bag
                    where detail.CurrentDuration == module.Bag.Max(d => d.CurrentDuration)
                    select detail)
                        .FirstOrDefault();
        }

        public string Name => "Правило найдовшої операції";
    }

    public class MaxRestComplexityHandler : IRuleHandler
    {
        public void SetDetail(FMM module)
        {
            float maxRestTime = 0;
            Detail chosen = null;

            foreach (var detail in module.Bag)
            {
                float restTime = 0;
                for (int i = detail.OpNumber; i < detail.Durations.Length; i++)
                {
                    restTime += detail.Durations[i];
                }

                if (restTime >= maxRestTime)
                {
                    maxRestTime = restTime;
                    chosen = detail;
                }
            }

            module.CurrentDetail = chosen;
        }

        public string Name => "Правило максимальної залишкової трудомісткості";
    }

    public class MinRestComplexityHandler : IRuleHandler
    {
        public void SetDetail(FMM module)
        {
            float minRestTime = float.MaxValue;
            Detail chosen = null;

            foreach (var detail in module.Bag)
            {
                float restTime = 0;
                for (int i = detail.OpNumber; i < detail.Durations.Length; i++)
                {
                    restTime += detail.Durations[i];
                }

                if (restTime <= minRestTime)
                {
                    minRestTime = restTime;
                    chosen = detail;
                }
            }

            module.CurrentDetail = chosen;
        }

        public string Name => "Правило мінімальної залишкової трудомісткості";
    }

    public class FIFOHandler : IRuleHandler
    {
        public void SetDetail(FMM module)
        {
            module.CurrentDetail = module.Bag.First();
        }

        public string Name => "Правило FIFO";
    }

    public class LIFOHandler : IRuleHandler
    {
        public void SetDetail(FMM module)
        {
            module.CurrentDetail = module.Bag.Last();
        }

        public string Name => "Правило LIFO";
    }

    public class SmoothLoadHandler : IRuleHandler
    {
        public void SetDetail(FMM module)
        {
            float minTime = float.MaxValue;
            Detail chosen = null;

            foreach (var detail in module.Bag)
            {
                int index = detail.OpNumber + 1;

                float time = index < detail.Route.Length ? detail.Route[index].Bag.Sum(d => d.CurrentDuration) : float.MaxValue;

                if (time <= minTime)
                {
                    minTime = time;
                    chosen = detail;
                }
            }

            module.CurrentDetail = chosen;
        }

        public string Name => "Правило вирівнювання завантаження верстатів";
    }
}
