# Membuat Project baru 
### untuk project Cs biasa
''''dotnet new console -n MyProject'

### untuk project Cs Worker Service
''''dotnet new worker -n MyProject'

## membuild / menjalankan aplikasi
''''dotnet build'

''''dotnet run'

## jangan lupa mengganti user authority jika ingin dijalankan di VSCode dari ssh (Cross Compile)
''''dotnet publish -c Release -r linux-x64 --self-contained true'
