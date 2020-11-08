FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 
COPY . /publish
WORKDIR /publish
ENV ASPNETCORE_URLS=http://*:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "MysqlHangfire.dll"]
# server_port@80@  
# volume@/opt/hangfire/logs:/publish/Logs@  