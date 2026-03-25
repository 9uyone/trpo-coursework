using TRPO_Coursework.Interfaces;

namespace TRPO_Coursework.Services;

public class SimulationStats: IStatsReadOnly {
	private uint _totalCustomersServed;
	private uint _sumLengths;
	private uint _countSamples;

	// IStatsReadOnly
	public uint TotalCustomersServed => Volatile.Read(ref _totalCustomersServed);
	public uint MaxLength { get; private set; }
	public double AverageLength => (double)_sumLengths / _countSamples;
	public uint QueueLength {  get; private set; }

	// Methods to update stats
	internal void IncrementCustomersServed() {
		Interlocked.Increment(ref _totalCustomersServed);
	}

	internal void IncrementQueue() {
		QueueLength++;
		MaxLength = Math.Max(MaxLength, QueueLength);
		_sumLengths += QueueLength;
		_countSamples++;
	}

	internal void DecrementQueue() {
		QueueLength--;
		_sumLengths += QueueLength;
		_countSamples++;
	}

	internal void Reset() {
		_totalCustomersServed = 0;
		MaxLength = 0;
		_sumLengths = 0;
		_countSamples = 0;
		QueueLength = 0;
	}
}
