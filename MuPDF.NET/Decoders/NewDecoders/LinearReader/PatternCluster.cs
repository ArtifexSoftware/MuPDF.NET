using System.Collections.Generic;

namespace BarcodeReader.Core.Common
{
    /// <summary>
    /// Contains patterns joined vertically.
    /// Analog of StackedPattern. 
    /// Used for LinearReader.
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    class PatternCluster : LinkedList<Pattern>
    {
        public MyPoint A;
        public MyPoint B;
        public bool IsStopPattern;
        public PatternCluster OppositeCluster;
        public float SumModule = 0f;
        public float AvgModule { get { return SumModule > float.Epsilon ? SumModule / Count : 0; }}

        public MyVectorF Normal;//perpendicular normalized vector to the cluster cloud
        public MyVectorF Dir;//vector along cluster point cloud
        public int RobustIndexA;
        public int RobustIndexB;
        public MyPoint RobustA;
        public MyPoint RobustB;
        public float Length;

        private List<Pattern> _list;

        public Pattern this[int index]
        {
            get { return _list[index]; }
        }

        public MyPoint GetPoint(int index)
        {
            var p = _list[index];
            return new MyPoint(IsStopPattern ? p.xEnd : p.xIn, p.y);
        }

        public MyPoint GetMiddlePoint()
        {
            var p = _list[Count / 2];
            return new MyPoint(IsStopPattern ? p.xEnd : p.xIn, p.y);
        }

        public void CalcParams(bool isStopPattern)
        {
            this.IsStopPattern = isStopPattern;
            A = new MyPoint(isStopPattern ? First.Value.xEnd : First.Value.xIn, First.Value.y);
            B = new MyPoint(isStopPattern ? Last.Value.xEnd : Last.Value.xIn, Last.Value.y);

            _list = new List<Pattern>(this);

            //calc robust ends
            if (Count < 8)
            {
                RobustIndexA = 0;
                RobustIndexB = Count - 1;
                RobustA = A;
                RobustB = B;
            }
            else
            {
                //cut points 30% - 70%
                RobustIndexA = (int)(Count * 0.3f);
                RobustIndexB = (int)(Count * 0.7f);
                RobustA = GetPoint(RobustIndexA);
                RobustB = GetPoint(RobustIndexB);
            }

            //Calc normal to the border
            Normal = new MyVectorF(RobustB.Y - RobustA.Y, RobustA.X - RobustB.X).Normalized;

            //calc direction and length
            Dir = new MyVectorF(RobustB.X - RobustA.X, RobustB.Y - RobustA.Y).Normalized;
            Length = (A - B).Length;
        }
    }
}