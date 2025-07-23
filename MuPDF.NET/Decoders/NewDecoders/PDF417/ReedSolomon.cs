using System;

namespace BarcodeReader.Core.PDF417
{
    // Reed-solomon decoder for PDF417, base3 code
    // http://en.wikipedia.org/wiki/Reed%E2%80%93Solomon_error_correction
    // http://en.wikiversity.org/wiki/Reed%E2%80%93Solomon_codes_for_coders
    // http://grandzebu.net/informatique/codbar-en/pdf417.htm
    class ReedSolomon
    {

        private const int Gprime = 929;
        private const int A0 = 928;

        private static readonly int[] Exp = new int[1024];
        private static readonly int[] Log = new int[1024];

        static ReedSolomon()
        {
            int powerOf3 = 1;
            Log[1] = Gprime - 1;

            for (int ii = 0; ii < Gprime - 1; ii += 1)
            {
                Exp[ii] = powerOf3;
                if (powerOf3 < Gprime)
                {
                    if (ii != Gprime - 1) Log[powerOf3] = ii;
                }
                else
                {
                    throw new Exception("Internal error: powers of 3 calculation");
                }

                powerOf3 = (powerOf3 * 3) % Gprime;
            }
            Log[0] = Gprime - 1;
            Exp[Gprime - 1] = 1;
            Log[Gprime] = A0;
        }

        private static int Modulo(int x)
        {
            return x % (Gprime - 1);
        }

        public int[] correcteddata = null;

