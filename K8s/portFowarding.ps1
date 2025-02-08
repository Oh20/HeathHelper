# Definir os serviços e as portas a serem mapeadas
$services = @(
    @{ name = "appointment-scheduler-service"; localPort = 8001; servicePort = 80 },
    @{ name = "auth-service"; localPort = 8002; servicePort = 80 },
    @{ name = "user-register-producer-service"; localPort = 8003; servicePort = 80 },
    @{ name = "scheduler-consumer-service"; localPort = 8004; servicePort = 80 },
    @{ name = "user-register-consumer-service"; localPort = 8005; servicePort = 80 },
    @{ name = "rabbitmq-service"; localPort = 5672; servicePort = 5672 },
    @{ name = "rabbitmq-service"; localPort = 15672; servicePort = 15672 }
)

# Armazenar processos em uma lista
$processes = @()

# Iniciar o redirecionamento para cada serviço
foreach ($service in $services) {
    $command = "port-forward svc/$($service.name) $($service.localPort):$($service.servicePort)"
    Write-Host "Iniciando redirecionamento para $($service.name) em http://localhost:$($service.localPort)..."
    $process = Start-Process kubectl -ArgumentList $command -NoNewWindow -PassThru
    $processes += $process
}

# Mostrar informações dos processos
Write-Host "`nTodos os redirecionamentos foram iniciados:"
Write-Host "Serviços Externos:"
Write-Host "- Appointment Scheduler: http://localhost:8001"
Write-Host "- Auth Service: http://localhost:8002"
Write-Host "- User Register Producer: http://localhost:8003"
Write-Host "`nServiços Internos (Debug):"
Write-Host "- Scheduler Consumer: http://localhost:8004"
Write-Host "- User Register Consumer: http://localhost:8005"
Write-Host "`nRabbitMQ:"
Write-Host "- AMQP: localhost:5672"
Write-Host "- Management UI: http://localhost:15672"

# Aguardar comando do usuário para encerrar
Write-Host "`nPressione qualquer tecla para parar todos os redirecionamentos..."
[System.Console]::ReadKey() > $null

# Parar todos os processos
Write-Host "`nEncerrando todos os redirecionamentos..."
foreach ($process in $processes) {
    Stop-Process -Id $process.Id -Force
    Write-Host "- Processo $($process.Id) encerrado."
}

Write-Host "Todos os redirecionamentos foram encerrados."