# Library Management System

ASP.NET Core MVC web application for managing a library catalog, users, borrowing records, payments, fines, and role-based access.

This project was built as an MVC application, not a Web API. The frontend uses Razor Views and Bootstrap, while the backend uses MVC controllers, services, repositories, Entity Framework Core, and SQL Server LocalDB. The Librarium interface is adapted from the Figma design; the running application does not use React or the export's sample data.

## Current Preview

Open **http://localhost:5147** for the current local design preview.
The default launch profile still uses port 5137; an older process on that
port may show an earlier build. Preview servers run only while their process
is active.

### Interface

- Role-specific sidebar and responsive mobile navigation.
- Locally hosted Inter font, Lucide icons and field-specific placeholders.
- Dashboard activity lists and role-specific shortcuts.
- Member book grid with placeholders when cover images are unavailable.
- Managers can upload, replace or remove book covers, with a preview before saving.
- Staff tables with search, 10/25/50-row client-side pagination and icon actions.
- Hover/focus tooltips, row highlighting, button loading states and
  client-side duplicate-submit guards.
- Confirmation dialogs before acknowledging cash payments or cash fine returns.
- Catalog/user create, edit and delete dialogs using real MVC forms.
  Direct form URLs and normal server-side validation remain supported.
- Cash/Card selection for member borrowing.

Build and HTTP page checks passed. Desktop/mobile visual matching and the
new dialog, search and pagination interactions still require browser testing.

Submission-guard unit tests (Node.js required):

```powershell
node --test Tests/UI/interactions.test.cjs
```

## Features

### Automatic Overdue Fines

Manual fine creation is disabled. New fines are generated automatically;
previously recorded manual fines remain in the database.

- Active loans accrue 5 USD per overdue calendar day, starting the day after
  the due date. An early-return request alone does not stop the calculation.
- There is one automatic fine per borrowing. The amount catches up after
  downtime and stops growing on the recorded return date.
- Checks run every minute while the app is running and before page requests.
- Existing active overdue loans are included. Old completed loans without an
  automatic fine are not retroactively charged. Existing manual fines remain
  separate and unchanged.
- Members see and pay only their own fines using Cash or Stripe Checkout.
  Unpaid fines block book returns.
- Cash stays pending until the manager confirms receipt of cash and the book.
  Successful card checkout settles automatically, with no manager action.
- Fine payment and the book return are saved together once all fines for that
  borrowing are settled. Failed or cancelled card attempts can be retried.
- Fines continue to increase while payment is pending. If the amount changes,
  the member must confirm it again. Cash fine payments have no 48-hour expiry.
- Fine edit/delete endpoints are removed; manual edits cannot bypass payment.
- Older outstanding manual fines must be settled before the automatic overdue
  fine so the overdue amount continues to accrue until the actual return.
- Apply the AddAutomaticOverdueFines migration before running the updated app.
- The AddFinePayments migration stores the selected method, quoted amount,
  current attempt identifier, payment status, and settlement time.

### Payment Flow

- Members choose Cash or Card when reserving a book.
- Cash holds the book for 48 hours. The manager confirms receipt of cash from
  Payment Details. This marks the payment Paid and starts the loan on that day.
- Unpaid cash reservations expire as Failed, releasing the book.
- Card opens Stripe Checkout when configured. Verified success starts the borrowing
  automatically; failure or cancellation releases the book. Managers cannot
  confirm, edit, or delete card payments.
- Card details are entered on Stripe's hosted checkout, not in this app.
  Use Stripe test keys for development. If Stripe is not configured, card
  checkout is unavailable; Cash remains supported.
- Abandoned card reservations expire after 30 minutes.
- Successful payment starts a fixed 14-day loan. All amounts are in USD.
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

- Can manage the catalog, process returns and view automatic fines.
- Can confirm cash fine payments together with the book return.
- Can view payments and confirm pending cash payments before reservation expiry.
- Cannot manually alter card payments.
- Cannot review or manage users.