        // data has the error correction codewords at the end of the array
        // erasurepositions has a 0-based indexing, 0 = first data word, (datalength-1) = last error correction codeword
        public bool Correct(int[] data, int[] erasurePositions, int errorCorrWordCount, out float confidence)
        {
            // confidence level (from 0 to 1.0f)
            confidence = 0F;

            if (erasurePositions.Length > errorCorrWordCount)
                return false;

            // make a copy of the data that will be treated as correct data
            correcteddata = new int[data.Length];
            Array.Copy(data, correcteddata, data.Length);

            // number of erasures we have (zero or more)
            int erasureCount = erasurePositions.Length;

            // input data length
            int dataLength = data.Length;

            // temporary variable 
            int tmp;
            // lambda 
            int[] lambda = new int[2048 + 1];
            // syndrome calculations
            int[] syndrome = new int[2048 + 1]; /* Err+Eras Locator poly
					 * and syndrome poly */
            int[] b = new int[2048 + 1];
            int[] t = new int[2048 + 1];
            int[] omega = new int[2048 + 1];
            // roots
            int[] root = new int[2048];
            int[] reg = new int[2048 + 1];

            // array with locations (as indexes) of errors and erasures 
            // that we should correct
            int[] errorLocations = new int[2048];
            int ci;

            // convert & reverse the erasurePositions array
            // we will need this erasures array when we are checking 
            // found error locations to see if we found all erasures
            for (int i = 0; i < erasureCount; ++i)
            {
                erasurePositions[i] = dataLength - erasurePositions[i];
            }

            /* form the syndromes; i.e. evaluate data(x) at roots of g(x)
               namely @**(1+i)*Prim, i = 0, ... , (NN-KK-1) */
            //int[] syn2 = new int[errorCorrWordCount + 1];
            for (int i = 1; i <= errorCorrWordCount; i++)
            {
                syndrome[i] = 0; //data[data_len];
              //  syn2[i] = 0;
            }

            // calculate the syndrome
            for (int j = 1; j <= dataLength; j++)
            {
                // check if we have erasure at this position
                // erasure is marked with -1 or 0 (zero)
                if (correcteddata[dataLength - j] <= 0)
                {
                    correcteddata[dataLength - j] = 0;
                    continue;
                }

                tmp = Log[correcteddata[dataLength - j]];

                // evaluate the data
                /*  s[i] ^= AlphaTo[modbase(tmp + (1+i-1)*j)]; */
                for (int i = 1; i <= errorCorrWordCount; i++)
                {
                    syndrome[i] = (syndrome[i] + Exp[Modulo(tmp + (i) * j)]) % Gprime;
                }
            }

            /* // commented by eugene on 27 march 2016 as it actually duplicates the blog above
            for (int i = 0; i < errorCorrWordCount; i++)
            {
                int x = Exp[i + 1];
                //Console.WriteLine("Eval at x=" + x);
                int result = correcteddata[0];
                for (int j = 1; j < dataLength; j++)
                {
                    int coef = result != 0 ? Exp[(Log[result] + Log[x]) % (Gprime - 1)] : 0;
                    //Console.WriteLine("  x=" + x + "*result=" + result + "=" + coef);
                    result = (coef + correcteddata[j]) % Gprime;
                    //Console.WriteLine("  -->result:" + result);
                }
                syn2[i] = result;
                //syndrome[i + 1] = result;
                //Console.WriteLine("================================================");
            }
             */

            /* Convert syndromes to index form, checking for nonzero condition */
            int synError = 0;
            for (int i = 1; i <= errorCorrWordCount; i++)
            {
                if (syndrome[i] != 0)
                    synError++;
                syndrome[i] = Log[syndrome[i]];
            }

            if (synError == 0)
            {
                /* if syndrome is zero, correcteddata[] is a codeword and there are no
                 * errors to correct. So return data[] unmodified
                 */
                confidence = 1F; //100% right
                return true;
            }

            // reset lambda degree to zero
            int degLambda = 0;

            int primitive = 1; // initial primitive set to 1

            /*                
                        // clear arrays
                        for (int ii = 0; ii < b.Length; ii++)
                        {
                            b[ii] = 0;
                            reg[ii] = 0;
                            t[ii] = 0;
                        }
                        for (int ii = 0; ii < root.Length; ii++)
                        {
                            root[ii] = 0;
                            loc[ii] = 0;
                        }
            */

            // put zero into lambda 
            for (ci = errorCorrWordCount - 1; ci >= 0; ci--)
                lambda[ci + 1] = 0;

            lambda[0] = 1; // start lamda with 1

            // check if we should proceed erasures
            if (erasureCount > 0)
            {
                /* Init lambda to be the erasure locator polynomial */
                lambda[1] = Exp[Modulo(primitive * erasurePositions[0])];
                for (int i = 1; i < erasureCount; i++)
                {
                    int u = Modulo(primitive * erasurePositions[i]);
                    for (int j = i + 1; j > 0; j--)
                    {
                        tmp = Log[lambda[j - 1]];
                        if (tmp != A0)
                            lambda[j] = (lambda[j] + Exp[Modulo(u + tmp)]) % Gprime;
                    }
                }
            }

            for (int i = 0; i < errorCorrWordCount + 1; i++)
                b[i] = Log[lambda[i]];

            /*
             * Begin Berlekamp-Massey algorithm to determine error+erasure
             * locator polynomial
             * and find locations of erasures and errors
             * we may have zero or more additional errors (in addition to erasures)
             * we should find all erasures + additional errors (if any)
             */
            int r = erasureCount;
            while (++r <= errorCorrWordCount)
            {
                /* r is the step number */
                /* Compute discrepancy at the r-th step in poly-form */
                int rThDiscr = 0;
                for (int i = 0; i < r; i++)
                {
                    if ((lambda[i] != 0) && (syndrome[r - i] != A0))
                    {
                        if (i % 2 == 1)
                        {
                            rThDiscr = (rThDiscr + Exp[Modulo((Log[lambda[i]] + syndrome[r - i]))]) % Gprime;
                        }
                        else
                        {
                            rThDiscr = (rThDiscr + Gprime - Exp[Modulo((Log[lambda[i]] + syndrome[r - i]))]) % Gprime;
                        }
                    }
                }

                rThDiscr = Log[rThDiscr]; /* Index form */

                if (rThDiscr == A0)
                {
                    /* 2 lines below: B(x) <-- x*B(x) */
                    //  COPYDOWN(&b[1],b,synd_len);
                    //
                    for (ci = errorCorrWordCount - 1; ci >= 0; ci--) b[ci + 1] = b[ci];
                    b[0] = A0;
                }
                else
                {
                    /* 7 lines below: T(x) <-- lambda(x) - discr_r*x*b(x) */
                    /*  the T(x) will become the next lambda */

                    t[0] = lambda[0];
                    for (int i = 0; i < errorCorrWordCount; i++)
                    {
                        if (b[i] != A0)
                        {

                            //  t[i+1] =  (lambda[i+1] + Gprime -
                            //              AlphaTo[modbase(discr_r + Gprime - 1 -  b[i])]) % Gprime;
                            t[i + 1] = (lambda[i + 1] + Exp[Modulo(rThDiscr + b[i])]) % Gprime;

                        }
                        else
                        {
                            t[i + 1] = lambda[i + 1];
                        }
                    }
                    if (0 <= r + erasureCount - 1)
                    {
                        /*
                         * 2 lines below: B(x) <-- inv(discr_r) *
                         * lambda(x)
                         */
                        for (int i = 0; i <= errorCorrWordCount; i++)
                        {

                            b[i] = lambda[i] == 0 ? A0 : Modulo(Log[lambda[i]] - rThDiscr + Gprime - 1);
                        }

                    }
                    else
                    {
                        /* 2 lines below: B(x) <-- x*B(x) */
                        //      COPYDOWN(&b[1],b,synd_len);
                        for (ci = errorCorrWordCount - 1; ci >= 0; ci--) b[ci + 1] = b[ci];
                        b[0] = A0;
                    }
                    //      COPY(lambda,t,synd_len+1);

                    for (ci = errorCorrWordCount + 1 - 1; ci >= 0; ci--)
                    {
                        lambda[ci] = t[ci];
                    }
                }
            }

            /* Convert lambda to index form and compute deg(lambda(x)) */
            degLambda = 0;
            for (int i = 0; i < errorCorrWordCount + 1; i++)
            {

                lambda[i] = Log[lambda[i]];

                if (lambda[i] != A0)
                    degLambda = i;

            }

            /*
             * Find roots of the error+erasure locator polynomial by Chien
             * Search
             */

            for (ci = errorCorrWordCount - 1; ci >= 0; ci--)
                reg[ci + 1] = lambda[ci + 1];

            // counting errors found (should be equal or larger than erasures count)
            int errorCount = 0; /* Number of roots of lambda(x) */

            // we are counting number of duplicated errors (if the same location is the error but was marked as erasure previously
            // it should be equal to erasuresCount
            int foundErasuresCount = 0;

            for (int i = 1, k = dataLength - 1; i <= Gprime; i++)
            {
                int q = 1;
                for (int j = degLambda; j > 0; j--)
                {

                    if (reg[j] != A0)
                    {
                        reg[j] = Modulo(reg[j] + j);
                        //      q = modbase( q +  AlphaTo[reg[j]]);
                        if (degLambda != 1)
                        {
                            if (j % 2 == 0)
                            {
                                q = (q + Exp[reg[j]]) % Gprime;
                            }
                            else
                            {
                                q = (q + Gprime - Exp[reg[j]]) % Gprime;
                            }
                        }
                        else
                        {
                            q = Exp[reg[j]] % Gprime;
                            if (q == 1) --q;
                        }
                    }
                }

                if (q == 0)
                {
                    /* store root (index-form) and error location number */
                    root[errorCount] = i;

                    // storing the inverted location index (index of the codeword with error)
                    errorLocations[errorCount] = Gprime - 1 - i;

                    // increase error count 
                    if (errorCount < errorCorrWordCount)
                    {
                        errorCount += 1;
                    }
                    else
                    {
                        // exit if we exceeded the number of correction codewords available
                        return false;
                    }

                    // check if the error location is erasure or not
                    if (errorLocations[errorCount - 1] <= dataLength)
                    {
                        // the location index of the error
                        int CurLoc = errorLocations[errorCount - 1];

                        // now check if we already have this error duplicated as erasure?
                        // NOTE: erasure positions already contain inverted locations (was inverted above at the beginning)
                        foreach (int ii in erasurePositions)
                        {
                            if (CurLoc == ii)
                            {
                                // increase number of errors
                                foundErasuresCount++;
                                break;
                            }
                        }


                    }
                    else
                        // exit if for some reason new error location 
                        // exceeds the length of the input data
                        return false;
                }

                if (k == 0)
                {
                    k = dataLength - 1;
                }
                else
                {
                    k -= 1;
                }

                /* If we've already found max possible roots,
                 * abort the search to save time
                 */

                if (errorCount >= degLambda)
                    break;

            }

            // if degreee lambda != error count then we should exit
            if (degLambda != errorCount)
            {
                /*
                 * deg(lambda) unequal to number of roots => uncorrectable
                 * error detected
                 */
                return false;
            }

            // check if we have more than zero erasures defined
            if (erasureCount > 0)
            {
                // check if we have found less errors than erasures
                // we should find at least the errors >= erasures count!
                if (errorCount < erasureCount)
                    return false;

                // exit if we have NOT found all erasures
                // (we should find all erasures as errors!)
                if (foundErasuresCount != erasureCount)
                    return false;

                // finally we should exit if the total number of errors found
                // exceed amount of error correction words available

                // foundErasureCount = number of erasuers at the same places as errors, i.e. the symbol at the index is both error and erasure
                // this way the number of unique erasures = (erasureCount - foundErasuresCount)
                // errorCount = number of unique corrections we have made (including errors and erasure alltogether)
                //if (errorCount != errorCorrWordCount) // we should not have errors more than error code words
                //    return false;
                if ((errorCount + (erasureCount - foundErasuresCount)) > errorCorrWordCount-1)
                    return false;
            }

            // calculate the confidence
            confidence = (float)(errorCorrWordCount - errorCount) / (float)(errorCorrWordCount);

            // checking the current confidence
            // fail if confidence iz zero
            if (confidence == 0.0f)
            {
                // confidence = 0f;
                return false;
            }

            /*
             * Compute err+eras evaluator poly omega(x) = s(x)*lambda(x) (modulo
             * x**(synd_len)). in index form. Also find deg(omega).
             */
            int degOmega = 0;
            for (int i = 0; i < errorCorrWordCount; i++)
            {
                tmp = 0;
                int j = (degLambda < i) ? degLambda : i;
                for (; j >= 0; j--)
                {
                    if ((syndrome[i + 1 - j] != A0) && (lambda[j] != A0))
                    {
                        if (j % 2 == 1)
                        {
                            tmp = (tmp + Gprime - Exp[Modulo(syndrome[i + 1 - j] + lambda[j])]) % Gprime;
                        }
                        else
                        {

                            tmp = (tmp + Exp[Modulo(syndrome[i + 1 - j] + lambda[j])]) % Gprime;
                        }
                    }
                }

                if (tmp != 0) degOmega = i;
                omega[i] = Log[tmp];
            }
            omega[errorCorrWordCount] = A0;

            /*
             * Compute error values in poly-form. num1 = omega(inv(X(l))), num2 =
             * inv(X(l))**(B0-1) and den = lambda_pr(inv(X(l))) all in poly-form
             */
            for (int j = errorCount - 1; j >= 0; j--)
            {
                int num1 = 0;
                for (int i = degOmega; i >= 0; i--)
                {
                    if (omega[i] != A0)
                    {
                        //    num1  = ( num1 + AlphaTo[modbase(omega[i] + (i * root[j])]) % Gprime;
                        num1 = (num1 + Exp[Modulo(omega[i] + ((i + 1) * root[j]))]) % Gprime;
                    }
                }

                // denominator if product of all (1 - Bj Bk) for k != j
                // if count = 1, then den = 1

                int den = 1;
                for (int k = 0; k < errorCount; k += 1)
                {
                    if (k != j)
                    {
                        tmp = (1 + Gprime - Exp[Modulo(Gprime - 1 - root[k] + root[j])]) % Gprime;
                        den = Exp[Modulo(Log[den] + Log[tmp])];
                    }
                }

                if (den == 0)
                {
                    /* Convert to dual- basis */
                    errorCount = -1;
                    return false;
                }

                int errorVal = Exp[Modulo(Log[num1] + Log[1] +
                                               Gprime - 1 - Log[den])] % Gprime;

                /* Apply error to data */
                if (num1 != 0)
                {
                    if (errorLocations[j] < dataLength + 1)
                    {
                        int fixLoc = -1;
                        fixLoc = dataLength - errorLocations[j];

                        if (fixLoc < dataLength + 1)
                        {
                            int newval = (correcteddata[fixLoc] + Gprime - errorVal) % Gprime;
                            correcteddata[fixLoc] = newval;
                        }
                    }
                }
            }

            return true;
        }

    }
}
