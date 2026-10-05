namespace CarRental.Core;

public sealed class Booking
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public required string UserId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "Confirmed";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Car? Car { get; set; }
}

public sealed record CreateBookingRequest(DateOnly StartDate, DateOnly EndDate);