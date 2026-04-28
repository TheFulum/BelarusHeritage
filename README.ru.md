# BelarusHeritage

[English version](README.md)

Веб-приложение о культурном наследии Беларуси: каталог объектов, карта, таймлайн, квизы и расширенный конструктор маршрутов.

## Технологии

- ASP.NET Core MVC (`.NET 8`)
- Entity Framework Core + Pomelo MySQL provider
- ASP.NET Core Identity + JWT (токен через cookie)
- Leaflet + MapLibre для карт
- OSRM (маршруты по дорогам и альтернативы), Nominatim (геокодинг и обратный геокодинг)

## Основные возможности

- Каталог объектов наследия с мультиязычным контентом (`ru`, `be`, `en`)
- Интерактивная карта и таймлайн
- Модуль квизов
- Админ-зона для управления контентом
- Единая UI-локализация через `Localization/UiText.cs` (`UiText.Lang/Is/T`)
- Переключение языка в `AdminDashboard` (`ru` / `be` / `en`)
- Конструктор маршрутов:
  - приватные точки старта/финиша
  - построение маршрута по дорогам
  - выбор альтернатив по каждому участку с визуальным выделением
  - оптимизация маршрута
  - сохранение персональной приватной копии публичного маршрута
- Карта (`/Map`) использует тот же фундамент, что и `RouteBuilder/Share`:
  - Leaflet + локализованный векторный стиль MapLibre
  - fallback на OSM raster tiles

## Требования

- .NET SDK 8
- MySQL 8+ (или совместимая версия)

## Быстрый запуск

1. Клонируйте репозиторий
2. Настройте подключение к базе данных в `appsettings.json` (или через переменные окружения)
3. Восстановите зависимости и запустите:

```bash
dotnet restore
dotnet run
```

Приложение запустится на локальных адресах ASP.NET Core (например, `https://localhost:xxxx` / `http://localhost:xxxx`).

## База данных

- Конфигурация EF Core находится в `Program.cs` через `UseMySql(...)`.
- Начальная SQL-схема: `Data/Migrations/001_InitialSchema.sql`.
- Сидирование и идемпотентные проверки схемы при старте: `Data/DbSeeder.cs`.

## Конфигурация

- `appsettings.json` содержит основные настройки приложения (сайт, JWT, почта и т.д.).
- Для локальных секретов используйте отдельные неотслеживаемые файлы:
  - `appsettings.Development.local.json`
  - `appsettings.Local.json`

Эти файлы добавлены в `.gitignore`.

## Локализация

- UI-строки централизованы в `Localization/UiText.cs` и вызываются во views через `UiText.T(Context, "key")`.
- Язык берётся из cookie `culture` и нормализуется к `ru`, `be` или `en`.
- Контентные переводы остаются в полях доменных моделей (`*_ru`, `*_be`, `*_en`).

## Карты и маршруты

- Базовая карта (`/Map`, `/RouteBuilder`, `/RouteBuilder/Share`): стиль MapLibre (с fallback на OSM raster tiles)
- Построение маршрутов и альтернатив: `/RouteBuilder/BuildRoadPolyline` и `/RouteBuilder/BuildRoadAlternatives` (proxy-эндпоинты)
- Геокодинг: Nominatim
