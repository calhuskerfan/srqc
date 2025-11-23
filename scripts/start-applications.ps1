Push-Location ".."
Push-Location "src"

Push-Location "Processor"
Start-Process -FilePath "dotnet" -ArgumentList "run --project Processor.csproj -c Release"
Pop-Location

Push-Location "Consumer"
Start-Process -FilePath "dotnet" -ArgumentList "run --project Consumer.csproj -c Release"
Pop-Location

Push-Location "Producer"
Start-Process -FilePath "dotnet" -ArgumentList "run --project Producer.csproj -c Release"
Pop-Location


Pop-Location
Pop-Location