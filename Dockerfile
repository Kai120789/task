# Используем официальный образ .NET SDK как базовый образ
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

# Устанавливаем рабочую директорию
WORKDIR /app

# Копируем csproj файл и восстанавливаем зависимости

COPY ["Task..sln", "./"]
COPY ["Task.Connector/Task.Connector.csproj", "Task.Connector/"]
COPY ["Task.Connector.Tests/Task.Connector.Tests.csproj", "Task.Connector.Tests/"]
RUN dotnet restore

# Копируем все файлы и строим проект
COPY . ./
RUN dotnet publish -c Release -o /app/out

# Копируем утилиту миграций
COPY DbCreationUtility/Task.Integration.Data.DbCreationUtility.exe ./DbCreationUtility.exe

# Устанавливаем права на выполнение утилиты миграций
RUN chmod +x ./DbCreationUtility.exe

# Используем официальный образ .NET Runtime как базовый образ для запуска
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime

# Устанавливаем рабочую директорию
WORKDIR /app

# Копируем собранное приложение из предыдущего этапа
COPY --from=build /app/out .

# Запускаем приложение
ENTRYPOINT ["dotnet", "Task.Connector.dll"]
