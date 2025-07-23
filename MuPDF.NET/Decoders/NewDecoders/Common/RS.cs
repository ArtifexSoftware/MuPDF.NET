using System;
using System.Collections.Generic;

namespace BarcodeReader.Core.Common
{
    class RS
    {
        GF gf;  //Galois Field used for this Reed Solomon
        int[] msg; //Message to validate/correct
        int ecw; //number of error correcting words in msg
        bool generatorPolyHasPower0;
        public int[] correctedData;

        //msg: message to validate/correct
        //ecw: number of error correcting words
        public RS(GF gf, int[] msg, int ecw, bool generatorPolyHasPower0)
        {
            this.gf = gf;
            this.msg = msg;
            this.ecw = ecw;
            this.generatorPolyHasPower0 = generatorPolyHasPower0;
            this.correctedData = new int[msg.Length - ecw];
            Array.Copy(msg, correctedData, correctedData.Length);
        }

        public bool correct() {
            int[] syndromes;
            if (!validateSyndromes(msg, ecw, generatorPolyHasPower0, out syndromes))
            {
                int[] pos = findErrors(syndromes, msg.Length);
                if (pos != null)
                {
                    correctErrors(msg, syndromes, pos);
                    return true;
                }
                return false;
            }
            Array.Copy(msg, correctedData, msg.Length - ecw);
            return true;
        }

        bool validateSyndromes(int[] msg, int ecw, bool generatorHasPower0, out int[] synd)
        {
            bool validate=true;
            int power0=generatorHasPower0?0:1;
            synd = new int[ecw];
            for (int i = 0; i < ecw; i++)
            {
                if ((synd[i] = gf.polyEval(msg, gf.exp[i + power0])) != 0) validate = false;
            }
            return validate;
        }

        //Berlekamp-Massey algorithm to find error positions
        //find error locator polynomial with Berlekamp-Massey algorithm
        int[] findErrors(int[] synd, int nmess)
        {
            int[] err_poly = new int[] { 1 };
            int[] old_poly = new int[] { 1 };

            for (int i = 0; i < synd.Length; i++)
            {
                old_poly = ArrayAdd(old_poly, 0);
                int delta = synd[i];
                for (int j = 1; j < err_poly.Length; j++)
                    delta ^= gf.mult(err_poly[err_poly.Length - 1 - j], synd[i - j]);
                if (delta != 0)
                {
                    if (old_poly.Length > err_poly.Length)
                    {
                        int[] new_poly = gf.polyScale(old_poly, delta);
                        old_poly = gf.polyScale(err_poly, gf.div(1, delta));
                        err_poly = new_poly;
                    }
                    err_poly = gf.polyAdd(err_poly, gf.polyScale(old_poly, delta));
                }

            }
            int errs = err_poly.Length-1;
            if (errs*2 > synd.Length) return null; //too many errors to correct

                
            //find zeros of error polynomial
            LinkedList<int> err_pos = new LinkedList<int>();
            for (int i=0;i<nmess;i++) 
                if (gf.polyEval(err_poly, gf.exp[gf.N-i]) == 0)
                    err_pos.AddLast(nmess-1-i);
            if (err_pos.Count != errs) return null; //couldn't find error locations
            int[] pos = new int[err_pos.Count];
            err_pos.CopyTo(pos,0);
            return pos;
        }

