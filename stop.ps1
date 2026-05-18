wsl docker compose down
  wsl pkill -f "dotnet" 2>$null
  $proc = (Get-NetTCPConnection -LocalPort 3000 -ErrorAction SilentlyContinue).OwningProcess
  if ($proc) {
      $process = Get-Process -Id $proc -ErrorAction SilentlyContinue
      if ($process -and $process.Name -eq "node") { Stop-Process -Id $proc -Force }
  }