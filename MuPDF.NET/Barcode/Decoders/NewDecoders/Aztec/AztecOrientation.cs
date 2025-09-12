using System;
using System.Collections;

namespace BarcodeReader.Core.Aztec
{
    // class designed to account for the orientation patterns used to orient the aztec code
    class AztecOrientation
    {
        public enum Pattern { TopLeft = 0, TopRight = 1, BottomLeft = 3, BottomRight = 2 }

        private static readonly Hashtable IndexMap = new Hashtable();

        static AztecOrientation()
        {
            IndexMap.Add(7, 0);
            IndexMap.Add(6, 1);
            IndexMap.Add(1, 2);
            IndexMap.Add(0, 3);
        }

        private enum Mirrored { DontKnowYet, Yes, No }

        private readonly MyPoint[] points = new MyPoint[4];

        private Mirrored mirrored = Mirrored.DontKnowYet;

        // tells if the current orientation is valid
        public bool IsValid
        {
            get
            {
                if (GetValidCount() < 3)
                {
                    return false;
                }

                MyVector scanVector = GetModeScanVector();
                if (Math.Abs(scanVector.X + scanVector.Y) != 1)
                {
                    return false;
                }

                MyVector s1 = points[1] - points[0];
                s1 = (MyVector)(s1 / s1.Length);
                MyVector s2 = points[2] - points[1];
                s2 = (MyVector)(s2 / s2.Length);
                if (RotateClockwise(s1) != s2)
                {
                    return false;
                }

                return true;
            }
        }

        public MyPoint StartPoint
        {
            get
            {
                return points[0];
            }
        }

        public AztecOrientation()
        {
            for (int i = 0; i < 4; i++)
            {
                points[i] = MyPoint.Empty;
            }
        }

        public void Rotate(AztecFinder finder)
        {
            for (int i = 0; i < 4; i++) 
                if (points[i].X == 0 && points[i].Y == 0) break;
                else finder.Rotate();
        }

        // adds a pattern, by classifying it on the palette of possible patterns
        public bool AddPattern(bool[] patternBits, MyPoint center)
        {
            int index = Classify(patternBits, ref mirrored);
            if (!IsIndexValid(index))
            {
                if (GetValidCount() == 3)
                {
                    for (int i = 0; i < 4; ++i)
                    {
                        if (points[i].IsEmpty)
                        {
                            points[i] = center;
                            return true;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }

            points[(int)IndexMap[index]] = center;
            return true;
        }

        // gets the start vector for scanning the mode bits
        public MyVector GetModeScanVector()
        {
            MyVector vector = points[1] - points[0];
            return (MyVector)(vector / vector.Length);
        }

        // rotates the scan vector clockwise, relative to the actual orientation
        public MyVector RotateClockwise(MyVector vector)
        {
            return mirrored == Mirrored.Yes
                       ? AztecUtils.RotateCounterClockwise(vector)
                       : AztecUtils.RotateClockwise(vector);
        }

        private int GetValidCount()
        {
            int i = 0;
            foreach (MyPoint point in points)
            {
                if (!point.IsEmpty)
                {
                    ++i;
                }
            }

            return i;
        }

        private static bool IsIndexValid(int index)
        {
            return IndexMap.ContainsKey(index);
        }

        // classifies a set of bits as a locator pattern (also takes care of mirroring)
        private static int Classify(bool[] patternBits, ref Mirrored mirrored)
        {
            if (patternBits.Length != 3)
            {
                throw new Exception("Invalid pattern length");
            }

            int index = 0;
            switch (mirrored)
            {
                case Mirrored.Yes:
                    for (int i = 0; i < 3; ++i)
                    {
                        if (patternBits[i])
                        {
                            index |= (1 << (2 - i));
                        }
                    }
                    break;
                case Mirrored.No:
                    for (int i = 0; i < 3; ++i)
                    {
                        if (patternBits[i])
                        {
                            index |= (1 << i);
                        }
                    }
                    break;
                default:
                    mirrored = Mirrored.No;
                    int nmIndex = Classify(patternBits, ref mirrored);
                    mirrored = Mirrored.Yes;
                    int mIndex = Classify(patternBits, ref mirrored);
                    if (nmIndex == mIndex)
                    {
                        index = nmIndex;
                        mirrored = Mirrored.DontKnowYet;
                        break;
                    }

                    if (!IsIndexValid(mIndex))
                    {
                        index = nmIndex;
                        mirrored = Mirrored.No;
                        break;
                    }

                    index = mIndex;
                    break;
            }

            return index;
        }
    }
}
