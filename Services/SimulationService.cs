using SharpRaven.Utilities;
using System.Collections.Concurrent;
using TRPO_Coursework.Enums;
using TRPO_Coursework.Interfaces;
using TRPO_Coursework.Models;

namespace TRPO_Coursework.Services;

public class SimulationService {
	// State & sync
	private CancellationTokenSource? cancellationTokenSource;
	private readonly SemaphoreSlim semaphore = new(0);
	private readonly SimulationStats _stats = new SimulationStats();
	private ConcurrentQueue<Customer> Queue { get; set; } = new();

	// Public state
	public List<CashDesk> CashDesks { get; private set; } = new();
	public bool Running { get; private set; }
	public IStatsReadOnly Stats => _stats;
	public CircularBuffer<LogEntry> LogEntries { get; private set; } = new(1000);

	public event Action? OnChange;

	public SimulationService() {
		CashDesks = [new(), new(), new(), new()];
	}

	public void Start() {
		if (Running)
			return;

		Running = true;
		cancellationTokenSource = new();

		_ = Task.Run(() => CustomerGenerator(cancellationTokenSource.Token));
		for (var i = 0; i < CashDesks.Count; i++) {
			var cashDesk = CashDesks[i];
			_ = Task.Run(() => CashDeskWorker(cashDesk, cancellationTokenSource.Token));
		}

		LogEvent(EventType.SimulationStarted);
		//OnChange?.Invoke();
	}

	public void Stop() {
		if (!Running)
			return;

		Running = false;
		cancellationTokenSource?.Cancel();
		cancellationTokenSource?.Dispose();
		cancellationTokenSource = null;

		foreach (var cashDesk in CashDesks) {
			cashDesk.IsBusy = false;
			cashDesk.CurrentCustomer = null;
		}

		LogEvent(EventType.SimulationStopped);
		//OnChange?.Invoke();
	}

	private async Task CustomerGenerator(CancellationToken cancellationToken) {
		try {
			while (!cancellationToken.IsCancellationRequested) {
				await Task.Delay(Random.Shared.Next(2, 8) * 100, cancellationToken);
				var customer = new Customer();
				Queue.Enqueue(customer);

				_stats.IncrementQueue();
				semaphore.Release();
				LogEvent(EventType.CustomerEnqueued, customer.Id);
				//OnChange?.Invoke();
			}
		}
		catch (OperationCanceledException) {}
	}

	private async Task CashDeskWorker(CashDesk cashDesk, CancellationToken cancellationToken) {
		try {
			while (!cancellationToken.IsCancellationRequested) {
				await semaphore.WaitAsync(cancellationToken);

				if (!Queue.TryDequeue(out var customer)) {
					LogEvent(EventType.CustomerNotEnqueued, customer.Id, cashDesk.Id);
					continue;
				}

				LogEvent(EventType.CustomerDequeued, customer.Id, cashDesk.Id);

				_stats.DecrementQueue();
				customer.StartService();
				cashDesk.IsBusy = true;
				cashDesk.CurrentCustomer = customer;

				LogEvent(EventType.ServiceStarted, customer.Id, cashDesk.Id);
				//OnChange?.Invoke();

				await Task.Delay(Random.Shared.Next(2, 11) * 100, cancellationToken);

				_stats.IncrementCustomersServed();
				customer.FinishService();
				cashDesk.IsBusy = false;
				cashDesk.CurrentCustomer = null;

				LogEvent(EventType.ServiceFinished, customer.Id, cashDesk.Id);
				//OnChange?.Invoke();
			}
		}
		catch (OperationCanceledException) {
			cashDesk.IsBusy = false;
			cashDesk.CurrentCustomer = null;
			OnChange?.Invoke();
		}
	}

	private void LogEvent(EventType eventType, uint? customerId = null, uint? cashDeskId = null) {
		LogEntries.Add(new LogEntry(
			eventType,
			DateTime.Now,
			customerId,
			cashDeskId
		));
		OnChange?.Invoke();
	}
}