### Member

- Can sign up from the login page.
- Can view the full catalog on the dashboard and browse available books.
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
- JavaScript for table search, pagination and MVC form dialogs
- Lucide icons and Inter font (served locally with licenses)
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
- Stripe.net for hosted checkout and webhook verification

## Architecture

The project follows the MVC pattern with a Service-Repository layer:

- Controllers handle browser requests and return Razor Views.
- Services contain business rules such as borrowing, returning, member sign up, payments, fines, and dashboard logic.
- Repositories handle database queries using Entity Framework Core.
- Models represent the database entities.
- ViewModels shape data for forms and pages.

## Database

### Book Covers

Optional covers accept JPG, PNG or WebP files up to 5 MB. The server decodes
the image, rejects dimensions above 6,000 pixels per side or 16 megapixels,
and resizes it to fit within 900 x 1,200 pixels. Metadata is discarded and
the image is saved as PNG data in the book's database record.

The `AddBookCoverImage` migration adds a nullable column; existing books keep
their placeholders. Covers are available to authenticated library users.
Editing without a new upload preserves the current cover. Select either a
replacement file or removal, not both. After a server-side validation error,
select the upload again before resubmitting.

Database backups include covers. Git pulls do not sync uploaded images or
other local database records between laptops.

Image processing uses [ImageSharp 3.1.12](https://www.nuget.org/packages/SixLabors.ImageSharp/3.1.12).
Review its linked license before commercial distribution.

Run validation and persistence tests (uses a temporary LocalDB database):

```powershell
dotnet run --project Tests/BookCovers/BookCovers.csproj
```

Each device has its own LocalDB database. Git pulls share code and
migrations, not books or other records added on a teammate's device.

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

Requires Windows, the .NET 10 SDK and SQL Server Express LocalDB.

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
dotnet run --project Lib_System.csproj --launch-profile http --urls http://localhost:5147
```

6. Open the browser:

```text
http://localhost:5147
```

If the folder contains both a project file and a solution file, build the project directly:

```powershell
dotnet build Lib_System.csproj
```

The app also applies pending migrations at startup. If port 5147 is already
occupied by the preview, use it directly or choose another free port.
Running without `--urls` uses the default profile URL, http://localhost:5137.

### Stripe Configuration

Cash testing requires no Stripe account. For card testing, use your own
Stripe test-mode credentials, never live keys or real card details.
Keep secrets out of Git:

```powershell
dotnet user-secrets init --project Lib_System.csproj
dotnet user-secrets set "Stripe:SecretKey" "sk_test_YOUR_KEY" --project Lib_System.csproj
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_YOUR_KEY" --project Lib_System.csproj
```

With the Stripe CLI installed and authenticated, forward local events:

```powershell
stripe listen --forward-to http://localhost:5147/stripe/webhook
```

Store the signing secret printed by that command, then restart the app:

```powershell
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_YOUR_SECRET" --project Lib_System.csproj
```

The app verifies Stripe payment state through webhook events and checkout
return reconciliation. The return URL alone does not prove payment succeeded.

## Suggested Test Flow

1. Login as Manager.
2. Create a category.
3. Create an author.
4. Create a book and assign its category/authors.
5. Sign up as a Member, or use the seeded member account.
6. Borrow an available book with Cash and verify the pending reservation.
7. Login as Manager and confirm the cash payment; verify the 14-day loan.
8. As Member, request an early return; as Manager, process the request.
9. With Stripe test configuration, check successful, cancelled and failed
   card checkout without manager confirmation.
10. Use the isolated payment test harness for overdue fines and expiry cases.
    New fines are automatic; there is no manual fine creation.
11. Verify unpaid fines block returns and cash/card settlement follows the
    appropriate manager or Stripe flow.
12. Login as Admin and verify catalog/payment records are view-only while
    user credential and status management remains available.
13. Check search, pagination, form dialogs and validation on desktop/mobile.

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
