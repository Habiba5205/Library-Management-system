# Library Management System

ASP.NET Core MVC web application for managing a library catalog, users, borrowing records, payments, fines, and role-based access.

This project was built as an MVC application, not a Web API. The frontend uses Razor Views and Bootstrap, while the backend uses MVC controllers, services, repositories, Entity Framework Core, and SQL Server LocalDB.

## Features

### Automatic Overdue Fines

Manual fine creation is disabled. New fines are generated automatically;
previously recorded manual fines remain in the database.

- Active loans accrue 5 EGP per overdue calendar day, starting the day after
  the due date. An early-return request alone does not stop the calculation.
- There is one automatic fine per borrowing. The amount catches up after
  downtime and stops growing on the recorded return date.
- Checks run every minute while the app is running and before page requests.
- Existing active overdue loans are included. Old completed loans without an
  automatic fine are not retroactively charged. Existing manual fines remain
  separate and unchanged.
- Members see their own fines. Managers can mark an automatic fine Paid after
  the book is returned, but cannot change its amount, reassign it, waive it, or
  delete it.
- Apply the AddAutomaticOverdueFines migration before running the updated app.

### Payment Flow

- Members choose Cash or Card when reserving a book.
- Cash holds the book for 48 hours. The manager confirms receipt of cash from
  Payment Details. This marks the payment Paid and starts the loan on that day.
- Unpaid cash reservations expire as Failed, releasing the book.
- Card opens a Development-only demo checkout. Success starts the borrowing
  automatically; failure or cancellation releases the book. Managers cannot
  confirm, edit, or delete card payments.
- Demo checkout never collects card details or charges money. A real provider
  integration is still required before enabling card payment outside Development.
- Abandoned demo card checkouts expire after 30 minutes.
- Expiry runs every minute while the app is running and before page requests.
  After downtime, overdue reservations expire on startup or the next request.
- Existing borrowing history is preserved; these rules apply to new reservations.

Payment integration checks use an isolated temporary LocalDB database:

```powershell
dotnet run --project Tests/PaymentFlow/PaymentFlow.csproj --configuration PaymentTests
```

- Dashboard with library statistics.
- Category management.
- Author management.
- Book management with category and author selection.
- Book search by title or ISBN.
- Book filtering by category and author.
- User management with hashed passwords.
- Borrowing and return flow.
- Automatic pending payment creation when a member borrows a book.
- Early return request flow for members returning before the due date.
- Payment management and updates.
- Fine management for borrowing records.
- Member sign up.
- Cookie-based login and logout.
- Role-based authorization.

## Roles

### Admin

- Can view records.
- Can review and manage users.
- Can create staff credentials for Admin and Manager users.
- Cannot borrow or return books.

### Manager

- Can manage the catalog, borrowings, and fines.
- Can view payments and confirm pending cash payments before reservation expiry.
- Cannot manually alter card payments.
- Cannot review or manage users.

### Member

- Can sign up from the login page.
- Can view available books.
- Can borrow available books.
- Can return their own borrowed books.
- Can view their own borrowings, payments, and fines.

## Demo Accounts

These accounts are created automatically when the app starts if they do not already exist.

| Role | Username | Password |
| --- | --- | --- |
| Admin | `admin` | `Admin@123` |
| Manager | `manager` | `Manager@123` |
| Member | `member` | `Member@123` |

## Technology Stack

### Frontend

- ASP.NET Core MVC Razor Views (`.cshtml`)
- Bootstrap
- Custom CSS
- jQuery validation from the MVC template

### Backend

- ASP.NET Core MVC
- C#
- Service-Repository pattern
- Entity Framework Core
- SQL Server LocalDB
- EF Core Migrations
- ASP.NET Core Cookie Authentication
- ASP.NET Core `PasswordHasher<User>`

## Architecture

The project follows the MVC pattern with a Service-Repository layer:

- Controllers handle browser requests and return Razor Views.
- Services contain business rules such as borrowing, returning, member sign up, payments, fines, and dashboard logic.
- Repositories handle database queries using Entity Framework Core.
- Models represent the database entities.
- ViewModels shape data for forms and pages.

## Database

Server:

```text
(localdb)\MSSQLLocalDB
```

Database:

```text
LibSystemMvcDb
```

Main tables:

- Roles
- Users
- Books
- Authors
- Categories
- BookAuthors
- Borrowings
- Payments
- Fines

## Setup

1. Clone the repository:

```powershell
git clone https://github.com/Habiba5205/Library-Management-system.git
cd Library-Management-system
```

2. Restore packages:

```powershell
dotnet restore
```

3. Restore local tools:

```powershell
dotnet tool restore
```

4. Create/update the database:

```powershell
dotnet tool run dotnet-ef database update
```

5. Run the application:

```powershell
dotnet run --project Lib_System.csproj
```

6. Open the browser:

```text
http://localhost:5137
```

If the folder contains both a project file and a solution file, build the project directly:

```powershell
dotnet build Lib_System.csproj
```

## Suggested Test Flow

1. Login as Manager.
2. Create a category.
3. Create an author.
4. Login as Admin and create a member user.
5. Create a book and assign category/author.
6. Login as Member.
7. Borrow an available book.
8. Confirm a pending payment appears for the borrowing.
9. If the member returns before the due date, send an early return request.
10. Login as Manager and process the requested return.
11. Add a fine to a borrowing record.
12. Update the borrowing payment if needed.
13. Login as Admin and confirm records are view-only.

## Project Structure

```text
Controllers/
Data/
Migrations/
Models/
Repositories/
Services/
ViewModels/
Views/
wwwroot/
```
