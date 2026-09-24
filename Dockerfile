FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY TicketApi.sln ./
COPY src/TicketApi.Domain/TicketApi.Domain.csproj src/TicketApi.Domain/
COPY src/TicketApi.Application/TicketApi.Application.csproj src/TicketApi.Application/
COPY src/TicketApi.Infrastructure/TicketApi.Infrastructure.csproj src/TicketApi.Infrastructure/
COPY src/TicketApi.Api/TicketApi.Api.csproj src/TicketApi.Api/
RUN dotnet restore src/TicketApi.Api/TicketApi.Api.csproj
COPY src/ ./src/
RUN dotnet publish src/TicketApi.Api/TicketApi.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TicketApi.Api.dll"]
