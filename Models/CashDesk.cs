namespace TRPO_Coursework.Models;

public class CashDesk {
	private static uint _lastId = 1;

	public CashDesk() {
		Id = Interlocked.Increment(ref _lastId);
	}

	public uint Id { get; }

	public bool IsBusy { get; set; }
	public Customer? CurrentCustomer { get; set; }
}