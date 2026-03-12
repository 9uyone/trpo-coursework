using System.Collections.Concurrent;
using TRPO_Coursework.Models;

namespace TRPO_Coursework.Services;

public class SimulationService {
	public ConcurrentQueue<Customer> Queue { get; private set; } = new();
	public List<CashDesk> CashDesks { get; private set; } = new();
	public uint TotalCustomersServed { get; private set; }
	public bool Running { get; private set; }

	private readonly SemaphoreSlim desks = new(4);

	public event Action? OnChange;


	public SimulationService() {
		CashDesks = [new(), new(), new(), new()];
		
	}

	public void Start() {
		Running = true;
	}

	public void Stop() {
		Running = false;
	}

	private void ConsumerGenerator() {
		new NotImplementedException();
	}

	private void CashDeskWorker(uint id) {
		throw new NotImplementedException();
	}
}