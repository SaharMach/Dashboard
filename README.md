# Dashboard Project


### 1. Clone the repository

```bash
git clone https://github.com/SaharMach/Dashboard.git
cd Dashboard
```

### 2. Configuration
Copy the example config files and add your private values (sent separately):

```bash
copy appsettings.example.json appsettings.json
copy .env.example .env
```

- **appsettings.json** → add your SQL connection string (LocalDB) + SMTP details  
- **.env** → add your `OPENAI_API_KEY`

---

### 3. Database Setup
This project uses **Entity Framework Core with LocalDB**.

1. Make sure **LocalDB** is installed (it comes with Visual Studio).  

2. Run the migrations to create the database:
```bash
dotnet tool install --global dotnet-ef
dotnet ef database update --project Dashboard/Dashboard.csproj
```

This will create a database named **UsersMgmt** with the required tables.  
⚠️ **No manual SQL queries are needed** — EF Core handles schema creation automatically.

---

### 4. Run the project
```bash
dotnet run --project Dashboard/Dashboard.csproj
```

Or open the `.sln` in Visual Studio and press **F5**.

---


