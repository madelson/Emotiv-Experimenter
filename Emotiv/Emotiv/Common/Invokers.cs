using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading;

namespace System.Threading
{
    /// <summary>
    /// An invoker which runs invoked delegates on a message queue processed by a single thread.
    /// </summary>
    public class SingleThreadedInvoker : SafeDisposable, ISynchronizeInvoke
    {
        private class AsyncResult : IAsyncResult
        {
            public ManualResetEvent OnComplete { get; set; }
            public bool endInvokedCalled = false;
            public volatile bool completed = false;

            public object AsyncState { get { return null; } }

            public WaitHandle AsyncWaitHandle { get { throw new NotSupportedException("Use EndInvoke instead"); } }

            public bool CompletedSynchronously { get; set; }

            public bool IsCompleted { get { return this.completed; } }
        }

        private const int MAX_QUEUE_SIZE = 10, BLOCK_TIME_MILLIS = 100;

        private readonly Thread thread;
        private volatile bool shouldStop = false;
        private readonly BlockingQueue<Trio<Delegate, object[], AsyncResult>> queue = new BlockingQueue<Trio<Delegate, object[], AsyncResult>>();
        private readonly Queue<ManualResetEvent> cache = new Queue<ManualResetEvent>(MAX_QUEUE_SIZE);

        /// <summary>
        /// Construct an invoker with its own background thread
        /// </summary>
        public SingleThreadedInvoker()
        {
            this.thread = new Thread(this.EventLoop) { IsBackground = true, Name = typeof(SingleThreadedInvoker) + " Thread" };
            this.thread.Start();
        }

        private void EventLoop()
        {
            Trio<Delegate, object[], AsyncResult> eventDelegate;

            while (true)
            {
                if (this.shouldStop)
                    break;

                if (this.queue.TryDequeue(BLOCK_TIME_MILLIS, out eventDelegate))
                {
                    // invoke the delegate on this thread                    
                    try { eventDelegate.Item1.DynamicInvoke(eventDelegate.Item2); }
                    catch (Exception) { }

                    // fire the completed event
                    eventDelegate.Item3.OnComplete.Set();
                }
            }

            // cleanup
            foreach (var mre in this.cache)
                mre.Dispose();
        }

        private ManualResetEvent GetMRE()
        {
            lock (this.cache)
                return (this.cache.Count == 0)
                    ? new ManualResetEvent(false)
                    : this.cache.Dequeue();
        }

        private void DisposeOfMRE(ManualResetEvent mre)
        {
            lock (this.cache)
            {
                if (this.cache.Count < MAX_QUEUE_SIZE)
                {
                    mre.Reset();
                    this.cache.Enqueue(mre);
                }
                else
                    mre.Dispose();
            }
        }

        /// <summary>
        /// Queue the delegate to be invoked on this invoker's thread
        /// </summary>
        public IAsyncResult BeginInvoke(Delegate method, object[] args)
        {
            var result = new AsyncResult() { OnComplete = this.GetMRE(), CompletedSynchronously = !this.InvokeRequired };
            this.queue.Enqueue(Tuples.New(method, args, result));
            
            return result;
        }

        /// <summary>
        /// Returns when the asynchronous invokation represented by result completes
        /// </summary>
        public object EndInvoke(IAsyncResult result)
        {
            var asyncResult = result as AsyncResult;
            if (asyncResult == null)
                throw new Exception("Result was not created by this ISynchronizedInvoke!");

            lock (asyncResult)
            {
                if (asyncResult.endInvokedCalled)
                    throw new Exception("EndInvoke called twice on result!");
                asyncResult.endInvokedCalled = true;
            }

            asyncResult.OnComplete.WaitOne();

            return asyncResult.AsyncState;
        }

        /// <summary>
        /// Invokes the delegate on this invoker's thread and returns upon completion
        /// </summary>
        public object Invoke(Delegate method, object[] args)
        {
            // we can avoid locking here since the result never leaves this method
            var result = (AsyncResult)this.BeginInvoke(method, args);
            result.OnComplete.WaitOne();

            return result.AsyncState;
        }

        /// <summary>
        /// Is the current thread different from this invoker's thread?
        /// </summary>
        public bool InvokeRequired
        {
            get { return Thread.CurrentThread == this.thread; }
        }

        /// <summary>
        /// Stops the message loop
        /// </summary>
        protected override void DisposeOfManagedResources()
        {
            this.shouldStop = true;
        }
    }

    /// <summary>
    /// Extension methods and utilities for ISynchronizedInvokes
    /// </summary>
    public static class Invokers
    {
        /// <summary>
        /// Invoke an action
        /// </summary>
        public static void Invoke(this ISynchronizeInvoke invoker, Action action)
        {
            invoker.Invoke(action, Utils.EmptyArgs);
        }

        /// <summary>
        /// Begin invoking an action
        /// </summary>
        public static IAsyncResult BeginInvoke(this ISynchronizeInvoke invoker, Action action)
        {
            return invoker.BeginInvoke(action, Utils.EmptyArgs);
        }

        /// <summary>
        /// As invoke, but always called on the correct thread
        /// </summary>
        public static void InvokeSafe(this ISynchronizeInvoke invoker, Action action)
        {
            if (invoker.InvokeRequired)
                invoker.Invoke(action);
            else
                action();
        }
    }
}
