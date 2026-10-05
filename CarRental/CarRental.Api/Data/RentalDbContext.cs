using CarRental.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Api.Data;

public sealed class RentalDbContext(DbContextOptions<RentalDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Car>(entity =>
        {
            entity.HasKey(car => car.Id);
            entity.Property(car => car.Make).HasMaxLength(80).IsRequired();
            entity.Property(car => car.Model).HasMaxLength(80).IsRequired();
            entity.Property(car => car.DailyRate).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(booking => booking.Id);
            entity.Property(booking => booking.UserId).IsRequired();
            entity.Property(booking => booking.TotalPrice).HasPrecision(10, 2);
            entity.Property(booking => booking.Status).HasMaxLength(20).IsRequired();
            entity.HasOne(booking => booking.Car)
                .WithMany()
                .HasForeignKey(booking => booking.CarId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(booking => new { booking.CarId, booking.StartDate, booking.EndDate });
            entity.HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(booking => booking.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}