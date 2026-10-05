using CarRental.Api.Data;
using CarRental.Api.Models;
using CarRental.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<RentalDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("RentalDatabase")
        ?? "Data Source=carrental-secure.db"));
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<RentalDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "CarRental.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.AddPolicy("FleetManagers", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Renters", policy => policy.RequireRole("Member"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultControllerRoute();

app.MapGet("/api/cars", [Authorize] async (RentalDbContext db) =>
    Results.Ok(await db.Cars.OrderBy(car => car.Id).ToListAsync()))
    .RequireAuthorization();

app.MapGet("/api/cars/{id:int}", [Authorize] async (int id, RentalDbContext db) =>
{
    var car = await db.Cars.FindAsync(id);
    return car is null ? Results.NotFound() : Results.Ok(car);
}).RequireAuthorization();

app.MapPost("/api/cars", [Authorize(Policy = "FleetManagers")] async (CreateCarRequest request, RentalDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Make) || string.IsNullOrWhiteSpace(request.Model) ||
        request.Year < 1886 || request.Year > DateTime.UtcNow.Year + 1 || request.DailyRate <= 0)
    {
        return Results.BadRequest(new { message = "Enter a make, model, valid year, and daily rate greater than zero." });
    }

    var car = new Car
    {
        Make = request.Make.Trim(),
        Model = request.Model.Trim(),
        Year = request.Year,
        DailyRate = request.DailyRate
    };
    db.Cars.Add(car);
    await db.SaveChangesAsync();
    return Results.Created($"/api/cars/{car.Id}", car);
}).RequireAuthorization("FleetManagers");

app.MapPut("/api/cars/{id:int}", [Authorize(Policy = "FleetManagers")] async (int id, UpdateCarRequest request, RentalDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Make) || string.IsNullOrWhiteSpace(request.Model) ||
        request.Year < 1886 || request.Year > DateTime.UtcNow.Year + 1 || request.DailyRate <= 0)
    {
        return Results.BadRequest(new { message = "Enter a make, model, valid year, and daily rate greater than zero." });
    }

    var car = await db.Cars.FindAsync(id);
    if (car is null)
    {
        return Results.NotFound();
    }

    car.Make = request.Make.Trim();
    car.Model = request.Model.Trim();
    car.Year = request.Year;
    car.DailyRate = request.DailyRate;
    await db.SaveChangesAsync();
    return Results.Ok(car);
}).RequireAuthorization("FleetManagers");

