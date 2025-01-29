#dotnet restore
#dotnet build --no-restore --configuration Release   
#dotnet test --no-restore --configuration Release --no-build --verbosity normal
#dotnet pack --no-restore --configuration Release --no-build --output ./pack-output
#foreach($file in (Get-ChildItem "${{ env.RAPTOR_PACK_OUTPUT }}" -Recurse -Include *.nupkg)) {
#  dotnet nuget push $file --api-key "${{ secrets.GITHUB_TOKEN }}" --source https://nuget.pkg.github.com/peddinghaus/index.json --skip-duplicate
#}