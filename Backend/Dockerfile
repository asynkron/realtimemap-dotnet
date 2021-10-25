# Stage 1 - Build
FROM mcr.microsoft.com/dotnet/sdk:5.0.402 AS builder

WORKDIR /app/src

# Restore
#COPY ./*.sln ./src/
COPY *.csproj .
# RUN for file in $(ls src/*.csproj); do mkdir -p ./${file%.*}/ && mv $file ./${file%.*}/; done
RUN dotnet restore -r linux-x64

# Build
COPY . .
RUN dotnet publish -c Release -o /app/publish -r linux-x64 --no-self-contained --no-restore -p:PublishReadyToRun=true -p:CrossGenDuringPublish=false

# Stage 2 - Publish
FROM mcr.microsoft.com/dotnet/aspnet:5.0.11
WORKDIR /app
COPY --from=builder /app/publish .

ENTRYPOINT ["./Backend"]