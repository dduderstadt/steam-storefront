wsl docker compose up postgres redis -d --wait
Start-Process powershell -ArgumentList "-NoExit", "-Command", "wsl --cd /mnt/c/Users/derek/repos/steam-storefront/backend/SteamStorefront dotnet run"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd C:\Users\derek\repos\steam-storefront\frontend; npm run dev"