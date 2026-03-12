using System.Collections.Concurrent;
using TRPO_Coursework.Models;

namespace TRPO_Coursework.Services;

public class SimulationService {
	public ConcurrentQueue<Customer> Queue { get; private set; } = new();
	public List<CashDesk> CashDesks { get; private set; } = new();
	public uint TotalCustomersServed { get; private set; }


}