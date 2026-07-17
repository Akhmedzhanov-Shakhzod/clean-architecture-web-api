# Clean Architecture Web API Template (.NET 8)

Базовый шаблон Web API: чистая архитектура, JWT-авторизация с refresh-токеном в HttpOnly cookie, ASP.NET Identity, PostgreSQL, Serilog, Docker.

## Структура

```
src/
  CleanArchitecture.Domain          — сущности, константы. Без зависимостей от других слоёв.
  CleanArchitecture.Application     — интерфейсы, DTO, валидация (FluentValidation), исключения.
  CleanArchitecture.Infrastructure  — EF Core (PostgreSQL), Identity, JWT, реализация сервисов, seed.
  CleanArchitecture.WebApi          — контроллеры, middleware, Swagger, Serilog, DI-композиция.
```

Зависимости направлены внутрь: `WebApi → Infrastructure → Application → Domain`.

## Аутентификация

- **Access token** — короткоживущий JWT (15 мин по умолчанию), возвращается в теле ответа, клиент передаёт его в заголовке `Authorization: Bearer`.
- **Refresh token** — 64 байта криптослучайности, живёт только в **HttpOnly cookie** (`Path=/api/v1/auth`, `Secure`, `SameSite=Strict`). В БД хранится **только SHA-256 хеш**.
- **Ротация**: каждый вызов `/api/v1/auth/refresh` отзывает старый токен и выдаёт новый. Повторное использование отозванного токена расценивается как кража — отзывается вся цепочка потомков.
- Смена пароля отзывает все refresh-токены пользователя.
- Lockout: 5 неудачных попыток входа → блокировка на 15 минут (ASP.NET Identity).

### Эндпоинты

| Метод | Путь | Описание |
|---|---|---|
| POST | `/api/v1/auth/register` | Регистрация + вход |
| POST | `/api/v1/auth/login` | Вход, ставит refresh-cookie |
| POST | `/api/v1/auth/refresh` | Обмен cookie на новый access token (ротация) |
| POST | `/api/v1/auth/logout` | Отзыв токена, удаление cookie |
| POST | `/api/v1/auth/change-password` | Смена пароля (авторизован) |
| GET | `/api/v1/auth/me` | Текущий пользователь |
| GET | `/api/v1/users` | Список пользователей (роль Admin) |
| GET | `/health` | Health check (включая БД) |

## Быстрый старт

Требуется запущенный PostgreSQL на хост-машине (`localhost:5432`, база `clean_architecture`).

### Вариант 1: Docker

```bash
docker compose up --build
```

API: http://localhost:5000/swagger. Вся конфигурация читается из `appsettings.json` / `appsettings.Development.json` — compose ничего не переопределяет. Если API в контейнере, а PostgreSQL на хосте, поменяйте в connection string `Host=localhost` на `Host=host.docker.internal` (маппинг уже прописан в compose). Миграции применяются автоматически, сидится админ `admin@example.com` / `Admin123!`.

### Вариант 2: локально

1. Создать первую миграцию (однократно):
   ```bash
   dotnet tool install -g dotnet-ef
   dotnet ef migrations add InitialCreate -p src/CleanArchitecture.Infrastructure -s src/CleanArchitecture.WebApi
   ```
2. Запустить:
   ```bash
   dotnet run --project src/CleanArchitecture.WebApi
   ```
   В Development миграции применяются на старте (`Database:RunMigrationsOnStartup: true`).

Swagger: http://localhost:5000/swagger. Примеры запросов — в `src/CleanArchitecture.WebApi/CleanArchitecture.WebApi.http`.

## Конфигурация

Ключевые секции `appsettings.json`:

- `JwtSettings` — `Secret` (мин. 32 символа), `Issuer`, `Audience`, время жизни токенов. Валидируется при старте — приложение не поднимется без секрета.
- `RefreshTokenCookie` — имя, путь, `SameSite`. Если SPA живёт на другом домене, поставьте `SameSite: "None"` и `Secure: true`.
- `AdminSeed` — учётка админа, сидится при старте, если задан пароль.
- `Cors:AllowedOrigins` — origin'ы фронтенда (`AllowCredentials` включён — нужен для cookie).
- `Database:RunMigrationsOnStartup` — в production обычно `false` (миграции через CI/CD).

**Секреты не хранить в git.** Слои конфигурации (каждый следующий переопределяет предыдущий):

1. `appsettings.json` — структура и безопасные дефолты (в git).
2. `appsettings.Development.json` — общие dev-настройки без секретов: логирование, cookie без Secure и т.п. (в git).
3. `appsettings.Local.json` — секреты и настройки конкретной машины (в `.gitignore`). Скопируйте из `appsettings.Local.json.example` и заполните.
4. Переменные окружения — production: `JwtSettings__Secret`, `ConnectionStrings__DefaultConnection`, `AdminSeed__Password`.

## Как расширять шаблон

1. **Новая сущность**: класс в `Domain/Entities` (наследуйте `BaseAuditableEntity` — поля аудита заполняются автоматически), `DbSet` в `ApplicationDbContext` + `IApplicationDbContext`, конфигурация в `Infrastructure/Persistence/Configurations`, миграция.
2. **Новая фича**: интерфейс + DTO + валидаторы в `Application/Features/<Имя>`, реализация в `Infrastructure/Services`, регистрация в `Infrastructure/DependencyInjection.cs`, контроллер в `WebApi/Controllers` (наследуйте `ApiControllerBase`).
3. **Ошибки**: бросайте исключения из `Application.Common.Exceptions` (`NotFoundException`, `BadRequestException`, ...) — `GlobalExceptionHandler` сам превратит их в ProblemDetails с нужным статусом.
4. **Роли**: добавьте константу в `Domain/Constants/Roles.cs` — она засеется при старте.

## Пример работы с фронтендом

```js
// login
await fetch('/api/v1/auth/login', {
  method: 'POST',
  credentials: 'include',              // важно: иначе cookie не сохранится
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email, password })
});

// refresh по истечении access-токена (обычно в interceptor на 401)
const { accessToken } = await (await fetch('/api/v1/auth/refresh', {
  method: 'POST',
  credentials: 'include'
})).json();
```
