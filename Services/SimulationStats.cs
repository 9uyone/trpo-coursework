using TRPO_Coursework.Interfaces;

namespace TRPO_Coursework.Services;

public class SimulationStats : IStatsReadOnly {
	private uint _totalCustomersServed;
	private double _weightedSum; // довжина * час (в секундах симуляції)
	private double _totalWaitingSeconds;
	private DateTime _lastChangeTime;
	private DateTime _startTime;
	private uint _queueLength;

	private readonly Lock _lock = new();
	private readonly Lock _lockTotalWaitingTime = new();

	// IStatsReadOnly
	public uint TotalCustomersServed => Volatile.Read(ref _totalCustomersServed);
	public uint MaxLength { get; private set; }
	public uint QueueLength => Volatile.Read(ref _queueLength);

	public double AverageWaitingTimeSeconds =>
		TotalCustomersServed == 0 ? 0 : _totalWaitingSeconds / TotalCustomersServed;

	public double AverageLength {
		get {
			lock (_lock) {
				var now = DateTime.UtcNow;
				var elapsedSeconds = (now - _lastChangeTime).TotalSeconds;
				var weightedSum = _weightedSum + QueueLength * elapsedSeconds;
				var totalTime = (now - _startTime).TotalSeconds;
				return totalTime <= 0 ? 0 : weightedSum / totalTime;
			}
		}
	}

	// Methods
	internal void IncrementCustomersServed() {
		Interlocked.Increment(ref _totalCustomersServed);
	}

	internal void IncrementQueue() {
		lock (_lock) {
			UpdateWeightedSum();
			_queueLength++;
			MaxLength = Math.Max(MaxLength, QueueLength);
		}
	}

	internal void DecrementQueue() {
		lock (_lock) {
			UpdateWeightedSum();
			_queueLength--;
		}
	}

	internal void AddWaitingTime(double waitingTime) {
		lock (_lockTotalWaitingTime) {
			_totalWaitingSeconds += waitingTime;
		}
	}

	internal void Reset() {
		_totalCustomersServed = 0;
		MaxLength = 0;
		_queueLength = 0;
		_totalWaitingSeconds = 0;
		_weightedSum = 0;

		_startTime = DateTime.UtcNow;
		_lastChangeTime = _startTime;
	}

	private void UpdateWeightedSum() {
		var now = DateTime.UtcNow;
		var elapsedSeconds = (now - _lastChangeTime).TotalSeconds;

		_weightedSum += QueueLength * elapsedSeconds;
		_lastChangeTime = now;
	}
}