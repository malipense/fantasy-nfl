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
            }
        }
    }
}
