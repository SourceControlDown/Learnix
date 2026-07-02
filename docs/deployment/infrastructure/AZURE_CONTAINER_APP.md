## Container App (API)

1. Search for **Container Apps** and click **Create**.
2. **Resource group:** `learnix-rg`.
3. **Container App name:** `learnix-api`.
4. **Region:** `West Europe`.
5. Under Container Apps Environment, click **Create new**. Name it `learnix-env` and save.
6. Click **Next: Container**.
7. **Use image from:** Select `Docker Hub or other registries` (if using Docker Hub) or `Azure Container Registry` (if using ACR).
8. **Image details:** 
   - **Image type:** `Public` (or `Private` if your repo is private).
   - *If private*, enter **Registry login server**: `docker.io`, and provide your Docker Hub username and password.
   - **Image and tag:** Enter your full image name, e.g., `yourusername/learnix-api:latest`.
9. **CPU and Memory:** 0.5 CPU, 1.0 Gi memory.
10. **Environment variables:** Add all necessary backend variables:
    - `ASPNETCORE_ENVIRONMENT` = `Production`
    - `ASPNETCORE_URLS` = `http://+:8080`
    - `ConnectionStrings__Postgres` = `<YOUR_POSTGRES_CONN_STRING>` (Use the Supabase URI if you followed Step 3 Alternative)
    - `ConnectionStrings__Redis` = `<YOUR_REDIS_CONN_STRING>`
    - `ConnectionStrings__AzureBlobStorage` = `<YOUR_BLOB_CONN_STRING>`
    - `Mongo__ConnectionString` = `<YOUR_COSMOS_CONN_STRING>`
    - `Mongo__DatabaseName` = `learnix`
    - `Jwt__Secret` = `<YOUR_64_CHAR_SECRET>`
    - `Jwt__RefreshTokenSecret` = `<YOUR_ANOTHER_64_CHAR_SECRET_PEPPER>`
    - `Jwt__Issuer` = `Learnix`
    - `Jwt__Audience` = `LearnixClient`
    - `Smtp__Host`, `Smtp__Username`, `Smtp__Password` (from SendGrid)
    - etc. (Refer to the CLI guide for the full list of variables).
11. Click **Next: Ingress**.
12. **Ingress:** Enabled.
13. **Ingress traffic:** Accepting traffic from anywhere.
14. **Target port:** `8080`.
15. Click **Review + create**, then **Create**.
16. Once created, copy the **Application Url** (e.g., `https://learnix-api.xxxx.azurecontainerapps.io`). This is your `VITE_API_URL` for the frontend.

---
