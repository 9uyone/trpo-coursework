using System.Collections.Concurrent;
using TRPO_Coursework.Interfaces;
using TRPO_Coursework.Models;

namespace TRPO_Coursework.Services;

public class SimulationService {
	private CancellationTokenSource? cancellationTokenSource;

	private ConcurrentQueue<Customer> Queue { get; set; } = new();
	public List<CashDesk> CashDesks { get; private set; } = new();
	
	public bool Running { get; private set; }

	public event Action? OnChange;
	private readonly SemaphoreSlim semaphore = new(0);

	// Stats
	private readonly SimulationStats _stats = new SimulationStats();
	public IStatsReadOnly Stats => _stats;

	public SimulationService() {
		CashDesks = [new(), new(), new(), new()];
	}

	public void Start() {
		if (Running) {
			return;
		}

		Running = true;
		cancellationTokenSource = new();

		_ = Task.Run(() => CustomerGenerator(cancellationTokenSource.Token));
		for (var i = 0; i < CashDesks.Count; i++) {
			var cashDesk = CashDesks[i];
			_ = Task.Run(() => CashDeskWorker(cashDesk, cancellationTokenSource.Token));
		}

		OnChange?.Invoke();
	}

	public void Stop() {
		if (!Running) {
			return;
		}

		Running = false;
		cancellationTokenSource?.Cancel();
		cancellationTokenSource?.Dispose();
		cancellationTokenSource = null;

		foreach (var cashDesk in CashDesks) {
			cashDesk.IsBusy = false;
			cashDesk.CurrentCustomer = null;
		}

		OnChange?.Invoke();
	}

	private async Task CustomerGenerator(CancellationToken cancellationToken) {
		try {
			while (!cancellationToken.IsCancellationRequested) {
				await Task.Delay(Random.Shared.Next(2, 8) * 100, cancellationToken);
				Queue.Enqueue(new Customer());
				_stats.IncrementQueue();
				semaphore.Release();
				OnChange?.Invoke();
			}
		}
		catch (OperationCanceledException) {}
	}

	private async Task CashDeskWorker(CashDesk cashDesk, CancellationToken cancellationToken) {
		try {
			while (!cancellationToken.IsCancellationRequested) {
				await semaphore.WaitAsync(cancellationToken);

				if (!Queue.TryDequeue(out var customer)) {
					continue;
				}

				_stats.DecrementQueue();
				customer.StartService();
				cashDesk.IsBusy = true;
				cashDesk.CurrentCustomer = customer;

				OnChange?.Invoke();

				await Task.Delay(Random.Shared.Next(2, 11) * 100, cancellationToken);

				_stats.IncrementCustomersServed();
				customer.FinishService();
				cashDesk.IsBusy = false;
				cashDesk.CurrentCustomer = null;

				OnChange?.Invoke();
			}
		}
		catch (OperationCanceledException) {
			cashDesk.IsBusy = false;
			cashDesk.CurrentCustomer = null;
			OnChange?.Invoke();
		}
	}
}