build:
	dotnet build
clean:
	dotnet clean
restore:
	dotnet restore
watch:
	dotnet watch --project MVC/MVC.csproj
run:
	dotnet run --project MVC/MVC.csproj
test:
	dotnet test
migrate:
	@echo "Enter migration name:"
	@read name; \
		echo "Creating migration with name: $$name"; \
		cd Infrastructure; \
		dotnet ef migrations add $$name --startup-project ../MVC/MVC.csproj;
update:
	@echo "Enter migration name:"
	@read name; \
		echo "Updating database to migration: $$name"; \
		cd Infrastructure; \
		dotnet ef database update $$name --startup-project ../MVC/MVC.csproj