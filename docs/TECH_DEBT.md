# Learnix — Технічний борг

> Речі, які працюють, але реалізовані не оптимально. Кожен запис описує поточний стан, чому це проблема, і конкретний план виправлення.
>
> Пріоритети: `high` · `medium` · `low`

---

## TD-001: Blob Storage — SAS URL для публічних зображень

**Пріоритет:** `medium`

### Як це влаштовано в реальних проектах

Azure Blob Storage має два режими доступу до blob-ів:

**Публічний контейнер** — blob доступний за прямим URL без будь-якої автентифікації:
```
https://account.blob.core.windows.net/avatars/abc123.jpg
```
URL стабільний, не протухає, ідеально кешується браузером і CDN. Використовується для контенту, який і так видно всім (аватари, обкладинки курсів, зображення категорій).

**SAS URL (Shared Access Signature)** — тимчасове підписане посилання з TTL і дозволами:
```
https://account.blob.core.windows.net/avatars/abc123.jpg?sv=2021-06-08&se=2026-05-30T10%3A00%3A00Z&sig=...
```
URL протухає, містить підпис. Підходить для захищеного контенту — платні відео, сертифікати (тільки власник має бачити).

У виробничих проектах (Coursera, Udemy, GitHub тощо) аватари і обкладинки курсів роздаються через CDN з публічних контейнерів. SAS URL використовують тільки там, де потрібен контроль доступу.

### Як зараз у Learnix

Усі контейнери приватні. Для будь-якого зображення (навіть публічного аватара) викликається `GenerateReadUrl` з TTL 24 години:

```csharp
// PublicCourseCatalogSearchService.cs, GetMyProfileQueryHandler.cs, etc.
blobStorage.GenerateReadUrl(c.CoverBlobPath, TimeSpan.FromHours(24))
```

### Чому це проблема

1. **URL протухає.** TanStack Query кешує відповідь API. Якщо кеш "свіжий" довше 24 годин — аватар або обкладинка зламається без перезавантаження.
2. **Зайва робота на кожен запит.** `GenerateReadUrl` виконує HMAC-підпис кожного разу, коли будується DTO. Для каталогу курсів (20+ карток) це 20+ підписів.
3. **CDN не може кешувати.** SAS URL містить підпис і timestamp — CDN вважає кожен URL унікальним.
4. **Концептуально неправильно.** Аватар студента не є захищеним контентом. Будь-хто може побачити його на сторінці курсу.

### Що потрібно зробити

#### Бекенд

**1. Розділити контейнери на публічні та приватні в `BlobStorageBootstrapper`:**

```csharp
private static readonly HashSet<string> PublicContainers =
[
    "avatars", "course-covers", "category-images"
];

foreach (var name in containers)
{
    var container = blobServiceClient.GetBlobContainerClient(name);
    var accessType = PublicContainers.Contains(name)
        ? PublicAccessType.Blob
        : PublicAccessType.None;
    await container.CreateIfNotExistsAsync(accessType, cancellationToken: ct);
}
```

**2. Додати `GetPublicUrl` в `IBlobStorageService`:**

```csharp
/// <summary>
/// Returns a stable public URL for blobs in public containers (avatars, covers, category images).
/// Do NOT use for protected content (videos, certificates).
/// </summary>
string GetPublicUrl(string blobPath);
```

**3. Реалізація в `AzureBlobStorageService`:**

```csharp
public string GetPublicUrl(string blobPath)
{
    var (container, blobName) = ParseBlobPath(blobPath);
    return blobServiceClient
        .GetBlobContainerClient(container)
        .GetBlobClient(blobName)
        .Uri.ToString();
}
```

**4. Замінити `GenerateReadUrl` → `GetPublicUrl` у хендлерах для публічних зображень:**

| Місце | Поле | Метод після виправлення |
|---|---|---|
| `GetMyProfileQueryHandler` | `AvatarUrl` | `GetPublicUrl` |
| `GetUserProfileQueryHandler` | `AvatarUrl` | `GetPublicUrl` |
| `GetAdminUsersQueryHandler` | `AvatarUrl` | `GetPublicUrl` |
| `PublicCourseCatalogSearchService` | `CoverImageUrl` | `GetPublicUrl` |
| `GetCourseByIdQueryHandler` | `CoverImageUrl` | `GetPublicUrl` |
| Усі хендлери з `CategoryImageUrl` | `ImageUrl` | `GetPublicUrl` |

**Залишити `GenerateReadUrl` тільки для:**
- Сертифікати (`certificates` контейнер)
- Відео уроків (`course-videos` контейнер)

#### Фронтенд

Жодних змін не потрібно — фронтенд вже отримує готовий URL і вставляє в `src`. Після виправлення бекенду URL просто стане стабільним замість тимчасового.

### Поточний стан

Зараз `AvatarUrl` генерується через `GenerateReadUrl` з 24h TTL у трьох хендлерах (`GetMyProfile`, `GetUserProfile`, `GetAdminUsers`). Це функціонально працює, але має описані вище недоліки.
