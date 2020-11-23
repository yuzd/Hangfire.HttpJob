## HttpAgent Project Template

### Push to Nuget

* dotnet pack
* modified Template.csproj
* push to nuget.org


### Quick Start

**install**
```
dotnet new -i HttpJob.Agent.Template
```

**usage**
```
## --type default value is RedisConsole
dotnet new agent -n=Hello --type=MysqlConsole
```