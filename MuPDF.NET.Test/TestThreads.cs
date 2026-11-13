using System;
using System.Collections.Concurrent;
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
            Console.WriteLine($"TestThreads.cs:  get_native_id={Thread.CurrentThread.ManagedThreadId} {text}");


        private static void ThreadFn(
            BlockingCollection<object> queueToThreads,
            BlockingCollection<object> queueFromThreads)
        {


            void Tlog(string text) => Log($"### threadfn(): {text}");
            try
            {
                var documents = new List<Document>();
                // while 1:
                while (true)
                {
                    // Python queue.Queue.get() blocks until an item is available.
                    object action = queueToThreads.Take();
                    // if action == 'quit':
                    if (action is string quit && quit == "quit")
                        break;
                    else if (action is ValueTuple<string, string> openAction && openAction.Item1 == "open")
                    {
                        string path = openAction.Item2;
                        var document = new Document(path);
                        documents.Add(document);
                    }
                    else if (action is string gettext && gettext == "gettext")
                    {
                        // if documents:
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
                            documents[documentI].Close();
                            documents.RemoveAt(documentI);
                        }
                    }
                    else
                    {
                        Assert.Fail($"Unrecognised action={action}.");
                    }
                }

                foreach (var document in documents)
                    document.Close();

                queueFromThreads.Add(Thread.CurrentThread);
            }
            catch (Exception e)
            {
                Tlog($"error: {e}");
                queueFromThreads.Add(e);
            }
        }

        [Fact]
        public void test_threads_stress()
        {
            // print
            Console.WriteLine();

            string[] paths =
            {
                Doc("test_3594.pdf"),
                Doc("test_3789.pdf"),
            };

            var threads = new List<Thread>();

            using var queueToThreads = new BlockingCollection<object>();
            using var queueFromThreads = new BlockingCollection<object>();


            void Put(object action) => queueToThreads.Add(action);

            // class Stats: pass
            var stats = new Stats();


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
                object stoppedThread = queueFromThreads.Take();
                Assert.True(
                    stoppedThread is Thread,
                    $"A thread has failed: {stoppedThread}");
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
                // start_thread
                StartThread();
                // open_document
                OpenDocument();
            }

            const int numits = 1000;
            for (int i = 0; i < numits; i++)
            {
                int op = Random.Shared.Next(100);
                // if op <10:
                if (op < 10)
                {
                    // start_thread
                    StartThread();
                }
                else if (op < 15)
                {
                    if (threads.Count >= 2)
                        // quit_thread
                        QuitThread();
                }
                else if (op < 30)
                {
                    // open_document
                    OpenDocument();
                }
                else if (op == 40)
                {
                    // if threads:
                    if (threads.Count > 0)
                        Put("close");
                }
                else if (op < 100)
                {
                    Put("gettext");
                    stats.NumGettexts++;
                }
                else
                {
                    Assert.Fail($"Unrecognised op={op}");
                }
            }


            Log("End:");

            Log($"len(threads)={threads.Count} num_opens={stats.NumOpens} num_gettexts={stats.NumGettexts} num_threads_max={stats.NumThreadsMax}.");

            int remaining = threads.Count;
            for (int i = 0; i < remaining; i++)
                // quit_thread
                QuitThread();

            // Ignore any warnings, which can occur for some pages in the documents.

            _ = Tools.MupdfWarnings();
        }

        private sealed class Stats
        {
            public int NumOpens;
            public int NumGettexts;
            public int NumThreadsMax;
        }
    }
}
