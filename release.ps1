dotnet restore

dotnet build --no-restore --configuration Release

dotnet test --no-restore --configuration Release --no-build --verbosity normal

dotnet pack --no-restore --configuration Release --no-build --output ./pack-output

