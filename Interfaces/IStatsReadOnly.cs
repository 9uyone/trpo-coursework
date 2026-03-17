namespace TRPO_Coursework.Interfaces;

public interface IStatsReadOnly {
	uint TotalCustomersServed { get; }

	uint QueueLength { get; }
	uint MaxLength { get; }
	double AverageLength { get; }
}