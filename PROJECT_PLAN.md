# Library Management System MVC Plan

## Project Direction

Build this as an ASP.NET Core MVC application, not a Web API.

The original specification describes API endpoints, Swagger, and optional JWT authentication. For this MVC version, convert those requirements into controllers, Razor views, forms, validation, and role-based pages.

## Main Modules

1. Books
   - Add, edit, delete, list, details.
   - Search by title and ISBN.
   - Filter by category and author.
   - Show availability status.

2. Authors
   - Add, edit, delete, list, details.
   - Show books written by each author.

3. Categories
   - Add, edit, delete, list.
   - Show books belonging to each category.

4. Members
   - Register member.
   - Edit member profile.
   - Delete member.
   - Show borrowed books for a member.

5. Borrowings
   - Borrow a book.
   - Return a book.
   - Prevent borrowing unavailable books.
   - Calculate due date.
   - Track borrowing history.

6. Payments
   - Record payment amount.
   - Store payment date, method, status, and transaction reference.

7. Authentication and Roles
   - Optional bonus unless required by the instructor.
   - Suggested roles: Admin, Manager, Member.

## Suggested Team Split

### Teammate A: Catalog and Admin

- Book model, controller, and views.
- Author model, controller, and views.
- Category model, controller, and views.
- Book search and filters.

### Teammate B: Users and Transactions

- Role and User models.
- Member screens.
- Borrowing model, controller, and views.
- Payment model, controller, and views.
- Borrow and return business rules.

### Shared Work

- Database context and relationships.
- Migration testing.
- Layout/navbar.
- Final testing and presentation/demo data.

## Build Order

1. Create MVC project.
2. Add EF Core and SQL Server packages.
3. Create models from the ERD.
4. Create `ApplicationDbContext`.
5. Configure relationships.
6. Add connection string.
7. Create initial migration.
8. Build CRUD for categories and authors.
9. Build books CRUD with search/filter.
10. Build members.
11. Build borrowing and return flow.
12. Build payments.
13. Add authentication/roles if needed.
14. Polish UI and test full workflows.

## First Milestone

The first milestone is database readiness:

- All models exist.
- `ApplicationDbContext` exists.
- SQL Server connection string is configured.
- Initial migration runs successfully.

