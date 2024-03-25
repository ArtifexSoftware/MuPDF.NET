using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class OCLayer
    {
        public int[] On;

        public int[] Off;

        public int[] Locked;

        public List<int[]> RBGroups;

        public string BaseState;
    }
}
