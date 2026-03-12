namespace TRPO_Coursework.Models;

public class Customer {
	private static uint _lastId = 1;

	public uint Id { get; }

	public Customer() {
		Id = Interlocked.Increment(ref _lastId);
	}
}