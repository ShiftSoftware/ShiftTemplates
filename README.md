## Intro

The ShiftTemplate repository contains a project template and an item template. The project that's used as template is also used as a sample project during framework development.

```
ShiftTemplates
 |-- content
 |   |-- Framework Project
 |       |-- StockPlusPlus.sln      <--- (Use this for Sample Porject & Framework Component Development)
 |   |-- ShiftEntity
 |-- ShiftTemplates.sln             <--- (Use this for Template Development)
```

**ShiftTemplates.sln** is used for the following:

- Project Template Development
- Item Template Development


**StockPlusPlus.sln** is used for the following:

- Development of the Sample Project (That's used as the Project Template)
- Development of framework components.

If all the relevant framework package repositories are cloned with the correct folder structure, the sample project automatically uses the local project references. Giving you the ability to modify the framework components and see the changes applied on the sample project.

## Running the Sample Project

- Open the sample project `ShiftTemplates/content/Framework Project/StockPlusPlus.sln`
- Build the solution
- Change the SQL Server Connection string if needed, located in StockPlusPlus.API/appsettings.Development.json
- Apply the EF Core migrations by running the following in the Package Manager Console

```
Update-Database -Project StockPlusPlus.Data -StartupProject StockPlusPlus.API
```


