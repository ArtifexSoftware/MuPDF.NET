using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{


    /// <summary>
    /// 
    /// </summary>
    internal class RSCodec
    {
        private int gf_mod;      // module of the Galois Field
        private int poly_degree; //  // GF(2^m) maximum degree of RS-polynomial
        private int log_mod;     // 2^m - 1 size of Lock-up table

        private int[] index_of; // index table for fast multiplication
        private int[] alpha_to; // table of powers of primitive polynomial

        /// <summary>
        /// Initializes a new instance of the <see cref="RSCodec"/> class.
        /// </summary>
        /// <param name="mod">The mod.</param>
        public RSCodec(int mod)
        {
            gf_mod = mod;
            Init();
        }

        private void Init()
        {
            poly_degree = 0;
            int bin = 1;
            while (bin <= gf_mod)
            {
                bin <<= 1;
                poly_degree++;
            }
            bin >>= 1;
            poly_degree--;

            log_mod = (1 << poly_degree) - 1;
            index_of = new int[log_mod + 1];
            alpha_to = new int[log_mod + 1];

            // Calculate Lock-up table
            int mask = 1;
            for (int i = 0; i < log_mod; i++)
            {
                alpha_to[i] = mask;
                index_of[mask] = i;
                mask <<= 1;
                if ((mask & bin) != 0)
                    mask ^= gf_mod;
            }
            index_of[0] = -1;
        }

        private int[] get_gpoly(int ec_length, int index)
        {
            int[] result = new int[ec_length + 1];
            result[0] = 1;
            for (int i = 1; i < result.Length; i++)
            {
                result[i] = 1;
                for (int k = i - 1; k > 0; k--)
                {
                    if (result[k] != 0)
                        result[k] = alpha_to[(index_of[result[k]] + index) % log_mod];
                    result[k] ^= result[k - 1];
                }
                result[0] = alpha_to[(index_of[result[0]] + index) % log_mod];
                index++;
            }
            return result;
        }

        /// <summary>
        /// Encodes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="ec_length">The ec_length.</param>
        /// <returns></returns>
        public int[] Encode(byte[] data, int ec_length)
        {
            // calculating generator polynomial
            int[] g_poly = get_gpoly(ec_length, 1);

            int[] result = new int[ec_length];
            int m;

            for (int i = 0; i < data.Length; i++)
            {
                m = result[ec_length - 1] ^ data[i];
                for (int k = ec_length - 1; k > 0; k--)
                {
                    if ((m != 0) && (g_poly[k] != 0))
                        result[k] = result[k - 1] ^ alpha_to[(index_of[m] + index_of[g_poly[k]]) % log_mod];
                    else
                        result[k] = result[k - 1];
                }
                if ((m != 0) && (g_poly[0] != 0))
                    result[0] = alpha_to[(index_of[m] + index_of[g_poly[0]]) % log_mod];
                else
                    result[0] = 0;
            }

            return result;
        }
    }
}
