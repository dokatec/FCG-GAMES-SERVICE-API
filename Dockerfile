FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# 1. Declara o argumento que receberá o token
ARG GH_TOKEN

# 2. Copia o nuget.config
COPY ["nuget.config", "./"]

# 3. Substitui o placeholder no nuget.config pelo token real antes do restore
# Isso garante que o dotnet restore tenha a senha correta
RUN sed -i "s/%GH_TOKEN%/$GH_TOKEN/g" nuget.config

# 4. Copia os arquivos de projeto
COPY ["src/FCG.Games.API/FCG.Games.API.csproj", "src/FCG.Games.API/"]
COPY ["src/FCG.Games.Domain/FCG.Games.Domain.csproj", "src/FCG.Games.Domain/"]
COPY ["src/FCG.Games.Application/FCG.Games.Application.csproj", "src/FCG.Games.Application/"]
COPY ["src/FCG.Games.Infrastructure/FCG.Games.Infrastructure.csproj", "src/FCG.Games.Infrastructure/"]

# 5. Agora o restore terá permissão para baixar a FCG.Shared
RUN dotnet restore "src/FCG.Games.API/FCG.Games.API.csproj"

# 6. Copiar o restante do código fonte e compilar
COPY . .
WORKDIR "/app/src/FCG.Games.API"
RUN dotnet build "FCG.Games.API.csproj" -c Release -o /app/build

# Estágio 2: Publicação
FROM build AS publish
RUN dotnet publish "FCG.Games.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio 3: Runtime (Imagem final leve)
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "FCG.Games.API.dll"]