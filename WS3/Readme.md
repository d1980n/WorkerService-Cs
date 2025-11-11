## Detail Program:
* Worker.cs
  
input = {"nilai":1}

## Masalah yang timbul (server Linux) -- api.nuget.org (htpps)
### 1. Update package list
sudo apt update

### 2. Install the necessary package for certificate management
### (This is often not installed in minimal Linux base images)
sudo apt install -y ca-certificates

### 3. Force the system to update the certificate links
sudo update-ca-certificates

### 4. Clean and restore NuGet packages
dotnet nuget locals all --clear

dotnet restore

cek header

curl -I https://google.com

PS C:\Users\33222> curl -I https://api.nuget.org/v3/index.json

dotnet clean

dotnet restore

dotnet build
