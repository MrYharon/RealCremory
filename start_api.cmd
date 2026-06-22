@echo off
set DATABASE_URL=Host=cremory-db.postgres.database.azure.com; Database=cremory; Username=cremory; Password=Gelo123!; SSL Mode=Require; Trust Server Certificate=true
set PORT=5105
set ASPNETCORE_ENVIRONMENT=Development
cd /d "C:\Users\hughd\source\repos\Cremory"
dotnet run --project Cremory.API
