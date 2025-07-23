using System;

namespace BarcodeReader.Core.Common
{
    class ReedSolomon
    {
        private readonly int modulus;
        private readonly int initZero;
        private const int ErasureCount = 0;
        private readonly int[] poly = new int[13];
        private readonly int[] g;

        private readonly int wordWidth;
        private readonly int fullLength;
        private readonly int dataLength;
        private readonly int ecwHalf;
        private readonly int ecw;
        private readonly int[] exp;
        private readonly int[] log;
        private readonly int[] inputData;
        private bool success;
        private int errorCount;

        public int[] CorrectedData
        {
            get
            {
                int[] correctedData = new int[inputData.Length];
                Array.Copy(inputData, correctedData, inputData.Length);
                Array.Reverse(correctedData);
                return correctedData;
            }
        }

        public bool CorrectionSucceeded
        {
            get
            {
                return success;
            }
        }

        public int ErrorCount
        {
            get
            {
                return errorCount;
            }
        }

        public ReedSolomon(int[] data, int ecwCount, int wW, int polynomCoefficients, int iZero)
        {
            fullLength = data.Length;
            dataLength = fullLength - ecwCount;
            ecwHalf = ecwCount / 2;
            ecw = 2 * ecwHalf;
            wordWidth = wW;
            initZero = iZero;

            g = new int[(1 << (wordWidth + 2))];
            modulus = (1 << wordWidth) - 1;
            exp = new int[modulus + 1];
            log = new int[modulus + 1];

            for (int i = 0; i < 10; i++)
            {
                poly[i] = (polynomCoefficients & (1 << i)) != 0 ? 1 : 0;
            }
            GenerateGaloisField();

            GeneratePoly();
            inputData = new int[data.Length];
            Array.Copy(data, inputData, inputData.Length);
            Array.Reverse(inputData);
            success = false;
            errorCount = 0;
        }

        // http://en.wikipedia.org/wiki/Galois_field
        private void GenerateGaloisField()
        {
            int window = 1;
            exp[wordWidth] = 0;
            for (int i = 0; i < wordWidth; i++)
            {
                exp[i] = window;
                log[exp[i]] = i;
                if (poly[i] != 0)
                {
                    exp[wordWidth] ^= window;
                }
                window <<= 1;
            }

            log[exp[wordWidth]] = wordWidth;
            window >>= 1;
            for (int i = wordWidth + 1; i < modulus; i++)
            {
                if (exp[i - 1] >= window)
                {
                    exp[i] = exp[wordWidth] ^ ((exp[i - 1] ^ window) << 1);
                }
                else
                {
                    exp[i] = exp[i - 1] << 1;
                }
                log[exp[i]] = i;
            }
            log[0] = -1;
        }

        // Compute the generator polynomial of the t-error correcting, length
        // n=(2^m -1) Reed-Solomon code from the product of (X+alpha^i), for
        // i = InitZero, InitZero + 1, ..., InitZero+length-k-1
        private void GeneratePoly()
        {
            g[0] = exp[initZero]; //  <--- vector form of alpha^InitZero
            g[1] = 1; // g(x) = (X+alpha^InitZero)
            for (int i = 2; i <= fullLength - dataLength; i++)
            {
                g[i] = 1;
                for (int j = i - 1; j > 0; j--)
                {
                    if (g[j] != 0)
                    {
                        g[j] = g[j - 1] ^ exp[(log[g[j]] + i + initZero - 1) % modulus];
                    }
                    else
                    {
                        g[j] = g[j - 1];
                    }
                }
                g[0] = exp[(log[g[0]] + i + initZero - 1) % modulus];
            }

            // convert g[] to log form for quicker encoding 
            for (int i = 0; i <= fullLength - dataLength; i++)
            {
                g[i] = log[g[i]];
            }

        }

