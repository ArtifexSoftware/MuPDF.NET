using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestThreads/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestThreads
    {
        private const string TestClassName = nameof(TestThreads);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static void Log(string text) =>
            Console.WriteLine($"{nameof(TestThreads)}.cs:  ManagedThreadId={Thread.CurrentThread.ManagedThreadId} {text}");

        private static void ThreadFn(Queue queueToThreads, Queue queueFromThreads)
        {
            void Tlog(string text) => Log($"### threadfn(): {text}");
            try
            {
                var documents = new List<Document>();
                while (true)
                {
                    object action = DequeueBlocking(queueToThreads);
                    if (action is string s && s == "quit")
                        break;
                    if (action is ValueTuple<string, string> openAction && openAction.Item1 == "open")
                    {
                        string path = openAction.Item2;
                        var document = new Document(path);
                        documents.Add(document);
                    }
                    else if (action is string gettext && gettext == "gettext")
                    {
                        if (documents.Count > 0)
                        {
                            int documentI = Random.Shared.Next(documents.Count);
                            var document = documents[documentI];
                            var page = document[Random.Shared.Next(document.PageCount)];
                            _ = page.GetText();
                        }
                    }
                    else if (action is string close && close == "close")
                    {
                        if (documents.Count >= 2)
                        {
                            int documentI = Random.Shared.Next(documents.Count);
                            documents[documentI].Dispose();
                            documents.RemoveAt(documentI);
                        }
                    }
                    else
                        Assert.Fail($"Unrecognised action={action}.");
                }

                queueFromThreads.Enqueue(Thread.CurrentThread);
            }
            catch (Exception e)
            {
                Tlog($"error: {e}");
                queueFromThreads.Enqueue(e);
            }
        }

        [Fact]
        public void test_threads_stress()
        {
            Console.WriteLine();
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PYODIDE_ROOT")))
            {
                Console.WriteLine("test_threads_stress(): not running on Pyodide - cannot create threads.");
                return;
            }

            string[] paths =
            {
                Doc("test_3594.pdf"),
                Doc("test_3789.pdf"),
            };

            var threads = new List<Thread>();
            var queueToThreads = Queue.Synchronized(new Queue());
            var queueFromThreads = Queue.Synchronized(new Queue());

            void Put(object action) => queueToThreads.Enqueue(action);

            var stats = new ThreadStressStats();

            void StartThread()
            {
                var thread = new Thread(() => ThreadFn(queueToThreads, queueFromThreads))
                {
                    IsBackground = true,
                };
                threads.Add(thread);
                thread.Start();
                stats.NumThreadsMax = Math.Max(stats.NumThreadsMax, threads.Count);
            }

            void QuitThread()
            {
                Put("quit");
                object stoppedThread = DequeueBlocking(queueFromThreads);
                if (stoppedThread is Exception ex)
                    throw new Xunit.Sdk.XunitException($"A thread has failed: {ex}");
                Assert.IsType<Thread>(stoppedThread);
                var thread = (Thread)stoppedThread;
                thread.Join();
                threads.Remove(thread);
            }

            void OpenDocument()
            {
                string path = paths[Random.Shared.Next(paths.Length)];
                Put(("open", path));
                stats.NumOpens++;
            }

            for (int i = 0; i < 10; i++)
            {
                StartThread();
                OpenDocument();
            }

            const int numits = 1000;
            for (int i = 0; i < numits; i++)
            {
                int op = Random.Shared.Next(100);
                if (op < 10)
                    StartThread();
                else if (op < 15)
                {
                    if (threads.Count >= 2)
                        QuitThread();
                }
                else if (op < 30)
                    OpenDocument();
                else if (op == 40)
                {
                    if (threads.Count > 0)
                        Put("close");
                }
                else if (op < 100)
                {
                    Put("gettext");
                    stats.NumGettexts++;
                }
                else
                    Assert.Fail($"Unrecognised op={op}.");
            }

            Log($"End: threads.Count={threads.Count} stats.NumOpens={stats.NumOpens} stats.NumGettexts={stats.NumGettexts} stats.NumThreadsMax={stats.NumThreadsMax}.");

            for (int i = 0; i < threads.Count; i++)
                QuitThread();

            // Ignore any warnings, which can occur for some pages in the documents.
            _ = Tools.MupdfWarnings();
        }

        private static object DequeueBlocking(Queue queue)
        {
            // Python queue.Queue.get() blocks until an item is available.
            while (true)
            {
                lock (queue.SyncRoot)
                {
                    if (queue.Count > 0)
                        return queue.Dequeue()!;
                }
                Thread.Sleep(1);
            }
        }

        private sealed class ThreadStressStats
        {
            public int NumOpens { get; set; }
            public int NumGettexts { get; set; }
            public int NumThreadsMax { get; set; }
        }
    }
}