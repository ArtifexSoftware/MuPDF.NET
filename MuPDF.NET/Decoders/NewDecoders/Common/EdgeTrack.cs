using System;

namespace BarcodeReader.Core.Common
{
    //class used to track an edge. Uses "dual" linear regression, to adjust the 
    // regressed line to edges of both sides of the pattern.
    // At each step finds the 4 edge-pixels (left-up, left-down, right-up, right-down)
    // and adds the edge-pixel closest to the regression line. 
	internal class EdgeTrack
    {
        ImageScaner scan;
        float moduleSize;
        bool currentIsBlack;

        MyPoint A,B,C,D;
        MyPointF fA, fB;
        MyPoint lastA, lastB;
        MyVector vdX, vdY;
        float dA, dB;
        Regression reg;

        public EdgeTrack(ImageScaner scan)
        {
            this.scan = scan;
        }

        private bool DebugWriteConsoleMessages = false;
        public void setDebugWriteConsoleMessages(bool debug) { this.DebugWriteConsoleMessages=debug; }

        //p0 --> starting pixel 
        //vd --> direction: (-1,0) for vertical left side, (1,0) for vertical right side
        public Regression Track(MyPoint p0, MyVector vd, float moduleSize, bool currentIsBlack)
        {
            this.moduleSize = moduleSize;
            this.currentIsBlack = currentIsBlack;

            //main directions
            vdX = vd; //direction perpendicular to the edge (first approximation: usually 1,0 or 0,1 is enough)
            vdY = new MyVector(-vdX.Y, vdX.X); //edge direction

            //Initial aproximation
            A = scan.FindTransition(p0 + vdY, vdX, currentIsBlack, out fA);
            B = scan.FindTransition(p0 - vdY, vdX, currentIsBlack, out fB);
            lastA = A;
            lastB = B;

            reg = new Regression(p0);
            reg.AddPointL(fA);
            reg.AddPointL(fB);

            A = scan.FindTransition(A + vdY, vdX, currentIsBlack, out fA);
            B = scan.FindTransition(B - vdY, vdX, currentIsBlack, out fB);
            
            bool end = false;
            float maxDist = moduleSize;
            int maxSumErr = (int)(moduleSize * 2f);
            //moduleSize = 0;
            int errA=0, errB=0, sumErr=0;
            while (!end )
            {
                dA = reg.DistL(fA); if (dA < 0) dA = -dA; //absolute dist
                dB = reg.DistL(fB); if (dB < 0) dB = -dB;
                if (dA <= dB && dA  < maxDist && scan.InBorder(A))
                {
                    reg.AddPointL(fA);
                    if (dA < maxDist / 2f) { lastA = A; errA = 0; }
                    A = scan.FindTransition(A + vdY, vdX, currentIsBlack, out fA);
                }
                else if (dB <= dA && dB < maxDist && scan.InBorder(B))
                {
                    reg.AddPointL(fB);
                    if (dB < maxDist / 2f) { lastB = B; errB = 0; }
                    B = scan.FindTransition(B - vdY, vdX, currentIsBlack, out fB);
                }
                else if (errA < moduleSize && sumErr< maxSumErr)
                {
                    errA++; sumErr++;
                    A = scan.FindTransition(lastA + errA*vdY, vdX, currentIsBlack, out fA);
                }
                else if (errB < moduleSize && sumErr < maxSumErr)
                {
                    errB++; sumErr++;
                    B = scan.FindTransition(lastB - errB * vdY, vdX, currentIsBlack, out fB);
                }
                else end = true;
            }
            return reg;
        }

        public MyPointF Up() { return Up(1); } //default offset +1
        public MyPointF Up(int offset)
        {
            int n = 0;
            float max = moduleSize;
            int iMax = (int) Math.Round(max);
            if (iMax <2) iMax = 2;
            A = lastA;
            while (scan.InBorder(A) && n < iMax)
            {
                if (dA < max)
                {
                    lastA = A; n = 0;
                    A = scan.FindTransition(A + vdY, vdX, currentIsBlack, out fA);
                }
                else
                {
                    n++;
                    A = scan.FindTransition(lastA + (n+offset)*vdY, vdX, currentIsBlack, out fA);
                }
                dA = reg.DistL(fA); if (dA < 0) dA = -dA; //absolute dist
            }
            try { return reg.Project(lastA); } catch (Exception) { return MyPointF.Empty; }
        }