        // http://en.wikipedia.org/wiki/Berlekamp%E2%80%93Massey_algorithm       
        public void Correct(out float confidence)
        {
            int[] d = new int[(1 << (wordWidth + 2)) + 10];
            int[] l = new int[(1 << (wordWidth + 2)) + 10];
            int[] uLu = new int[(1 << (wordWidth + 2)) + 10];
            int[] syndrome = new int[(1 << (wordWidth + 2)) + 10];
            int[] forney = new int[(1 << (wordWidth + 2)) + 10];
            int[] tau = new int[(1 << (wordWidth + 1)) + 10];
            int[] root = new int[(1 << (wordWidth + 1)) + 10];
            int[] loc = new int[(1 << (wordWidth + 1)) + 10];
            int[] err = new int[(1 << (wordWidth + 2)) + 10];
            int[] reg = new int[(1 << (wordWidth + 1)) + 10];
            int[] omega = new int[(1 << (wordWidth + 2)) + 10];
            int[] phi = new int[(1 << (wordWidth + 2)) + 10];
            int[] phiprime = new int[(1 << (wordWidth + 2)) + 10];
            confidence = 0F;

            int[][] errorLocatorPolynom = new int[modulus + 1][];
            for (int x = 0; x < errorLocatorPolynom.Length; ++x)
            {
                errorLocatorPolynom[x] = new int[(1 << (wordWidth + 2)) + 10];
            }

            // Compute the syndromes
            int synError = 0;
            for (int k = 1; k <= ecw; k++)
            {
                syndrome[k] = 0;
                for (int j = 0; j < fullLength; j++)
                {
                    if (inputData[j] != 0)
                    {
                        syndrome[k] ^= exp[(log[inputData[j]] + (k + initZero - 1) * j) % modulus];
                    }
                }
                // convert syndrome from vector form to log form  */
                if (syndrome[k] != 0)
                {
                    synError++; // set flag if non-zero syndrome => error
                }
                syndrome[k] = log[syndrome[k]];
            }

            if (synError <= 0)
            {
                errorCount = 0;
                success = true;
                confidence = 1F; //100% right
                return;
            }
            syndrome[0] = 0; // S(x) = 1 + s_1x + ...

            tau[0] = 0;
            for (int k = 1; k <= modulus - dataLength; k++)
            {
                forney[k] = syndrome[k];
            }

            // THE BERLEKAMP-MASSEY ALGORITHM FOR ERRORS AND ERASURES
            // initialize table entries
            d[0] = 0; // log form
            d[1] = forney[ErasureCount + 1]; // log form
            errorLocatorPolynom[0][0] = 0; // log form 
            errorLocatorPolynom[1][0] = 1; // vector form 
            for (int k = 1; k < ecw; k++)
            {
                errorLocatorPolynom[0][k] = -1; // log form
                errorLocatorPolynom[1][k] = 0; // vector form 
            }

            l[0] = 0;
            l[1] = 0;
            uLu[0] = -1;
            uLu[1] = 0;
            int u = 0;
            int q;
            if (ErasureCount < ecw)
            {
                // If errors can be corrected
                do
                {
                    u++;
                    if (d[u] == -1)
                    {
                        l[u + 1] = l[u];
                        for (int k = 0; k <= l[u]; k++)
                        {
                            errorLocatorPolynom[u + 1][k] = errorLocatorPolynom[u][k];
                            errorLocatorPolynom[u][k] = log[errorLocatorPolynom[u][k]];
                        }
                    }
                    else
                        // search for words with greatest u_lu[q] for which d[q]!=0
                    {
                        q = u - 1;
                        while ((d[q] == -1) && (q > 0)) q--;
                        // have found first non-zero d[q] 
                        if (q > 0)
                        {
                            int j = q;
                            do
                            {
                                j--;
                                if ((d[j] != -1) && (uLu[q] < uLu[j]))
                                {
                                    q = j;
                                }
                            } while (j > 0);
                        }

                        // have now found q such that d[u]!=0 and u_lu[q] is maximum 
                        // store degree of new elp polynomial 
                        if (l[u] > l[q] + u - q)
                        {
                            l[u + 1] = l[u];
                        }
                        else
                        {
                            l[u + 1] = l[q] + u - q;
                        }

                        // compute new elp(x) 
                        for (int k = 0; k < ecw; k++)
                        {
                            errorLocatorPolynom[u + 1][k] = 0;
                        }
                        for (int k = 0; k <= l[q]; k++)
                        {
                            if (errorLocatorPolynom[q][k] != -1)
                            {
                                errorLocatorPolynom[u + 1][k + u - q] =
                                    exp[(d[u] + modulus - d[q] + errorLocatorPolynom[q][k])%modulus];
                            }
                        }
                        for (int k = 0; k <= l[u]; k++)
                        {
                            errorLocatorPolynom[u + 1][k] ^= errorLocatorPolynom[u][k];
                            errorLocatorPolynom[u][k] = log[errorLocatorPolynom[u][k]];
                        }
                    }

                    uLu[u + 1] = u - l[u + 1];
                    // compute (u+1)th discrepancy 
                    if (u < (ecw - ErasureCount)) // no discrepancy computed on last iteration 
                    {
                        d[u + 1] = forney[ErasureCount + u + 1] != -1 ? exp[forney[ErasureCount + u + 1]] : 0;
                        for (int k = 1; k <= l[u + 1]; k++)
                        {
                            if ((forney[ErasureCount + u + 1 - k] != -1) && (errorLocatorPolynom[u + 1][k] != 0))
                            {
                                d[u + 1] ^= exp[(forney[ErasureCount + u + 1 - k]
                                                 + log[errorLocatorPolynom[u + 1][k]])%modulus];
                            }
                        }
                        d[u + 1] = log[d[u + 1]]; // put d[u+1] into index form 
                    }
                } while ((u < (ecw - ErasureCount)) && (l[u + 1] <= ((ecw - ErasureCount)/2)));
            }

            u++;

            if (l[u] <= ecwHalf - ErasureCount/2) // can correct errors
            {
                confidence = (float)(ecwHalf - l[u]) / (float)(ecwHalf);
                // put elp into index form 
                for (int k = 0; k <= l[u]; k++)
                {
                    errorLocatorPolynom[u][k] = log[errorLocatorPolynom[u][k]];
                }

                // find roots of the error location polynomial 
                for (int k = 1; k <= l[u]; k++)
                {
                    reg[k] = errorLocatorPolynom[u][k];
                }

                int count = 0;
                for (int k = 1; k <= modulus; k++)
                {
                    q = 1;
                    for (int j = 1; j <= l[u]; j++)
                    {
                        if (reg[j] != -1)
                        {
                            reg[j] = (reg[j] + j)%modulus;
                            q ^= exp[reg[j]];
                        }
                    }
                    if (q == 0) // store root and error location number indices 
                    {
                        root[count] = k;
                        loc[count] = modulus - k;

                        count++;
                    }
                }

                if (count == l[u]) // no. roots = degree of elp hence <= t errors 
                {
                    // Compute the errata evaluator polynomial, omega(x)
                    forney[0] = 0; // as a log, to construct 1+T(x)
                    for (int k = 1; k <= ecw; k++)
                    {
                        omega[k] = 0;
                    }
                    for (int k = 0; k <= ecw; k++)
                    {
                        for (int j = 0; j <= l[u]; j++)
                        {
                            {
                                if (k + j <= ecw)
                                    if ((forney[k] != -1) && (errorLocatorPolynom[u][j] != -1))
                                        omega[k + j] ^= exp[(forney[k] + errorLocatorPolynom[u][j])%modulus];
                            }
                        }
                    }
                    for (int k = 0; k <= ecw; k++)
                    {
                        omega[k] = log[omega[k]];
                    }

                    // Compute the errata locator polynomial, phi(x)
                    int degphi = ErasureCount + l[u];
                    for (int k = 1; k <= degphi; k++)
                    {
                        phi[k] = 0;
                    }
                    for (int k = 0; k <= ErasureCount; k++)
                    {
                        for (int j = 0; j <= l[u]; j++)
                        {
                            if ((tau[k] != -1) && (errorLocatorPolynom[u][j] != -1))
                            {
                                phi[k + j] ^= exp[(tau[k] + errorLocatorPolynom[u][j])%modulus];
                            }
                        }
                    }
                    for (int k = 0; k <= degphi; k++)
                    {
                        phi[k] = log[phi[k]];
                    }


                    // Compute the "derivative" of phi(x): phiprime
                    for (int k = 0; k <= degphi; k++)
                    {
                        phiprime[k] = -1; // as a log
                    }
                    for (int k = 0; k <= degphi; k++)
                    {
                        if (k%2 != 0) // Odd powers of phi(x) give terms in phiprime(x)
                        {
                            phiprime[k - 1] = phi[k];
                        }
                    }

                    bool hadError = false;
                    // evaluate errors at locations given by errata locations, loc[i]
                    for (int k = 0; k < degphi; k++)
                    {
                        // compute numerator of error term  
                        err[loc[k]] = 0;
                        for (int j = 0; j <= ecw; j++)
                        {
                            if ((omega[j] != -1) && (root[k] != -1))
                            {
                                err[loc[k]] ^= exp[(omega[j] + j*root[k])%modulus];
                            }
                        }

                        // The term loc[i]^{2-InitZero}
                        if ((err[loc[k]] != 0) && (loc[k] != -1))
                        {
                            err[loc[k]] = exp[(log[err[loc[k]]]
                                               + loc[k]*(2 - initZero + modulus))%modulus];
                        }
                        if (err[loc[k]] != 0)
                        {
                            err[loc[k]] = log[err[loc[k]]];
                            // compute denominator of error term 
                            q = 0;
                            for (int j = 0; j <= degphi; j++)
                            {
                                if ((phiprime[j] != -1) && (root[k] != -1))
                                {
                                    q ^= exp[(phiprime[j] + j*root[k])%modulus];
                                }
                            }

                            // Division by q
                            err[loc[k]] = exp[(err[loc[k]] - log[q] + modulus)%modulus];

                            if (loc[k] < inputData.Length)
                            {
                                inputData[loc[k]] ^= err[loc[k]];
                            }
                            else
                            {
                                hadError = true;
                            }
                        }
                    }

                    errorCount = degphi;
                    success = !hadError;
                }
                // no. roots != degree of elp => >t errors and cannot solve
            }
            // elp has degree has degree >t hence cannot solve 
        }
    }
}