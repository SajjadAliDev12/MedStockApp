# MedStock Pro

A comprehensive Hospital Inventory Management System. Designed to streamline the tracking of medical supplies, batches, expiry dates, and requisitions, along with a comprehensive reporting system.

---

## 📸 Screenshots

*(Replace the links below with the actual image paths after uploading them)*

| Main Dashboard | Items Management |
| :---: | :---: |
| ![Dashboard](https://github.com/user-attachments/assets/04ad6486-14da-4309-b0e8-9c6f6a5c9efc) | ![Items](https://github.com/user-attachments/assets/0d685980-c2f4-4a85-a142-7b451f35d9a6) |
| **Consumption Report** | **Categories** |
| ![Report](https://github.com/user-attachments/assets/858bc8ba-63b8-4dd5-b827-570843d542fb) | ![Categories](https://github.com/user-attachments/assets/025e25af-4af0-4e22-8766-63f4fc86b659) |

---

## 🚀 Key Features

* **Items & Batches Management:**
  * Define items with SKUs and units of measure.
  * Manage batches and track expiry dates.
  * Min stock and reorder level alerts.
* **Transactions Management:**
  * Stock-In and Stock-Out operations.
  * Requisitions system for different departments.
  * Stocktakes system to match actual stock with the system.
* **Reporting System:**
  * Stock Card report to track specific item transactions.
  * Aggregated Consumption Report with Excel/CSV export capabilities.
  * Expired or near-expiry items report.
* **High Performance:**
  * Pagination for large tables to ensure fast response times.
  * Optimized queries using Entity Framework Core.
* **Security & Auditing:**
  * Audit logs to track changes and who made them.
  * Support for SQL Server and Windows Authentication.
* **Dynamic Setup:**
  * Built-in interface for initial database setup and server connection upon first launch, without the need to manually edit configuration files.

---

## 🛠️ Tech Stack

* **Programming Language:** C#
* **Framework:** .NET (WPF) for graphical user interfaces.
* **Architectural Pattern:** MVVM (Model-View-ViewModel) with Dependency Injection.
* **Database:** Microsoft SQL Server.
* **ORM (Data Access):** Entity Framework Core.
* **Project Structure:** N-Tier Architecture (Data, Services, UI, DTOs).

---

## ⚙️ Prerequisites

1. [Visual Studio 2022](https://visualstudio.microsoft.com/) or newer.
2. [.NET SDK](https://dotnet.microsoft.com/download) (The version used in the project).
3. [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express or Developer).

---

## 📥 Installation & Setup

1. **Clone the repository:**
   ```bash
   git clone [https://github.com/YourUsername/MedStock.git](https://github.com/YourUsername/MedStock.git)
   ```

2. **Open the project:**
   * Open the `MedStock.sln` file using Visual Studio.

3. **Run the application:**
   * Set the `MedStock.UI` project as the Startup Project.
   * Press `F5` to run.

4. **Database Configuration:**
   * Upon the first launch, the application will detect the absence of a database connection.
   * The initial Database Setup View will appear.
   * Enter the server name (e.g., `.`), and choose the authentication type (Windows or SQL Auth).
   * Click "Save and Run". The system will automatically create the `appsettings.json` file in the `AppData` folder.

---

## 📂 Project Structure

```text
MedStock/
├── MedStock.Data/        # Database Context (DbContext) and Entities
├── MedStock.Services/    # Business Logic and DTOs
├── MedStock.UI/          # User Interfaces (WPF), ViewModels, and DI Setup
└── MedStock.sln          # Solution File
```
