param (
    [string] $containerName = "rabbitmq-srqc",
    [int] $launchBrowser = 1
)

$isRunning = $null -ne (docker ps --filter "name=$containerName" --format "{{.ID}}")

if ($isRunning) {
    Write-Host "Container '$containerName' is running."
}
else {

    $isRegistered = $null -ne (docker ps --filter "name=$containerName" --filter "status=exited" --format "{{.ID}}")
    
    if ($isRegistered) {
        docker start $containerName
    }
    else {
        docker run `
            --name $containerName `
            -p 5672:5672 `
            -p 15672:15672 `
            -d `
            rabbitmq:4-management    
    }
}

if($launchBrowser) {
    Start-Process "http://localhost:15672"
    Write-Host "Launched RabbitMQ Management UI in browser."
}

