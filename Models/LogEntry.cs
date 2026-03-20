using TRPO_Coursework.Enums;

namespace TRPO_Coursework.Models;

public record LogEntry(
	EventType Event,
	DateTime Timestamp,
	uint? CustomerId = null,
	uint? CashDeskId = null
);
