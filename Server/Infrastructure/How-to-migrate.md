# Liebherr CIM DB

1. Install dotnet-ef

   ```csharp
      dotnet tool install --global dotnet-ef
   ```

2. In _Visual Studio_ got to _Tools_ > _Nuget Package Manager_ > _Package Manager Console_

3. Execute the following command. Choose a name for the migration (change _name_of_the_migration_ to a meaningful name).

   ```powershell
   dotnet ef migrations add _name_of_the_migration_ --project ./UseCaseApp.Infrastructure --startup-project ./UseCaseApp.Api
   ```

Note: If you see a tools/runtime version mismatch like "tools version '8.0.0' is older than that of the runtime '8.0.7'", update the global dotnet-ef tool to match the runtime, for example:

```powershell
dotnet tool update --global dotnet-ef --version 8.0.7
```

<br/>

---

> If you get an **exception** like this:

```csharp
Migration environment is empty. Set the environment first.
In 'Package Manager Console' execute the command:
    $env:DATABASE_MIGRATION_ENVIRONMENT='your environment here'.
Example:
    $env:DATABASE_MIGRATION_ENVIRONMENT='Development'
Caution: appsettings.your_environment_here.json should be exists and contains the connection-string to the database.
```

follow the instructions defined in the exception and specify the environment for the migration.

---

<br/>

4. Now you should have a _**Migrations**_ folder inside the project.

5. Update the database:

   ```csharp
    dotnet ef database update --project .\UseCaseApp.Infrastrucutre
   ```