        public MyPointF Down() { return Down(1); } //default offset +1
        public MyPointF Down(int offset)
        {
            int n = 0;
            float max = moduleSize;
            int iMax = (int)Math.Round(max);
            if (iMax <2) iMax = 2;
            B = lastB;
            while (scan.InBorder(B) && n < iMax)
            {
                if (dB < max)
                {
                    lastB = B; n = 0;
                    B = scan.FindTransition(B - vdY, vdX, currentIsBlack, out fB);
                }
                else
                {
                    n++;
                    B = scan.FindTransition(lastB - (n+offset) * vdY, vdX, currentIsBlack, out fB);
                }
                dB = reg.DistL(fB); if (dB < 0) dB = -dB; //absolute dist
            }
            try { return reg.Project(lastB); } catch (Exception) { return MyPointF.Empty; }
        }

        public RegressionLine GetLine()
        {
            return reg.LineL;
        }

        float abs(float f)
        {
            if (f < 0) return -f; //absolute dist
            return f;
        }

        //p0, p1 --> starting pixel (dual edge track, used in QR and Aztec (square finders)
        //vd --> direction: (-1,0) for vertical left side, (1,0) for vertical right side
        public Regression Track(MyPoint p0, MyPoint p1, MyVector vd, float moduleSize, bool currentIsBlack)
        {
            //main directions
            MyVector vdX = vd; //direction of the edge (first approximation: usually 1,0 or 0,1 is enough)
            MyVector vdY = new MyVector(-vdX.Y, vdX.X);

            //Initial aproximation
            MyPointF fA, fB, fC, fD;
            //p0 = scan.FindTransition(p0, vdX, currentIsBlack, out fA);
            A = scan.FindTransition(p0 + vdY, vdX, currentIsBlack, out fA);
            B = scan.FindTransition(p0 - vdY, vdX, currentIsBlack, out fB);
            C = scan.FindTransition(p1 + vdY, vdX, !currentIsBlack, out fC);
            D = scan.FindTransition(p1 - vdY, vdX, !currentIsBlack, out fD);
            Regression reg = new Regression(p0, C-A);
            //if (DebugWriteConsoleMessages) reg.setDebug(true);
            reg.AddPointL(fA);
            reg.AddPointL(fB);
            reg.AddPointR(fC);
            reg.AddPointR(fD);

            A = scan.FindTransition(A + vdY, vdX, currentIsBlack, out fA);
            B = scan.FindTransition(B - vdY, vdX, currentIsBlack, out fB);
            C = scan.FindTransition(C + vdY, vdX, !currentIsBlack, out fC);
            D = scan.FindTransition(D - vdY, vdX, !currentIsBlack, out fD);

            bool end = false;
            float maxDist = moduleSize * 2F;
            float dA, dB, dC, dD;
            update(reg, fA, fB, fC, fD, out dA, out dB, out dC, out dD);
            while (!end)
            {               
                if (dA <= dB && dA <= dC && dA <= dD && dA - 1F < maxDist / (float)reg.NL)
                {
                    reg.AddPointL(fA);
                    A = scan.FindTransition(A + vdY, vdX, currentIsBlack, out fA);
                    update(reg, fA, fB, fC, fD, out dA, out dB, out dC, out dD);
                }
                else if (dB <= dA && dB <= dC && dB <= dD && dB -1F< maxDist / (float)reg.NL)
                {
                    reg.AddPointL(fB);
                    B = scan.FindTransition(B - vdY, vdX, currentIsBlack, out fB);
                    update(reg, fA, fB, fC, fD, out dA, out dB, out dC, out dD);
                }
                else if (dC <= dA && dC <= dB && dC <= dD && dC - 1F < maxDist / (float)reg.NR)
                {
                    reg.AddPointR(fC);
                    C = scan.FindTransition(C + vdY, vdX, !currentIsBlack, out fC);
                    update(reg, fA, fB, fC, fD, out dA, out dB, out dC, out dD);
                }
                else if (dD <= dA && dD <= dB && dD <= dC && dD - 1F < maxDist / (float)reg.NR)
                {
                    reg.AddPointR(fD);
                    D = scan.FindTransition(D - vdY, vdX, !currentIsBlack, out fD);
                    update(reg, fA, fB, fC, fD, out dA, out dB, out dC, out dD);
                }
                else end = true;
            }
            return reg;
        }