        void  correctErrors(int[] msg, int[] synd, int[] pos) {
            //calculate error locator polynomial
            int[] q = new int[]{1};
            for (int i=0;i<pos.Length;i++) {
                int x = gf.exp[msg.Length-1-pos[i]];
                q = gf.polyMult(q, new int[]{x,1});
            }

            //calculate error evaluator polynomial
            int[] p = new int[pos.Length*2+1];
            Array.Copy(synd, 0, p, 1, pos.Length*2);
            p[0] = 1; //S(x)=Sn*x^n + S_(n-1)*x^(n-1)+...S_1*x+1
            Array.Reverse(p);  // S(x) is the partial syndrome polynomial
            p = gf.polyMult(p, q); // S(x)A(x)
            int[] pp=new int[pos.Length*2];
            Array.Copy(p, p.Length - pos.Length*2, pp, 0, pos.Length*2); //S(x)A(x) mod(x^2t)
            
            //formal derivative of error locator eliminates even terms
            int[] qq=new int[q.Length/2];
            for (int i=q.Length%2, j=0; i<q.Length; i+=2, j++) qq[j]=q[i];
            
            //compute corrections
            Array.Copy(msg, correctedData, correctedData.Length);
            for (int i=0; i<pos.Length;i++) if (pos[i]<correctedData.Length) {
              int x = gf.exp[pos[i]+gf.N+1-msg.Length];
              int y = gf.polyEval(pp, x);
              int z = gf.polyEval(qq, gf.mult(x,x));
              int e = gf.div(y, gf.mult(x, z));
              correctedData[pos[i]] ^= e;
            }
        }

        int[] ArrayAdd(int[] a, int v)
        {
            int[] n = new int[a.Length + 1];
            for (int i = 0; i < a.Length; i++) n[i] = a[i];
            n[a.Length] = v;
            return n;
        }

        /*
        // generator poly (x-alfa^0)*(x-alfa^1)*(x-alfa^2)*...
        public int[] genearorPoly(int n)
        {
            int[] g=new int[n];
            for (int i=0;i<n;i++)
                g = gf.polyMult(g,new int[]{1,gf.exp[i]}); // (x-alfa^i)
            return g;
        }
        */
    }

    //Galois Field aritmethic for values and polynomials with Galois fiels coeficients
    internal class GF
    {
        //RS basis^exp (ex 2^8)
        int n, p;
        public int N; //base ^ exp  -1
        public int[] exp, log;
        public GF(int basis, int _exp, int p)
        {
            this.n=(int)Math.Pow(basis,_exp);
            this.N = n - 1;
            this.exp = new int[n * 2]; //so no need to check array overflow with multiplying
            this.log = new int[n];
            this.p=p;

            //init exp and log lookup tables
            int x = 1;
            exp[0] = exp[n-1]=1;
            log[1] = 0;
            for (int i=1; i<n;i++) {
               x <<= 1;
               if ((x & n)!=0) x ^= p; //if goes beyond gf_n, apply module
               exp[i+n-1]=exp[i] = x;                
               log[x] = i;
            }
        }

        public int mult(int a, int b)
        {
            if (a==0 || b==0) return 0;
            return exp[log[a] + log[b]];
        }

        public int div(int a, int b)
        {
            if (b==0) throw new DivideByZeroException();
            if (a==0) return 0;
            return exp[log[a] + N - log[b]];
        }

        public int[] polyScale(int[] p, int s)
        {
            int[] r = new int[p.Length];
            for (int i=0;i<p.Length;i++)
                r[i] = mult(p[i], s);
            return r;
        }

        //polynomials a and b are in reverse order: 
        //-poly[0] has the bigger exponent
        //-poly[n-1] is the exponent x^0
        public int[] polyAdd(int[] a, int[] b)
        {
            int max = a.Length > b.Length ? a.Length : b.Length;
            int[] r = new int[max];
            for (int i=0;i<a.Length;i++)
                r[i+r.Length-a.Length] = a[i];
            for (int i = 0; i < b.Length; i++)
                r[i + r.Length - b.Length] ^= b[i];
            return r;
        }

        public int[] polyMult(int[] a, int[] b)
        {
            int[] r=new int[a.Length+ b.Length -1]; //all 0's
            for (int j=0; j<b.Length; j++)
                for (int i=0;i<a.Length;i++)
                    r[i+j] ^= mult(a[i], b[j]);
            return r;
        }

        //optimized version to evaluate a poly
        //01 x4 + 0f x3 + 36 x2 + 78 x + 40 = (((01 x + 0f) x + 36) x + 78) x + 40
        public int polyEval(int[] p, int x)
        {
            int y = p[0];
            for (int i=1; i<p.Length;i++)
                y = mult(y,x) ^ p[i];
            return y;
        }

    }
}
