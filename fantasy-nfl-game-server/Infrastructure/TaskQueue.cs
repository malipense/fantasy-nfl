using System;
using System.Collections.Generic;

namespace Game.Infrastructure
{
    internal sealed class TaskQueue
    {
        private readonly object _lock = new object();
        private Task _lastQueueTask;
        private volatile bool _drained;
        private readonly int? _maxSize;
        private long _size;

        public TaskQueue():this(TaskAsyncHelper.Empty)
        {   }
        public TaskQueue(Task initialTask)
        {
            _lastQueueTask = initialTask;
        }
        public TaskQueue(Task initialTask, int maxSize)
        {
            _lastQueueTask = initialTask;
            _maxSize = maxSize;
        }

        public IPerformanceCounter QueueSizeCounter { get; set; }
        public bool IsDrained 
        { 
            get
            {
                return this._drained;
            } 
        }

        public Task Enqueue(Func<object, Task> taskFunc, object state)
        {
            lock (_lock)
            {
                if (_drained)
                {
                    return _lastQueueTask;
                }

                if (_maxSize != null)
                {
                    if (Interlocked.Increment(ref _size) > _maxSize)
                    {
                        Interlocked.Decrement(ref _size);
                        return null;
                    }

                    var counter = QueueSizeCounter;
                    if (counter != null)
                    {
                        counter.Increment();
                    }
                }

                var newTask = _lastQueueTask.Then((n, ns, q) => q.InvokeNext(n, ns), taskFunc, state, this);

                _lastQueueTask = newTask;
                return newTask;
            }
        }
        private void Dequeue()
        {
            if (_maxSize != null)
            {
                // Decrement the number of items left in the queue
                Interlocked.Decrement(ref _size);

                var counter = QueueSizeCounter;
                if (counter != null)
                {
                    counter.Decrement();
                }
            }
        }
        private Task InvokeNext(Func<object, Task> next, object nextState)
        {
            return next(nextState).Finally(s => ((TaskQueue)s).Dequeue(), this);
        }
    }
}
