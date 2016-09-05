﻿/*
Copyright (c) 2014 <a href="http://www.gutgames.com">James Craig</a>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.*/

using System;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Utility.任务
{
    /// <summary>
    /// Class that helps with running tasks in parallel on a set of objects (that will come in on an
    /// ongoing basis, think producer/consumer situations)
    /// </summary>
    /// <typeparam name="T">Object type to process</typeparam>
    public class TaskQueue<T> : BlockingCollection<T>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Capacity">
        /// Number of items that are allowed to be processed in the queue at one time
        /// </param>
        /// <param name="ProcessItem">Action that is used to process each item</param>
        /// <param name="HandleError">
        /// Handles an exception if it occurs (defaults to eating the error)
        /// </param>
        public TaskQueue(int Capacity, Action<T> ProcessItem, Action<Exception> HandleError = null)
            : base(new ConcurrentQueue<T>())
        {
            Contract.Requires<ArgumentException>(Capacity > 0, "Capacity must be greater than 0");
            this.ProcessItem = ProcessItem;
            if (this.HandleError == null)
            {
                this.HandleError = q => { };
            }
            this.CancellationToken = new CancellationTokenSource();
            Tasks = new Task[Capacity];
            for (int i = 0; i < Capacity; i++)
            {
                Tasks[i] = Task.Factory.StartNew(Process);
            }
        }

        /// <summary>
        /// Determines if it has been cancelled
        /// </summary>
        public bool IsCanceled
        {
            get { return CancellationToken.IsCancellationRequested; }
        }

        /// <summary>
        /// Determines if it has completed all tasks
        /// </summary>
        public bool IsComplete
        {
            get { return Tasks.All(x => x.IsCompleted); }
        }

        /// <summary>
        /// Token used to signal cancellation
        /// </summary>
        private CancellationTokenSource CancellationToken { get; set; }

        /// <summary>
        /// Called when an exception occurs when processing the queue
        /// </summary>
        private Action<Exception> HandleError { get; set; }

        /// <summary>
        /// Action used to process an individual item in the queue
        /// </summary>
        private Action<T> ProcessItem { get; set; }

        /// <summary>
        /// Group of tasks that the queue uses
        /// </summary>
        private Task[] Tasks { get; set; }

        /// <summary>
        /// Cancels the queue from processing
        /// </summary>
        /// <param name="Wait">
        /// Determines if the function should wait for the tasks to complete before returning
        /// </param>
        public void Cancel(bool Wait = false)
        {
            if (IsCompleted || IsCanceled)
                return;
            CancellationToken.Cancel(false);
            if (Wait)
                Task.WaitAll(Tasks);
        }

        /// <summary>
        /// Adds the item to the queue to be processed
        /// </summary>
        /// <param name="Item">Item to process</param>
        public void Enqueue(T Item)
        {
            Contract.Requires<InvalidOperationException>(!IsCompleted && !IsCanceled, "TaskQueue has been stopped");
            Add(Item);
        }

        /// <summary>
        /// Disposes of the objects
        /// </summary>
        /// <param name="disposing">
        /// True to dispose of all resources, false only disposes of native resources
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (Tasks != null)
            {
                Cancel(true);
                foreach (Task Task in Tasks)
                {
                    Task.Dispose();
                }
                Tasks = null;
            }
            if (CancellationToken != null)
            {
                CancellationToken.Dispose();
                CancellationToken = null;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Processes the queue
        /// </summary>
        private void Process()
        {
            Contract.Requires<NullReferenceException>(CancellationToken != null, "CancellationToken");
            Contract.Requires<NullReferenceException>(ProcessItem != null, "ProcessItem");
            while (true)
            {
                try
                {
                    ProcessItem(Take(CancellationToken.Token));
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    HandleError(ex);
                }
            }
        }
    }
}