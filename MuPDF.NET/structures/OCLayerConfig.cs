using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class OCLayerConfig
    {
        public int Number;

        public string Name;

        public string Creator;

        public OCLayerConfig(int number, string name, string creator)
        {
            Number = number;
            Name = name;
            Creator = creator;
        }
    }
}
