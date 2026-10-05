# CarRental Project — Presentation Speaking Notes

> Use these notes with `CarRental_Project_Overview.html`. The speaker names and project-work split below are **suggested**, based on the project modules. The repository does not record individual authorship, so confirm the actual work with all five members before presenting it as fact.

## Slide-by-slide speaking notes

### Slide 1 — CarRental: project presentation
**Speaker: Karan Nale**

“Good morning. We are presenting CarRental, a web application for managing a vehicle fleet and date-based rentals. Our team is Karan Nale, Om Bangar, Sanket Narsale, Nishant Khatri, and Chirag Dayal. We will explain the user experience, project modules, database, data flow, and the main implementation choices.”

### Slide 2 — What the application does
**Speaker: Karan Nale**

“The application supports two main activities: fleet management and car reservations. A renter can browse vehicles and book dates. An administrator can maintain vehicle records and availability. Bookings are saved so the renter can review or cancel an eligible reservation.”

### Slide 3 — Two roles, distinct responsibilities
**Speaker: Nishant Khatri**

“We use two roles. A Member is a renter who can browse and book cars and manage their own reservations. An Admin manages the fleet, reviews every booking together with the customer email, and has additional access to the database viewer. Role checks happen on the server, not just by hiding buttons in the page.”

### Slide 4 — Reservation journey
**Speaker: Karan Nale**

“A member creates an account, signs in, chooses a car, selects dates, and submits a booking. The server rechecks the dates and availability before saving. The member can then see their own booking and cancel it before its start date.”

### Slide 5 — Admin fleet-management workflow
**Speaker: Karan Nale**

“An administrator can add a vehicle, edit its make, model, year, and daily rate, or change availability. The Admin dashboard also lists bookings with the car, dates, customer email, price, and status, and lets the Admin cancel a booking. A car can only be deleted if it has no booking history.”

### Slide 6 — Booking, pricing, and cancellation rules
**Speaker: Karan Nale**

“The start date cannot be in the past, the end date must be on or after the start date, and a booking can be at most 60 days. The car must be available and there cannot be another confirmed booking for overlapping dates. The total is the daily rate multiplied by the inclusive number of rental days.”

### Slide 7 — Application architecture
**Speaker: Om Bangar**

“The browser shows Razor pages and uses JavaScript to call the application. The ASP.NET Core application handles pages, API routes, authentication, and validation. Shared domain models are in CarRental.Core. Entity Framework Core connects the app to the SQLite database.”

### Slide 8 — Core data model and relationships
**Speaker: Sanket Narsale**

“The Car entity stores a vehicle’s identity, rate, and availability. The Booking entity stores which car and user made a reservation, the date range, price, status, and creation time. A booking points to one car and one user through foreign keys.”

### Slide 9 — HTTP API surface
**Speaker: Nishant Khatri**

“The API provides routes for listing and retrieving cars, managing cars as an Admin, and creating or viewing bookings as a Member. A separate Admin-only route lists all bookings with the associated customer email. Cancellation is allowed for the booking owner or an Admin. The endpoints return HTTP status codes such as 201 when a record is created, 400 for invalid input, and 409 for a conflict.”

### Slide 10 — Identity, authorization, and security
**Speaker: Nishant Khatri**

“ASP.NET Core Identity handles user accounts and sign-in. The application uses Admin and Member roles and requires authentication by default. Public registration always creates a Member; it cannot grant Admin privileges. An initial Admin is provisioned only when credentials are supplied through deployment configuration or secrets. Passwords have length and complexity rules, failed sign-in attempts cause a temporary lockout, and authentication cookies use protective settings.”

### Slide 11 — Interface and pages
**Speaker: Chirag Dayal**

“The home page changes what controls are shown based on the signed-in user’s role. Admins get fleet controls and an all-bookings dashboard showing the customer and car; Members get booking controls and their own booking list. Login and registration have separate pages. Admins also have a read-only database viewer that shows up to 200 rows.”

### Slide 12 — Validation and failure paths
**Speaker: Nishant Khatri**

“The server checks car fields and booking rules even if the browser already validates them. It rejects invalid dates, long bookings, unavailable cars, and overlapping reservations. It also prevents deleting cars with booking history and prevents a renter from cancelling someone else’s booking.”

### Slide 13 — Technology stack and solution layout
**Speaker: Om Bangar**

“The current solution targets .NET 10 and uses ASP.NET Core MVC, Minimal APIs, ASP.NET Core Identity, Entity Framework Core, and SQLite. CarRental.Api contains the web application and pages. CarRental.Core contains the shared car and booking models and request types.”

### Slide 14 — Local runbook and persistence
**Speaker: Chirag Dayal**

“When the app starts, it ensures the database setup is present, creates the Admin and Member roles if needed, provisions an initial Admin only when InitialAdmin credentials are configured, and adds demo car data when appropriate. SQLite uses the configured RentalDatabase connection string or a local fallback file. Production needs managed secrets, backups, and database migrations.”

### Slide 15 — Implemented scope and next steps
**Speaker: Chirag Dayal**

