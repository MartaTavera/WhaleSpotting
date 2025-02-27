FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /WhaleSpotting

# Copy everything first in folder
COPY . .

# Navigate to backend and build
WORKDIR /WhaleSpotting/backend
RUN dotnet restore
RUN dotnet publish -c Release -o /WhaleSpotting/out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /WhaleSpotting/out ./

# Add the secrets file mounting point
RUN mkdir -p /etc/secrets

EXPOSE 80
ENTRYPOINT ["dotnet", "WhaleSpotting.dll"]