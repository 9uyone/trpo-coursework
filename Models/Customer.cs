namespace TRPO_Coursework.Models;

public class Customer {
	private static uint _lastId = 1;
	public DateTime CreatedAt { get; init; }
	public DateTime ServiceStartedAt { get; private set; }
	public DateTime ServiceFinishedAt { get; private set; }

	public TimeSpan WaitingTime => ServiceStartedAt - CreatedAt;
	public TimeSpan ServiceTime => ServiceFinishedAt - ServiceStartedAt;
	public TimeSpan TotalTime => ServiceFinishedAt - CreatedAt;

	public uint Id { get; }

	public Customer() {
		Id = Interlocked.Increment(ref _lastId);
		CreatedAt = DateTime.UtcNow;
	}

	public void StartService() {
		if (ServiceStartedAt != default)
			throw new InvalidOperationException("Обслуговування вже почалося для цього покупця.");

		ServiceStartedAt = DateTime.Now;
	}

	public void FinishService() {
		if (ServiceStartedAt == default)
			throw new InvalidOperationException("Обслуговування ще не почалося для цього покупця.");

		if (ServiceFinishedAt != default)
			throw new InvalidOperationException("Обслуговування вже закінчилося для цього покупця.");

		ServiceFinishedAt = DateTime.Now;
	}
}
