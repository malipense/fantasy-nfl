using System.Diagnostics; 

namespace Game.Infrastructure
{
    public interface IPerformanceCounter
    {
        string CounterName { get; }
        long Decrement();
        long Increment();
        long IncrementBy(long value);
        CounterSample NextSample();
        long RawValue { get; set; }
        void Close();
        void RemoveInstance();
    }
}
