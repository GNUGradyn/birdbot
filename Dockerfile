FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
COPY ai/* /ai/
WORKDIR /birdbot

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY --from=build-env /ai /ai
WORKDIR /birdbot
COPY --from=build-env /birdbot/out .
ENTRYPOINT ["dotnet", "BirdBot.dll"]