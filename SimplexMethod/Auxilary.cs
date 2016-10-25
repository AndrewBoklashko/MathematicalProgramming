using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathematicalProgramming
{
    public class FMM
    {
        public int Index;
        public List<Detail> Bag;
        public Detail CurrentDetail;
        public IRuleHandler RuleHandler;
        public List<DetailPos> DetailQueue;
        public float EndTime;

        public void SetDetail()
        {
            RuleHandler.SetDetail(this);
            if (CurrentDetail != null)
            {
                CurrentDetail.CurrentEndTime = EndTime + CurrentDetail.CurrentDuration;
                EndTime = CurrentDetail.CurrentEndTime;
            }
        }

        public void AddToQueue()
        {
            DetailPos current = new DetailPos
            {
                Index = CurrentDetail.Index,
                StartTime = CurrentDetail.StartTimes[CurrentDetail.OpNumber],
                Duration = CurrentDetail.CurrentDuration
            };

            DetailPos last = DetailQueue.LastOrDefault() ?? new DetailPos {Index = -1, Duration = 0, StartTime = 0};

            float idleDuration = current.StartTime - (last.StartTime + last.Duration);
            if (idleDuration > 0.01)
            {
                DetailPos idle = new DetailPos
                {
                    Index = 0,
                    StartTime = last.StartTime + last.Duration,
                    Duration = idleDuration
                };
                DetailQueue.Add(idle);
            }
            DetailQueue.Add(current);
        }

        public void AddToQueue(DetailPos dp, FMM currentModule)
        {
            if (dp.Index != 15)
            {
                float time = dp.StartTime;
                DetailPos last = DetailQueue.LastOrDefault() ?? new DetailPos { Index = -1, Duration = 0, StartTime = 0 };
                dp.StartTime = Math.Max(EndTime, currentModule.CurrentDetail.CurrentEndTime - currentModule.CurrentDetail.CurrentDuration);
                float idleDuration = dp.StartTime - (last.StartTime + last.Duration);
                if (idleDuration > 0.01)
                {
                    DetailPos idle = new DetailPos
                    {
                        Index = 0,
                        StartTime = last.StartTime + last.Duration,
                        Duration = idleDuration
                    };
                    DetailQueue.Add(idle);
                }
                DetailQueue.Add(dp);


                currentModule.CurrentDetail.CurrentEndTime += dp.Duration + EndTime - (time - currentModule.CurrentDetail.CurrentDuration);
                EndTime = currentModule.CurrentDetail.CurrentEndTime - currentModule.CurrentDetail.CurrentDuration + idleDuration;
            }
            else
            {
                DetailQueue.Add(dp);
                EndTime += dp.Duration;
            }
        }
    }

    public class Detail
    {
        public int Index;
        public int OpNumber;
        public FMM[] Route;
        public float[] Durations;
        public float[] StartTimes;

        public FMM CurrentModule => Route[OpNumber];
        public float CurrentEndTime;
        public float CurrentDuration => Durations[OpNumber];

        public void ToNextOperation()
        {
            OpNumber++;
            if(OpNumber > Route.Length - 1)return;
            if (!CurrentModule.Bag.Any())
            {
                CurrentModule.EndTime = CurrentEndTime;
            }
            CurrentModule.Bag.Add(this);
        }

        public void RemoveFromBag()
        {
            CurrentModule.Bag.Remove(this);
        }

        public void ResetCurrentModule()
        {
            OpNumber = 0;
        }
    }

    public class DetailPos
    {
        public int Index { get; set; }
        public float StartTime { get; set; }
        public float Duration { get; set; }
    }

    /*public static class Helpers
    {
        public static T[] InitializeArray<T>(int length) where T : new()
        {
            T[] array = new T[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = new T();
            }

            return array;
        }
    }
    */
}
