# Learnix — ADR: Blob Storage

> Format: decision → why → rejected alternatives.
> Covers the backend blob storage architecture and asset management.

## Endpoints Summary

| Endpoint | Who | What it does |
|---|---|---|
| `POST /api/uploads/request-url` | Authenticated Users | Requests a pre-signed SAS URL for direct-to-Azure file upload. |

---

## ADR-BLOB-001: Azure Blob Storage Integration & SDK

**Decision:** The platform uses Azure Blob Storage for all file assets (avatars, course covers, videos, category images, and certificates). The integration is implemented in the `Learnix.Infrastructure` layer using the official `Azure.Storage.Blobs` SDK. 

**Why:**
- Provides a robust, highly scalable storage backend natively supported by Azure.
- The `Azure.Storage.Blobs` SDK allows generating Shared Access Signatures (SAS) easily, enabling secure, time-limited direct client uploads and downloads.

---

## ADR-BLOB-002: Relative Paths in the Database

**Decision:** The database does NOT store absolute URLs for blob assets. Instead, it stores a relative path in the format `{containerName}/{blobName}` (e.g., `avatars/users/123/abcd.jpg`).

**Why:**
- Avoids vendor lock-in and prevents database updates if the storage account name, domain, or environment (Dev vs Prod) changes.
- The application layer can dynamically construct the necessary URL (public or private) based on the context.

**Implementation Details:**
- To build a public URL, the backend uses a pattern like: `!string.IsNullOrWhiteSpace(c.CoverBlobPath) ? blobStorage.GetPublicUrl(c.CoverBlobPath) : null`
- To generate a private read URL with a TTL, the backend uses `blobStorage.GenerateReadUrl(blobPath, ttl)`, which builds a SAS token dynamically.

---

## ADR-BLOB-003: Two-Phase Upload + Outbox for Side-Effects

**Decision:** The entire lifecycle of file uploads is divided into three clear phases to ensure reliability and minimize server load:

1. **Direct Upload (Bypassing API):** 
   - The client asks the backend for a secure, temporary link (`POST /api/uploads/request-url`).
   - The client uploads the file directly to Azure using this link, taking the heavy lifting off our API servers.
2. **Validation:** 
   - When the client submits a form (e.g., updating a profile or lesson), they only send the new file's path.
   - The backend verifies if the file exists in Azure, checks its size, and reads its "magic bytes" to guarantee the file type wasn't spoofed (`IBlobStorageService.ValidateAsync()`).
3. **Background Confirmation (Outbox Pattern):** 
   - The backend saves the new path to the database and simultaneously schedules a background task (Domain Event -> `OutboxMessage`).
   - A background worker safely contacts Azure to "confirm" the new file (`MarkBlobConfirmed`) and "delete" the old one (`DeleteBlob`). 
   - If a file is never confirmed, Azure's lifecycle policy automatically deletes it to prevent orphans.

**Why Pre-signed Upload:**
- Files do not pass through the API server — this removes memory and bandwidth pressure, especially for large videos (up to 2 GB).
- The API doesn't need file streaming middleware or multipart parsing.
- Azure stores the blob atomically — there are no stale partial uploads.

**Why Magic Byte Validation (not Content-Type header):**
- The `Content-Type` header on a SAS PUT can be spoofed by the client.
- Magic bytes (the first N bytes of a file) cannot be spoofed without actually rewriting the file.
- It is implemented and verified for `jpeg` (`FF D8 FF`), `png` (`89 50 4E 47`), `webp` (`52 49 46 46...57 45 42 50`), `mp4` (`ftyp` box), `webm` (`1A 45 DF A3`), and `pdf` (`%PDF`).

**Why Outbox for side-effects (and not direct call after SaveChanges):**
- `MarkConfirmedAsync` and `DeleteAsync` are network calls that can fail. An in-process call after `SaveChangesAsync` is not atomic with entity persistence.
- If the process crashes between `SaveChanges` and `MarkConfirmedAsync`, the blob will be deleted by the lifecycle policy, but the entity will still point to it → resulting in data corruption.
- An `OutboxMessage` in the same transaction as the entity guarantees the operation will be performed eventually (ADR-010).

**Blob path naming convention:**
```text
avatars/users/{userId}/{uploadId}.{ext}
course-covers/courses/{courseId}/{uploadId}.{ext}
course-videos/courses/{courseId}/lessons/{lessonId}/{uploadId}.mp4
certificates/{code}.pdf
```

**UploadTarget validation limits:**
| Target | Max size | Allowed types |
|---|---|---|
| Avatar | 5 MB | jpeg, png, webp |
| CourseCover | 10 MB | jpeg, png, webp |
| LessonVideo | 2 GB | mp4, webm |
| Certificate | 5 MB | pdf |

**Alternatives:**
- **Multipart upload via API:** Simpler for the client, but makes the API a bottleneck for large videos. Rejected.
- **Confirmation without Outbox (direct call after SaveChanges):** Vulnerable to crashes between operations. Rejected after risk analysis.

**Consequences:**
- The `AzureBlobStorageService` implements `IBlobStorageService` as a cross-cutting abstraction.
- Blob paths are stored in the entity, and SAS reading is generated on-demand.


