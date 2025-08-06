using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BarcodeReader.Core.Common
{
#if !DISABLE_PARALLEL
    internal static class Parallel
    {
        internal static bool DisableParallelProcessing = false;

        static int coreCount;

        static Parallel()
        {
            coreCount = 4;
        }

        public static void For(int fromInclusive, int toExclusive, Action<int> act)
        {
            if (DisableParallelProcessing)
            {
                for (int i = fromInclusive; i < toExclusive; i++)
                    act(i);
            }
            else
            {
                For(fromInclusive, toExclusive, -1, 1, (pair) =>
                {
                    var from = pair.From;
                    var to = pair.To;
                    for (int i = from; i < to; i++)
                        act(i);
                });
            }
        }

        public static void For(int fromInclusive, int toExclusive, int maxThreadsCount, int minPartitionSize, Action<Pair> act)
        {
            if (DisableParallelProcessing)
            {
                act(new Pair { From = fromInclusive, To = toExclusive });
            }
            else
            {
                var threadCount = maxThreadsCount < 1 ? coreCount : maxThreadsCount;
                var partSize = Math.Max(minPartitionSize, 1 + (toExclusive - fromInclusive) / threadCount);

                var waits = new AutoResetEvent[threadCount];

                for (int iThread = 0; iThread < threadCount; iThread++)
                {
                    var iT = iThread;
                    var from = fromInclusive + iThread * partSize;
                    var to = Math.Min(toExclusive, fromInclusive + (iThread + 1) * partSize);

                    waits[iT] = new AutoResetEvent(false);

                    if (from >= to)
                    {
                        waits[iT].Set();
                        continue;
                    }

                    new Thread(
                            (o) =>
                            {
                                var pair = new Pair { From = from, To = to };
                                act(pair);
                                waits[iT].Set();
                            }
                        )
                        { IsBackground = true }.Start();
                }

                //wait all threads
                for (int iThread = 0; iThread < threadCount; iThread++)
                    waits[iThread].WaitOne();
            }
        }

        public struct Pair
        {
            public int From;
            public int To;

            public override string ToString()
            {
                return From + " - " + To;
            }
        }
    }
#endif

#if DISABLE_PARALLEL
    internal static class Parallel
    {
        public static void For(int fromInclusive, int toExclusive, Action<int> act)
        {
            for (int i = fromInclusive; i < toExclusive; i++)
                act(i);
        }

        public static void For(int fromInclusive, int toExclusive, int maxThreadsCount, int minPartitionSize, Action<Pair> act)
        {
            act(new Pair { From = fromInclusive, To = toExclusive });
        }

        public struct Pair
        {
            public int From;
            public int To;

            public override string ToString()
            {
                return From + " - " + To;
            }
        }
    }

#endif

}
