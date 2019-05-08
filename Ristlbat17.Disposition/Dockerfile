FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app

# Because EPPlus uses System.Drawing therefore these native dependencies are needed
# see https://github.com/dotnet/dotnet-docker/issues/618
RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
        libc6-dev \
        libgdiplus \
        libx11-dev \
     && rm -rf /var/lib/apt/lists/*

FROM microsoft/dotnet:2.2-sdk AS build
# Setup NodeJs
RUN apt-get update && \
    apt-get install -y wget && \
    apt-get install -y gnupg2 && \
    wget -qO- https://deb.nodesource.com/setup_6.x | bash - && \
    apt-get install -y build-essential nodejs
# End setup
WORKDIR /src
COPY ["Ristlbat17.Disposition.csproj", "."]
RUN dotnet restore "Ristlbat17.Disposition.csproj"
COPY . .
RUN dotnet build "Ristlbat17.Disposition.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Ristlbat17.Disposition.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Ristlbat17.Disposition.dll"]