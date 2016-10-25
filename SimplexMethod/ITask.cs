using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MathematicalProgramming
{
    public interface ITask
    {
        void Resolve();
        void ReadDataFromTxt(string path);
        void WriteResultToTxt(string path);
    }
}
