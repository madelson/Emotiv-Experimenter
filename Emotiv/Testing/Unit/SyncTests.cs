using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MCAEmotiv.Testing.Unit
{
    class SyncTests
    {
        public static void Run()
        {
            TestBlockingQueue();
            TestInvoker();
        }

        public static void TestBlockingQueue()
        {
            var bq = new BlockingQueue<int>();
            var range = Arrays.Range(0, 1, 5);
            var list = new List<int>();
            for (int i = 0; i < 2 * range.Count; i++)
                ThreadPool.QueueUserWorkItem((o) =>
                {
                    int val;
                    if (!bq.TryDequeue(500, out val))
                        val = -1;

                    lock (list)
                        list.Add(val);
                });
            Thread.Sleep(10);
            foreach (var i in range)
                bq.Enqueue(i);

            var now = DateTime.Now;
            while ((DateTime.Now - now).TotalSeconds < 5)
            {
                lock (list)
                    if (list.Count >= 2 * range.Count)
                    {
                        if (list.Count > 2 * range.Count)
                            throw new Exception("Too many items");
                        if (list.Count(i => i == -1) != range.Count)
                            throw new Exception("Wrong number of -1's");
                        if (!list.Where(i => i != -1).InOrder().SequenceEqual(range))
                            throw new Exception("Wrong non-negative elements!");
                        return; // success
                    }

                Thread.Sleep(10);
            }

            throw new Exception("Failed to complete after 5 seconds!");
        }

        public static void TestInvoker()
        {
            var range = Arrays.Range(0, 1, 100);
            var list = new List<int>();
            IAsyncResult result = null;

            using (var sti = new SingleThreadedInvoker())
            {
                foreach (int i in range)
                {
                    int j = i;
                    if (j % 2 == 0)
                        result = sti.BeginInvoke(new Action(() =>
                        {
                            list.Add(j);
                        }), new object[0]);
                    else
                        sti.Invoke(new Action(() =>
                        {
                            list.Add(j);
                        }), new object[0]);
                }

                // wait for all to finish
                sti.EndInvoke(result);
            }

            if (!list.SequenceEqual(range))
                throw new Exception("SingleThreadedInvoker failed!");
        }
    }
}
