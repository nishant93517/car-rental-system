namespace CarRental.Core;

public sealed class Car
{
    public int Id { get; set; }
    public required string Make { get; set; }
    public required string Model { get; set; }
    public int Year { get; set; }
    public decimal DailyRate { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public sealed record CreateCarRequest(string Make, string Model, int Year, decimal DailyRate);

public sealed record UpdateCarRequest(string Make, string Model, int Year, decimal DailyRate);

public sealed record SetAvailabilityRequest(bool IsAvailable);