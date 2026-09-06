# Library Management System

ASP.NET Core MVC web application for managing a library catalog, users, borrowing records, payments, fines, and role-based access.

This project was built as an MVC application, not a Web API. The frontend uses Razor Views and Bootstrap, while the backend uses MVC controllers, Entity Framework Core, and SQL Server LocalDB.

## Features

- Dashboard with library statistics.
- Category management.
- Author management.
- Book management with category and author selection.
- Book search by title or ISBN.
- Book filtering by category and author.
- User management with hashed passwords.
- Borrowing and return flow.
- Payment management.
- Fine management for borrowing records.
- Cookie-based login and logout.
- Role-based authorization.

## Roles

### Admin

- Can view records.
- Can review and manage users.
- Cannot borrow or return books.

### Manager

- Can manage and control the system.
- Can create, edit, delete, and view categories, authors, books, borrowings, payments, and fines.
- Cannot review or manage users.

### Member

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
- Entity Framework Core
- SQL Server LocalDB
- EF Core Migrations
- ASP.NET Core Cookie Authentication
- ASP.NET Core `PasswordHasher<User>`

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
ViewModels/
Views/
wwwroot/
```