app.MapDelete("/api/cars/{id:int}", [Authorize(Policy = "FleetManagers")] async (int id, RentalDbContext db) =>
{
    var car = await db.Cars.FindAsync(id);
    if (car is null)
    {
        return Results.NotFound();
    }

    if (await db.Bookings.AnyAsync(booking => booking.CarId == id))
    {
        return Results.Conflict(new { message = "This car has booking history and cannot be deleted." });
    }

    db.Cars.Remove(car);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization("FleetManagers");

app.MapPut("/api/cars/{id:int}/availability", [Authorize(Policy = "FleetManagers")] async (int id, SetAvailabilityRequest request, RentalDbContext db) =>
{
    var car = await db.Cars.FindAsync(id);
    if (car is null)
    {
        return Results.NotFound();
    }

    car.IsAvailable = request.IsAvailable;
    await db.SaveChangesAsync();
    return Results.Ok(car);
}).RequireAuthorization("FleetManagers");

app.MapGet("/api/bookings/mine", [Authorize(Policy = "Renters")] async (ClaimsPrincipal user, RentalDbContext db) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var bookings = await db.Bookings
        .Where(booking => booking.UserId == userId)
        .OrderByDescending(booking => booking.CreatedAtUtc)
        .Select(booking => new
        {
            booking.Id,
            booking.CarId,
            CarName = booking.Car!.Year + " " + booking.Car.Make + " " + booking.Car.Model,
            booking.StartDate,
            booking.EndDate,
            booking.TotalPrice,
            booking.Status
        })
        .ToListAsync();
    return Results.Ok(bookings);
}).RequireAuthorization("Renters");

app.MapGet("/api/admin/bookings", async (RentalDbContext db) =>
{
    var bookings = await (
        from booking in db.Bookings.AsNoTracking()
        join account in db.Users on booking.UserId equals account.Id
        orderby booking.CreatedAtUtc descending
        select new AdminBookingResponse(
            booking.Id,
            booking.Car!.Year + " " + booking.Car.Make + " " + booking.Car.Model,
            account.Email ?? account.UserName ?? "Unknown account",
            booking.StartDate,
            booking.EndDate,
            booking.TotalPrice,
            booking.Status,
            booking.CreatedAtUtc))
        .ToListAsync();

    return Results.Ok(bookings);
}).RequireAuthorization("FleetManagers");

app.MapPost("/api/cars/{id:int}/bookings", [Authorize(Policy = "Renters")] async (
    int id, CreateBookingRequest request, ClaimsPrincipal user, RentalDbContext db) =>
{
    if (request.StartDate < DateOnly.FromDateTime(DateTime.UtcNow) || request.EndDate < request.StartDate)
    {
        return Results.BadRequest(new { message = "Choose valid dates. The start date cannot be in the past and the end date must be on or after it." });
    }

    var numberOfDays = request.EndDate.DayNumber - request.StartDate.DayNumber + 1;
    if (numberOfDays > 60)
    {
        return Results.BadRequest(new { message = "A booking can be at most 60 days." });
    }

    var car = await db.Cars.FindAsync(id);
    if (car is null)
    {
        return Results.NotFound(new { message = "Car not found." });
    }

    if (!car.IsAvailable)
    {
        return Results.Conflict(new { message = "This car is currently marked unavailable." });
    }

    var dateConflict = await db.Bookings.AnyAsync(booking =>
        booking.CarId == id && booking.Status == "Confirmed" &&
        booking.StartDate <= request.EndDate && booking.EndDate >= request.StartDate);
    if (dateConflict)
    {
        return Results.Conflict(new { message = "This car is already booked for one or more selected dates." });
    }

    var booking = new Booking
    {
        CarId = car.Id,
        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!,
        StartDate = request.StartDate,
        EndDate = request.EndDate,
        TotalPrice = car.DailyRate * numberOfDays
    };
    db.Bookings.Add(booking);
    await db.SaveChangesAsync();
    return Results.Created($"/api/bookings/mine", new
    {
        booking.Id,
        booking.CarId,
        CarName = $"{car.Year} {car.Make} {car.Model}",
        booking.StartDate,
        booking.EndDate,
        booking.TotalPrice,
        booking.Status
    });
}).RequireAuthorization("Renters");

app.MapDelete("/api/bookings/{id:int}", [Authorize] async (int id, ClaimsPrincipal user, RentalDbContext db) =>
{
    var booking = await db.Bookings.FindAsync(id);
    if (booking is null)
    {
        return Results.NotFound();
    }

    var isAdmin = user.IsInRole("Admin");
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!isAdmin && booking.UserId != userId)
    {
        return Results.Forbid();
    }

    if (booking.StartDate <= DateOnly.FromDateTime(DateTime.UtcNow) && !isAdmin)
    {
        return Results.BadRequest(new { message = "A booking can only be cancelled before its start date." });
    }

    booking.Status = "Cancelled";
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RentalDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS Bookings (
            Id INTEGER NOT NULL CONSTRAINT PK_Bookings PRIMARY KEY AUTOINCREMENT,
            CarId INTEGER NOT NULL,
            UserId TEXT NOT NULL,
            StartDate TEXT NOT NULL,
            EndDate TEXT NOT NULL,
            TotalPrice TEXT NOT NULL,
            Status TEXT NOT NULL,
            CreatedAtUtc TEXT NOT NULL,
            CONSTRAINT FK_Bookings_Cars_CarId FOREIGN KEY (CarId) REFERENCES Cars (Id) ON DELETE RESTRICT,
            CONSTRAINT FK_Bookings_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE RESTRICT
        );
        CREATE INDEX IF NOT EXISTS IX_Bookings_CarId_StartDate_EndDate ON Bookings (CarId, StartDate, EndDate);
        """);
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        var roleResult = await roleManager.CreateAsync(new IdentityRole("Admin"));
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(error => error.Description)));
        }
    }

    if (!await roleManager.RoleExistsAsync("Member"))
    {
        var roleResult = await roleManager.CreateAsync(new IdentityRole("Member"));
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(error => error.Description)));
        }
    }

    var configuredAdminEmail = app.Configuration["InitialAdmin:Email"];
    var configuredAdminPassword = app.Configuration["InitialAdmin:Password"];
    var fallbackDevAdminEmail = "admin@carrental.local";
    var fallbackDevAdminPassword = "Admin@123456";

    var adminEmail = string.IsNullOrWhiteSpace(configuredAdminEmail)
        ? (app.Environment.IsDevelopment() ? fallbackDevAdminEmail : null)
        : configuredAdminEmail;
    var adminPassword = string.IsNullOrWhiteSpace(configuredAdminPassword)
        ? (app.Environment.IsDevelopment() ? fallbackDevAdminPassword : null)
        : configuredAdminPassword;

    if (string.IsNullOrWhiteSpace(adminEmail) != string.IsNullOrWhiteSpace(adminPassword))
    {
        throw new InvalidOperationException("Configure both InitialAdmin:Email and InitialAdmin:Password, or leave both unset.");
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    if (!string.Equals(adminEmail, fallbackDevAdminEmail, StringComparison.OrdinalIgnoreCase))
    {
        var legacyAdmin = await userManager.FindByEmailAsync(fallbackDevAdminEmail);
        if (legacyAdmin is not null && await userManager.IsInRoleAsync(legacyAdmin, "Admin"))
        {
            var removeRoleResult = await userManager.RemoveFromRoleAsync(legacyAdmin, "Admin");
            if (!removeRoleResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", removeRoleResult.Errors.Select(error => error.Description)));
            }
        }
    }

    if (!string.IsNullOrWhiteSpace(adminEmail))
    {
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var createResult = await userManager.CreateAsync(adminUser, adminPassword!);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(error => error.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(error => error.Description)));
            }
        }
    }

    if (!await db.Cars.AnyAsync())
    {
        db.Cars.AddRange(
            new Car { Make = "Toyota", Model = "Corolla", Year = 2022, DailyRate = 45 },
            new Car { Make = "Honda", Model = "Civic", Year = 2023, DailyRate = 52 },
            new Car { Make = "Ford", Model = "Focus", Year = 2021, DailyRate = 40 });
        await db.SaveChangesAsync();
    }
}

app.Run();
