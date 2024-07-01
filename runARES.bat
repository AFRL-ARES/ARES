cd C:/ARES/FC2/UI
start dotnet run --no-build

cd ..
cd FC2Service
start dotnet run --no-build

timeout /t 10 /nobreak
start https://localhost:7084