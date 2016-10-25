using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathematicalProgramming;

namespace SimplexApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] defaults = {"op", "myData.txt", "myDataout.txt"};

            do
            {
                var command = new string[3];
                defaults.CopyTo(command,0);
                Console.WriteLine("Enter the command:");
                string[] userCommand = Console.ReadLine().Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < userCommand.Length; i++)
                {
                    command[i] = userCommand[i].Trim();
                }
                
                ITask task = null;
                switch (command[0].ToLower())
                {
                    case "lp":
                    {
                        task = new LPTask();
                        break;
                    }
                    case "ilp":
                    {
                        task = new ILPTask();
                        break;
                    }
                    case "ja":
                    {
                        task = new JohnsonTask();
                        break;
                    }
                    case "op":
                    {
                        task = new OPTask();
                        break;
                    }
                    
                    default:
                    {
                        Console.WriteLine("Uncorrect command");
                        break;
                    }
                }

                /*try
                {*/
                    task.ReadDataFromTxt(command[1]);
                //}
                /*catch (Exception e)
                {
                    task = null;
                    Console.WriteLine("Some errors occurred, please check your input and try again.");
                }*/

                if (task != null)
                {
                    task.Resolve();
                    task.WriteResultToTxt(command[2]);
                    Console.WriteLine("Work has been finished successfully!");
                }
                
                Console.WriteLine("Press any key to continue or Esc to exit...\n");
                if (Console.ReadKey(true).Key == ConsoleKey.Escape)break;
            } while (true);
        }
    }
}
