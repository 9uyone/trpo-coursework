using TRPO_Coursework.Interfaces;

namespace TRPO_Coursework.Services;

public class SimulationStats: IStatsReadOnly {
	private uint _totalCustomersServed;
	private uint _currentLength;
	private uint _sumLengths;
	private uint _countSamples;

	// IStatsReadOnly
	public uint TotalCustomersServed => Volatile.Read(ref _totalCustomersServed);

	public uint MaxLength { get; private set; }
	public double AverageLength => (double)_sumLengths / _countSamples;
	public uint QueueLength => _currentLength;

	// Methods to update stats
	internal void IncrementCustomersServed() {
		Interlocked.Increment(ref _totalCustomersServed);
	}

	internal void IncrementQueue() {
		_currentLength++;
		MaxLength = Math.Max(MaxLength, _currentLength);
		_sumLengths += _currentLength;
		_countSamples++;
	}

	internal void DecrementQueue() {
		_currentLength--;
		_sumLengths += _currentLength;
		_countSamples++;
	}
}