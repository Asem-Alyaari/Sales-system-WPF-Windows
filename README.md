# Sales System (WPF Windows)

A comprehensive desktop application built with C# and WPF (Windows Presentation Foundation) for managing sales, purchases, inventory, and accounting.

## Features

- **Dashboard**: Overview of system statistics.
- **Inventory Management**: Track products, stock levels, and item details.
- **Sales Invoices**: Create and manage sales to customers.
- **Purchase Invoices**: Record purchases from suppliers and update inventory.
- **Customer Management**: Maintain customer records and details.
- **Accounting System**: Chart of accounts, financial transactions, and logging.

## Technologies Used

- C# .NET
- WPF (Windows Presentation Foundation)
- Entity Framework Core
- MVVM (Model-View-ViewModel) Architecture

## Getting Started

1. Clone the repository.
2. Open the solution (`App2.sln`) in Visual Studio.
3. Build the solution to restore NuGet packages.
4. Update the database using Entity Framework Core tools (`Update-Database` in Package Manager Console).
5. Run the application.

## Project Structure

- `Models/`: Entity classes representing the database schema.
- `ViewModels/`: Presentation logic and state for the views.
- `Views/`: XAML files defining the user interface.
- `Data/`: Database context and entity configurations.
- `Migrations/`: Entity Framework Core database migrations.
