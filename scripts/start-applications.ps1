#Start-Process -FilePath "C:\Program Files\Application1\App1.exe"
#Start-Process -FilePath "C:\Program Files\Application2\App2.exe"
#Start-Process -FilePath "C:\Program Files\Application3\App3.exe"

Push-Location ".."
Push-Location "src"

Push-Location "Service"
Start-Process -FilePath "dotnet" -ArgumentList "run --project Service.csproj -c Release"
Pop-Location

Push-Location "Consumer"
Start-Process -FilePath "dotnet" -ArgumentList "run --project Consumer.csproj -c Release"
Pop-Location

Push-Location "Producer"
Start-Process -FilePath "dotnet" -ArgumentList "run --project Producer.csproj -c Release"
Pop-Location


Pop-Location
Pop-Location