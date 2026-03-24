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
	private (uint, uint) _generationIntervalSecs = (2 * 60, 7 * 60); // 2 to 8 minutes in seconds
	private (uint, uint) _servingIntervalSecs = (1 * 60, 10 * 60);

	// Public state
	public List<CashDesk> CashDesks { get; private set; } = new();
	public bool Running { get; private set; }
	public IStatsReadOnly Stats => _stats;
	public CircularBuffer<LogEntry> LogEntries { get; private set; } = new(1000);
	public CircularBuffer<Customer> CustomerEntries { get; private set; } = new(100);

	public uint SpeedUpTimes { get; 
		set {
			if (Running)
				throw new InvalidOperationException("Не можна змінювати швидкість симуляції під час запуску.");
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), "SpeedUpTimes має бути >= 1.");
			field = value;
		}
	} = 250;
	public (uint, uint) GenerationIntervalMs { get => _generationIntervalSecs;
		set { if (value.Item1 > value.Item2)
				throw new ArgumentException("Мінімальний час генерації має бути <= максимальному часу.");
		_generationIntervalSecs = value;
	} }

	public (uint, uint) ServingIntervalMs { get => _servingIntervalSecs;
		set { if (value.Item1 > value.Item2)
				throw new ArgumentException("Мінімальний час обслуговуванний має бути <= максимальному часу.");
		_servingIntervalSecs = value;
	} }

	public event Action? OnChange;

	// Ctor & public methods
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

	// Private methods
	private async Task CustomerGenerator(CancellationToken cancellationToken) {
		try {
			while (!cancellationToken.IsCancellationRequested) {
				var (min, max) = GenerationIntervalMs;
				var delay = Random.Shared.Next((int)(min * 1000.0 / SpeedUpTimes), (int)(max * 1000.0 / SpeedUpTimes));
				await Task.Delay(delay, cancellationToken);
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
					LogEvent(EventType.CustomerNotEnqueued, cashDeskId: cashDesk.Id);
					continue;
				}

				LogEvent(EventType.CustomerDequeued, customer.Id, cashDesk.Id);

				_stats.DecrementQueue();
				customer.StartService();
				cashDesk.IsBusy = true;
				cashDesk.CurrentCustomer = customer;

				LogEvent(EventType.ServiceStarted, customer.Id, cashDesk.Id);
				//OnChange?.Invoke();

				var (min, max) = ServingIntervalMs;
				var delay = Random.Shared.Next((int)(min * 1000 / SpeedUpTimes), (int)(max * 1000 / SpeedUpTimes));
				await Task.Delay(delay, cancellationToken);

				_stats.IncrementCustomersServed();
				customer.FinishService();
				cashDesk.IsBusy = false;
				cashDesk.CurrentCustomer = null;
				
				CustomerEntries.Add(customer);
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
