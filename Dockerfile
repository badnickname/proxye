FROM mcr.microsoft.com/dotnet/aspnet:8.0.18-alpine3.21-arm32v7 as runtime
WORKDIR /build
COPY build/ .
ENTRYPOINT ["dotnet", "Proxye.Application.dll"]