        void update(Regression reg, MyPointF fA, MyPointF fB, MyPointF fC, MyPointF fD, out float dA, out float dB, out float dC, out float dD)
        {
            dA = abs(reg.DistL(fA));
            dB = abs(reg.DistL(fB));
            dC = abs(reg.DistR(fC));
            dD = abs(reg.DistR(fD));
        }

        //p0 --> starting pixel 
        //vd --> direction: (-1,0) or (0,-1) 
        public void TrackBar(MyPoint p0, MyVector vd, float moduleSize, bool currentIsBlack, out MyPointF lu, out MyPointF ld, out MyPointF ru, out MyPointF rd)
        {
            this.moduleSize = moduleSize;
            this.currentIsBlack = currentIsBlack;

            //main directions
            vdX = vd; //direction perpendicular to the edge (first approximation: usually 1,0 or 0,1 is enough)
            vdY = new MyVector(-vdX.Y, vdX.X); //edge direction

            float ud, dd;
            MyPoint UM,DM, lastUM, lastDM;
            MyPoint UL, UR, DL, DR;
            scan.FindModule(p0+vdY, vdX, currentIsBlack, out UL, out UR, out UM, out ud);
            scan.FindModule(p0-vdY, vdX, currentIsBlack, out DL, out DR, out DM, out dd);

            Regression reg = new Regression(p0);
            reg.AddPointL(UM);
            reg.AddPointL(DM);
            lastUM = UM;
            lastDM = DM;

            bool Uend = false, Dend = false;
            int Uhole, Dhole,hole;
            Uhole = Dhole = hole=Convert.ToInt32(ud);

            while (!Uend || !Dend)
            {
                if (!Uend)
                {
                    UM = reg.project(UM + vdY, vdX);
                    scan.FindModule(UM, vdX, currentIsBlack, out UL, out UR, out UM, out ud);
                    if (ud == 0f) { if (--Uhole <= 0) Uend = true; }
                    else if (Calc.Around(ud/hole,1f,0.1f))
                    {
                        Uhole = hole;
                        reg.AddPointL(UM);
                        lastUM = UM;
                    }
                }

                if (!Dend)
                {
                    DM = reg.project(DM - vdY, vdX);
                    scan.FindModule(DM, vdX, currentIsBlack, out DL, out DR, out DM, out dd);
                    if (dd == 0f) { if (--Dhole <= 0) Dend = true; }
                    else if (Calc.Around(dd/hole,1f,0.1f))
                    {
                        Dhole = 0;
                        reg.AddPointL(DM);
                        lastDM = DM;
                    }
                }
            }

            MyPointF um = reg.Project(lastUM);
            MyPointF dm = reg.Project(lastDM);

            MyVectorF x = reg.VdY.Normalized;
            //correct x to go in the same direction of vdX
            if (vdX.X != 0) { if (vdX.X<0 && x.X > 0 || vdX.X>0 && x.X < 0 ) x = x * -1f; }
            else { if (vdX.Y < 0 && x.Y > 0 || vdX.Y > 0 && x.Y < 0) x = x * -1f; }
            lu = um + x * (hole / 2f);
            ld = dm + x * (hole / 2f);
            ru = um - x * (hole / 2f);
            rd = dm - x * (hole / 2f);
        }
    }
}
