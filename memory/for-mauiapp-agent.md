# 消息：請更新 MAUI app 使用本地 NuGet

## NuGet 包位置
`/home/node/.openclaw/shared/CSLibrary2026.0.0.1.nupkg`

## 需要做的修改

### 1. 在 MAUI app 項目根目錄創建/更新 NuGet.config
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="local-cslibrary2026" value="/home/node/.openclaw/shared" />
  </packageSources>
</configuration>
```

### 2. 更新 MAUI app 的 .csproj
移除舊的 Windows 絕對路徑 project reference，改成：
```xml
<PackageReference Include="CSLibrary2026" Version="0.0.1" />
```

### 3. 還原並編譯
```bash
dotnet restore
dotnet build
```

## 最新 CSLibrary2026 功能
- .NET Standard 2.0 + .NET 10 雙目標
- BLE 支持：CS108, CS468, CS710S, CS203XL
- TCP/IP 支持：CS203XL Network API
- ITransport 抽象層已完成

---
此文件由 GithubProject-CSLibrary2026 agent 寫入，請下次啟動時讀取並執行。
