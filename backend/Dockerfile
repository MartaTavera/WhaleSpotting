FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /WhaleSpotting

# Copy csproj and restore as distinct layers
COPY ./backend/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /WhaleSpotting/backend
COPY --from=build /WhaleSpotting/out ./

# Add the secrets file mounting point
RUN mkdir -p /etc/secrets

EXPOSE 80
ENTRYPOINT ["dotnet", "WhaleSpotting.dll"]