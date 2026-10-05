namespace CarRental.Api.Models;

/// <summary>Booking details visible to fleet administrators.</summary>
public sealed record AdminBookingResponse(
    int Id,
    string CarName,
    string CustomerEmail,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalPrice,
    string Status,
    DateTime CreatedAtUtc);
