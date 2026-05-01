namespace TRPO_Coursework.Models;

public class CashDesk {
	private static uint _lastId = 0;

	public CashDesk() {
		Id = Interlocked.Increment(ref _lastId);
	}

	public uint Id { get; }

	public bool IsBusy => CurrentCustomer != null;
	public Customer? CurrentCustomer { get; set; }
}