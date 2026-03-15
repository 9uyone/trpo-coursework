using System.Collections.Concurrent;
using TRPO_Coursework.Models;

namespace TRPO_Coursework.Services;

public class SimulationService {
	private int totalCustomersServed;
	private CancellationTokenSource? cancellationTokenSource;

	public ConcurrentQueue<Customer> Queue { get; private set; } = new();
	public List<CashDesk> CashDesks { get; private set; } = new();
	public uint TotalCustomersServed => (uint)Volatile.Read(ref totalCustomersServed);
	public bool Running { get; private set; }

	public event Action? OnChange;
	private readonly SemaphoreSlim semaphore = new(0);

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

				cashDesk.IsBusy = true;
				cashDesk.CurrentCustomer = customer;
				OnChange?.Invoke();

				await Task.Delay(Random.Shared.Next(2, 11) * 100, cancellationToken);
				Interlocked.Increment(ref totalCustomersServed);

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