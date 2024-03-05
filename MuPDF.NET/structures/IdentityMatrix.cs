using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class IdentityMatrix : Matrix
    {
        public IdentityMatrix() : base(1.0f, 1.0f)
        {

        }

        public IdentityMatrix(float a, float b) : base(a, b)
        {

        }

        public float this[string k]
        {
            set
            {
                switch (k)
                {
                    case "A":
                        base.A = 1.0f;
                        break;
                    case "D":
                        base.A = 1.0f;
                        break;
                    case "E":
                        base.E = 0.0f;
                        break;
                    case "B":
                        base.E = 0.0f;
                        break;
                    case "C":
                        base.E = 0.0f;
                        break;
                    case "F":
                        base.E = 0.0f;
                        break;
                    default:
                        PropertyInfo kInfo = base.GetType().GetProperty(k);
                        kInfo.SetValue(this, value, null);
                        break;
                }
            }
        }
    }
}
