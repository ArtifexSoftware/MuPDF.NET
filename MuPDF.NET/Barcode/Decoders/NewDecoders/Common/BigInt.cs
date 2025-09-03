using System;
using System.Collections;

namespace BarcodeReader.Core.Common
{
	internal class BigInt
    {
        bool[] bits;

        public BigInt(int N, int n)
        {
            this.bits = new bool[N];
            int i = 0;
            while (n > 0)
            {
                this.bits[i++]=(n % 2) == 1;
                n /= 2;
            }
        }

        public BigInt(int N, Decimal n)
        {
            this.bits = new bool[N];
            int i = 0;
            while (n > 0)
            {
                this.bits[i++] = (n % 2) == 1;
                n = Decimal.Floor(n/2);
            }
        }

        public BigInt(int n)
        {
            ArrayList b = new ArrayList();
            while (n > 0)
            {
                b.Add((n % 2)==1);
                n /= 2;
            }
            this.bits = new bool[b.Count];
            for (int i = 0; i < b.Count; i++) this.bits[i] = (bool)b[i];
        }

        public BigInt(Decimal n)
        {
            ArrayList b = new ArrayList();
            while (n > 0)
            {
                b.Add((n % 2) == 1);
                n = Decimal.Floor(n/2);
            }
            this.bits = new bool[b.Count];
            for (int i = 0; i < b.Count; i++) this.bits[i] = (bool)b[i];
        }

        public BigInt(BigInt n)
        {
            this.bits = new bool[n.bits.Length];
            Array.Copy(n.bits, this.bits, this.bits.Length);
        }

        public int ArrayLength { get { return this.bits.Length; } }

        public int Length { get { for (int i = this.bits.Length - 1; i >= 0; i--) if (this.bits[i]) return i+1; return 0; } }

        private bool addBit(int nBit, bool n, bool carry)
        {
            int c = (this.bits[nBit] ? 1 : 0) + (n ? 1 : 0) + (carry?1:0);
            switch (c)
            {
                case 0: this.bits[nBit] = false; carry = false; break;
                case 1: this.bits[nBit] = true; carry = false; break;
                case 2: this.bits[nBit] = false; carry = true; break;
                case 3: this.bits[nBit] = true; carry = true; break;
            }
            return carry;
        }

        private bool subsBit(int nBit, bool n, bool carry)
        {
            int c = (this.bits[nBit] ? 1 : 0) - (n ? 1 : 0) - (carry ? 1 : 0);
            switch (c)
            {
                case -2: this.bits[nBit] = false; carry = true; break;
                case -1: this.bits[nBit] = true; carry = true; break;
                case 0: this.bits[nBit] = false; carry = false; break;
                case 1: this.bits[nBit] = true; carry = false; break;
            }
            return carry;
        }

        public static BigInt operator +(BigInt a, BigInt n)
        {
            BigInt r = new BigInt(a);
            bool carry = false;
            int i = 0;
            while (i < n.ArrayLength) carry=r.addBit(i, n.bits[i++], carry);
            while (carry && i < r.ArrayLength) carry = r.addBit(i++, false, carry);
                
            return r;
        }

        public static BigInt operator -(BigInt a, BigInt n)
        {
            BigInt r = new BigInt(a);
            bool carry = false;
            int i = 0;
            while (i < n.ArrayLength) carry = r.subsBit(i, n.bits[i++], carry);
            while (carry && i < r.ArrayLength) carry = r.subsBit(i++, false, carry);
            return r;
        }

        public BigInt ShiftLeft(int n)
        {
            BigInt r = new BigInt(this.ArrayLength, 0);
            for (int i = 0; i + n < this.ArrayLength; i++) r.bits[i + n] = this.bits[i];
            return r;
        }

        public static BigInt operator *(BigInt a, BigInt n)
        {
            BigInt r = new BigInt(a.bits.Length,0);
            for (int i = 0; i < n.Length; i++) if (n.bits[i]) r = r + a.ShiftLeft(i);
            return r;
        }

        public void Divide(BigInt n, out BigInt q, out BigInt r)
        {
            int thisN = this.Length;
            int lengthN = n.Length;
            Decimal iN = n.ToDecimal();
            BigInt current=new BigInt(lengthN+1,0);
            if (lengthN < thisN) for (int i = 0; i < lengthN; i++) current.bits[i] = this.bits[thisN - lengthN + i];
            else for (int i = 0; i < thisN; i++) current.bits[i] = this.bits[i];
            q = new BigInt(this.ArrayLength, 0);
            int pos = thisN - lengthN;
            do {
                Decimal iCurrent = current.ToDecimal();
                Decimal iQ = Decimal.Floor(iCurrent / iN);
                Decimal iR = iCurrent % iN;

                //update quotient
                q=q.ShiftLeft(1); q.bits[0] = (iQ == 1);

                //update remainder
                r = new BigInt(lengthN + 1, iR);
                if (pos--> 0) { current = r.ShiftLeft(1); current.bits[0] = this.bits[pos]; }
            } while (pos>=0);            
        }


        public override string ToString()
        {
            string s = "";
            for (int i = 0; i < this.bits.Length; i++) s = (this.bits[i] ? "1" : "0") + s;
            return s;
        }

        public int ToInt()
        {
            int r = 0, pow = 1;
            for (int i = 0; i < this.bits.Length; i++, pow*=2) r += (this.bits[i] ? pow : 0);
            return r;
        }

        public Decimal ToDecimal()
        {
            Decimal r = 0, pow = 1;
            for (int i = 0; i < this.Length; i++, pow *= 2) r += (this.bits[i] ? pow : 0);
            return r;
        }

        public int GetByte(int n)
        {
            int i0=8*n; 
            int b=0;
            for (int i = 0; i < 8; i++)
                b+= this.bits[i0 + i] ? 1 << i : 0;
            return b;
        }
    }
}
