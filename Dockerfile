# prepare python env
FROM python:slim as python
# create a venv and install dependencies, use pip/poetry etc.
RUN python -m venv /venv
COPY requirements.txt .
# make sure you use the pip inside the venv
RUN /venv/bin/python -m pip install -r requirements.txt

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
ENV PATH="/python/bin:$PATH"
WORKDIR /birdbot
COPY --from=build-env /birdbot/out .
# copy the python environment we've prepared
COPY --from=python /venv /python
ENTRYPOINT ["dotnet", "BirdBot.dll"]