“The implemented features include accounts and roles, fleet operations, member booking management, Admin visibility into all bookings and their customers, SQLite persistence, and the admin database viewer. The slide also distinguishes future ideas—such as payments, vehicle locations, notifications, tests, and deployment automation—from features that are currently implemented.”

### Slide 16 — Project modules
**Speaker: Om Bangar**

“The project is organized by responsibility. Views build the screens, controllers handle account and database page actions, Program.cs configures the application and API routes, Core models describe the data, and RentalDbContext maps that data to tables. Entity Framework Core translates the application’s queries into SQL.”

### Slide 17 — End-to-end project workflow
**Speaker: Karan Nale**

“For a Member, the flow is sign in, load the fleet, select dates, pass server-side checks, save the booking, and later view or cancel it. For an Admin, the flow is sign in, review bookings and customer details, cancel when needed, or manage vehicles. In both cases, the server checks permissions before it accesses or changes data.”

### Slide 18 — Code in simple words
**Speaker: Om Bangar**

“Program.cs is the application’s setup and route map. AccountController handles login, registration, and logout. RentalDbContext describes database mappings. Car.cs and Booking.cs define the records. The Home view builds the page and calls the API. DatabaseController supports the Admin’s read-only table viewer.”

### Slide 19 — How data moves through the application
**Speaker: Om Bangar**

“The browser sends a request with the user’s authentication cookie. The endpoint checks sign-in, role, and input. EF Core turns the C# database operation into SQL for SQLite. The database result returns through EF Core and the API sends a response—often JSON—to the browser.”

### Slide 20 — Database tables, keys, and fields
**Speaker: Sanket Narsale**

“Cars and Bookings are the application’s main tables. A primary key uniquely identifies a row, for example Cars.Id or Bookings.Id. Booking.CarId and Booking.UserId are foreign keys that connect each booking to a car and an Identity user. Required fields and rate constraints help protect data quality.”

### Slide 21 — Entity-relationship diagram
**Speaker: Sanket Narsale**

“The diagram shows one Car can be related to many Bookings, while each Booking refers to one Car. One Identity user can also have many Bookings, while each booking belongs to one user. The foreign-key delete behavior is Restrict so related booking history is not accidentally removed.”

### Slide 22 — SQL used to create tables
**Speaker: Sanket Narsale**

“The schema creates Cars and Bookings if they do not already exist. It sets the primary keys, required fields, defaults, and foreign keys. It also creates an index over CarId and the start and end dates, which supports booking-date lookups. ASP.NET Core Identity creates its own account and role tables.”

### Slide 23 — Main SQL queries and data access
**Speaker: Sanket Narsale**

“The main database operations are selecting the fleet, finding one car by ID, checking for overlapping confirmed bookings, inserting a new booking, listing one user’s bookings, and updating a booking’s status when it is cancelled. In this project most SQL is generated by Entity Framework Core. The question marks represent parameters, which are supplied separately rather than joined into SQL text.”

### Slide 24 — Who explains what in the viva
**Speaker: All team members (briefly)**

“Here is our suggested split for explaining the project. Each person should describe the part they actually worked on, show its connection to the application, and answer questions together. We will update this slide if our real task allocation was different.”

### Slide 25 — Summary
**Speaker: Chirag Dayal**

“To summarize, CarRental combines fleet management, role-based access, and date-based bookings. The key ideas are validating a booking before saving it, connecting records with primary and foreign keys, and keeping database access behind the server. Thank you. We are ready for your questions.”

---

## Suggested team responsibilities for the viva

**Important:** These are suggested topic owners inferred from the project structure, not verified evidence of who wrote each part. Confirm the assignment with the group and replace it with the real contribution before presenting.

| Member | Suggested area to explain | Relevant project work to point to | Suggested slides |
|---|---|---|---|
| Karan Nale | Product purpose and user workflows | Member/Admin journeys; car browsing and booking flow | 1–2, 4–6, 17 |
| Om Bangar | Architecture, modules, and request/data flow | Program setup, shared Core models, views, EF Core context | 7, 13, 16, 18–19 |
| Sanket Narsale | Database, ER model, keys, and SQL | Cars/Bookings schema, relationships, constraints, index, data queries | 8, 20–23 |
| Nishant Khatri | API, accounts, security, and validation | Account handling, endpoint access rules, roles, booking checks | 3, 9–10, 12 |
| Chirag Dayal | User interface, demo, scope, and conclusion | Home/account/admin screens, run steps, implemented vs future work | 11, 14–15, 24–25 |

## Quick viva definitions

- **Primary key (PK):** the unique identifier for a row in a table.
- **Foreign key (FK):** a field that points to a row in another table and links the records.
- **Index:** an extra database structure that helps lookups run faster; this project indexes booking car/date fields.
- **Entity Framework Core (EF Core):** the library that maps C# objects to database records and generates SQL for common operations.
- **SQLite:** a relational database stored in a file, used here for the local application data.
- **Why check overlap?** Two confirmed bookings for the same car must not cover any of the same dates.
- **Why cancel by changing Status?** It keeps the booking history rather than deleting the record.
- **What is not currently implemented?** Payment processing, tax/invoice handling, vehicle location inventory, email/SMS notifications, and production deployment/monitoring are future extensions, not current features.
