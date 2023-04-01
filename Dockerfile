FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
COPY ai/* /ai/
WORKDIR /ai/
RUN wget https://huggingface.co/Pi3141/alpaca-7B-ggml/resolve/main/ggml-model-q4_0.bin\
    && mv ggml-model-q4_0.bin ggml-alpaca-7b-q4.bin
WORKDIR /birdbot

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
COPY --from=build-env /ai /ai
WORKDIR /birdbot
COPY --from=build-env /birdbot/out .
ENTRYPOINT ["dotnet", "BirdBot.dll"]