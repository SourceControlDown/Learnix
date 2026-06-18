# Learnix — Azure Deployment Guide (Azure Portal UI)

> Manual first-time deployment walkthrough using the Azure Portal UI.  
> **Stack:** Azure Container Apps (API) + Azure Static Web Apps (frontend)  
> **Estimated time:** 2–3 hours for first-time setup

For deployment via Azure CLI, see [AZURE_DEPLOY_CLI.md](./AZURE_DEPLOY_CLI.md).

---

## Prerequisites

1. Login to the [Azure Portal](https://portal.azure.com/).
2. You still need Docker installed locally to build and push the API image, and `.NET Core CLI` to run EF Core database migrations.

---

## Resource naming convention

| Resource | Name used in this guide |
|---|---|
| Resource Group | `learnix-rg` |
| Location | `West Europe` (or `North Europe`) |
| Container Registry | `learnixacr` |
| Container Apps Environment | `learnix-env` |
| Container App (API) | `learnix-api` |
| PostgreSQL Server | `learnix-postgres` |
| Cosmos DB (Mongo API) | `learnix-cosmos` |
| Redis Cache | `learnix-redis` |
| Storage Account | `learnixstorage` |
| Static Web App | `learnix-frontend` |

> **Note:** Storage account names must be globally unique, all lowercase, 3–24 chars, no hyphens.  
> Replace `learnixstorage` with something unique if it's taken.

---

## Step 1 — Resource Group

A resource group is a logical container for all Learnix Azure resources.
It lets you manage, monitor, and delete everything (ACR, Container Apps, PostgreSQL, etc.)
as a single unit.

1. In the Azure Portal, search for **Resource groups** and click **Create**.

2. **Subscription:** choose your active subscription.

3. **Resource group name:** `learnix-rg`
   — one group for all Learnix resources keeps billing and cleanup simple.

4. **Region:** `Poland Central` (or `West Europe`)
   — determines where the group's metadata is stored. Does not restrict where
   individual resources inside it can be deployed, but keeping everything
   in one region avoids cross-region egress costs.

5. Click **Review + create**, then **Create**.

---

## Step 2 — Azure Container Registry (ACR)

> [!WARNING]
> **SKIPPED / DEPRECATED:** ACR costs ~$5/month even if empty. For pet projects and 100% free deployments, we highly recommend using **Docker Hub** instead. Proceed directly to **Step 2 (Alternative)** below.

<details>
<summary>Click here if you still want to deploy Azure Container Registry (Original Instructions)</summary>

ACR is a private Docker image registry. GitHub Actions will push built images here,
and Container Apps will pull from here to run your backend.

1. Search for **Container registries** and click **Create**.

2. **Resource group:** `learnix-rg`
   — keeps all Learnix resources grouped together.

3. **Registry name:** `learnixacr`
   — must be globally unique; becomes your login server: `learnixacr.azurecr.io`.
   If taken, try `learnixacrdev` or similar.

4. **Location:** `Poland Central` (or `West Europe`)
   — keep the same region as your other resources to avoid cross-region egress costs.

5. **Domain name label scope:** `Unsecure`
   — affects registry domain name format only; irrelevant for portfolio use.

6. **Registry domain name:** leave empty
   — auto-filled based on registry name.

7. **Use availability zones:** leave unchecked
   — only available on Premium plan; not needed for portfolio.

8. **Pricing plan:** `Basic`
   — ~$5/month flat. Sufficient storage and throughput for portfolio CI/CD.

9. **Role assignment permissions mode:** `RBAC Registry Permissions`
   — standard mode; controls who can push/pull images via Azure roles.

10. Click **Review + create**, then **Create**.

11. Once created, go to the resource → **Settings → Access keys**.
    Enable **Admin user** and save:
    - **Login server** (e.g. `learnixacr.azurecr.io`)
    - **Username**
    - **Password**
    
    These will be stored as GitHub Actions secrets for the CI/CD pipeline.
</details>

---

## Step 2 (Alternative) — Docker Hub (Free)

Docker Hub provides 1 free private repository and unlimited public repositories.

1. Go to [Docker Hub](https://hub.docker.com/) and create a free account if you don't have one.
2. Log in and click **Create repository**.
3. **Name:** `learnix-api`
4. **Visibility:** `Public` (or `Private` if you prefer). Public is easier because Azure Container Apps can pull it without configuring authentication.
5. Click **Create**.
6. Note down your Docker Hub username (e.g., `yourusername`) and the repository name. Your full image name will be `yourusername/learnix-api`.

---

## Step 3 — Azure Database for PostgreSQL (Flexible Server)

> [!WARNING]
> **SKIPPED / DEPRECATED:** Due to the minimum cost of ~$20/month even for the lowest `B1ms` tier, this step is skipped for pet projects. We highly recommend using **Supabase** (Free Tier) instead. Proceed directly to **Step 3 (Alternative)** below.

<details>
<summary>Click here if you still want to deploy Azure PostgreSQL (Original Instructions)</summary>

PostgreSQL Flexible Server is the primary relational database for the Learnix backend.

1. Search for **Azure Database for PostgreSQL flexible servers** and click **Create**.

2. **Resource group:** `learnix-rg`
   — keeps the database logically grouped with the rest of the project.

3. **Server name:** `learnix-postgres`
   — must be globally unique across Azure. Try adding a suffix if it's taken (e.g., `learnix-postgres-dev`).

4. **Region:** `Poland Central` (or `West Europe`)
   — select the exact same region as your Resource Group to ensure low latency and avoid cross-region data transfer costs.

5. **PostgreSQL version:** `16`
   — the latest stable version supported by the application.

6. **Workload type:** `Development`
   — pre-selects cost-effective defaults.

7. **Compute + storage:** Click *Configure server* to open the detailed configuration pane:
   - **Cluster options:** Select `Server` (Elastic cluster is not needed).
   - **Compute tier:** Select `Burstable (1-20 vCores)` — intended for development use outside of a production environment.
   - **Compute size:** Select `Standard_B1ms (1 vCore, 2 GiB memory, 640 max iops)` — this is the most cost-effective option for development and pet projects. (Azure might default to B2s, but B1ms is perfectly sufficient).
   - **Storage type:** Select `Premium SSD`.
   - **Storage size:** `32 GiB`.
   - **Performance tier:** `P4 (120 iops)`.
   - **Storage autogrow:** Uncheck the box (leave it disabled).
   - **Zonal resiliency:** Select `Disabled (99.9% SLA)`.
   - **Backup retention period (in days):** Set the slider to `7`.
   - Click **Save** at the bottom of the pane.

8. **Authentication:** 
   - **Authentication method:** Select `PostgreSQL authentication only` (or `PostgreSQL and Microsoft Entra authentication`).
   - Create an admin user `learnixadmin` and a strong password `YourStrongPassword123!`. Keep these safe!

9. Click **Next: Networking**.

10. **Firewall rules:** 
    - Check **Allow public access from any Azure service within Azure to this server**
      — *Crucial:* this allows your Azure Container App (backend) to communicate with this database. Without it, the backend will fail to connect.
    - Click **Add current client IP address**
      — *Crucial:* this allows your home/work PC to connect to the database to run Entity Framework migrations.

11. Click **Review + create**, then **Create** (provisioning takes ~3-5 minutes).

12. Once created, go to the resource, click **Databases** on the left menu, and click **Add**. Name it `learnix` and save.

**Connection string format for later:**
```
Host=learnix-postgres.postgres.database.azure.com;Port=5432;Database=learnix;Username=learnixadmin;Password=YourStrongPassword123!;Ssl Mode=Require;Trust Server Certificate=true
```
</details>

---

## Step 3 (Alternative) — Supabase PostgreSQL (Free)

1. Go to [Supabase.com](https://supabase.com/) and create a free account.
2. Create a new organization (required for first-time users):
   - **Name:** Enter your name (e.g., `My-Organisation's Org`).
   - **Type:** Select `Personal`.
   - **Plan:** Select `Free - $0/month`.
   - Click **Create organization**.
3. Click **New Project** and select your newly created organization.
4. **Name:** `learnix`
5. **Database Password:** Generate a strong password (e.g., `YourStrongPassword123!`) and save it securely.
6. **Region:** Select `Central EU (Frankfurt)` for the lowest latency to your Azure resources in West Europe / Poland Central.
7. **Security** (Leave defaults as shown in the UI):
   - **Enable Data API:** Checked.
   - **Automatically expose new tables:** Checked.
   - **Enable automatic RLS:** Unchecked.
     *(Note: Since Learnix uses its own custom .NET backend API to access the database directly, these Supabase-specific Data API features aren't used, so the defaults are perfectly fine).*
8. Click **Create new project** (takes a few minutes).
9. Once the dashboard loads, click the green **Connect** button at the top of the screen.
10. In the modal that opens, select the **Direct (Connection string)** tab.
11. Under **Connection Method**, select **Session pooler**.
    *(Important: Do NOT select "Direct connection". Supabase direct connections use IPv6 by default, which may block your home PC or Azure Container Apps from connecting. "Session pooler" provides a compatible IPv4 connection on port `5432` which is required for Entity Framework migrations).*
12. Under **Type**, ensure **URI** is selected.
13. Scroll down and copy your **Connection string**. It will look something like this:

**Supabase connection string format for later:**
```
postgresql://postgres.yourprojectref:[YOUR-PASSWORD]@aws-0-eu-central-1.pooler.supabase.com:5432/postgres?sslmode=require&Trust Server Certificate=true
```
*(Remember to manually replace `[YOUR-PASSWORD]` in the copied string with the actual password you created earlier).*

> [!TIP]
> **SSL Encryption:** We appended `?sslmode=require&Trust Server Certificate=true` to the end of the connection string. This ensures your data is encrypted in transit (`sslmode=require`), while bypassing the need to manually install Supabase's CA certificates on your server or local machine (`Trust Server Certificate=true`). This provides the ideal balance of security and simplicity.

---

## Step 4 — Azure Cosmos DB (MongoDB API)

1. Search for **Azure Cosmos DB** and click **Create**.
2. Select **Azure Cosmos DB for MongoDB** and click **Create**.
3. Select **Request Unit (RU)** architecture (or vCore if you prefer).
4. **Resource group:** `learnix-rg`.
5. **Account name:** `learnix-cosmos`.
6. **Location:** `West Europe`.
7. **Capacity mode:** Serverless (cheaper for Dev) or Provisioned.
8. Click **Review + create**, then **Create** (takes ~5-10 mins).
9. Once created, click **Data Explorer** and click **New Database**. Name it `learnix`.
10. Click **Connection strings** on the left menu. Copy the **PRIMARY CONNECTION STRING**.

---

## Step 5 — Azure Cache for Redis

1. Search for **Azure Cache for Redis** and click **Create**.
2. **Resource group:** `learnix-rg`.
3. **DNS name:** `learnix-redis`.
4. **Location:** `West Europe`.
5. **Pricing tier:** `Basic C0`.
6. Click **Review + create**, then **Create**.
7. Once created, go to **Access keys** on the left menu to find your Primary connection string.

---

## Step 6 — Azure Blob Storage

1. Search for **Storage accounts** and click **Create**.
2. **Resource group:** `learnix-rg`.
3. **Storage account name:** `learnixstorage`.
4. **Region:** `West Europe`.
5. **Performance:** Standard. **Redundancy:** LRS.
6. Click **Review + create**, then **Create**.
7. Once created, click **Containers** under Data storage.
8. Create the following containers:
   - `avatars` (Set public access level to **Blob**)
   - `course-covers` (Set public access level to **Blob**)
   - `course-videos` (Set public access level to **Blob**)
   - `certificates` (Set public access level to **Private**)
9. Go to **Access keys** on the left menu and copy your **Connection string**.

---

## Step 7 — Run Database Migrations

Before deploying the API, apply EF migrations from your local machine using the CLI.

> [!NOTE]
> If you used **Supabase (Step 3 Alternative)**, paste your Supabase connection URI here instead of the Azure one. Ensure connection pooling is off (port 5432) for migrations to succeed.

```powershell
# Open a terminal in the Learnix.Backend directory.
# Set your connection string temporarily (Azure or Supabase):
$env:ConnectionStrings__Postgres = "postgresql://postgres.yourprojectref:YourStrongPassword123!@aws-0-eu-central-1.pooler.supabase.com:5432/postgres?sslmode=require&Trust Server Certificate=true"
$env:ASPNETCORE_ENVIRONMENT = "Production"

dotnet ef database update --project Learnix.Infrastructure --startup-project Learnix.API
```

---

## Step 8 — Build & Push API Docker Image

You need to push your API image to the registry created in Step 2.

> [!NOTE]
> If you used **Docker Hub (Step 2 Alternative)**, use the commands below. If you used ACR, replace `yourusername` with your ACR login server.

```powershell
# Login to Docker Hub (it will prompt for your password/access token)
docker login -u yourusername

# Build the image from the solution root
docker build -t yourusername/learnix-api:latest -f Learnix.Backend/Dockerfile ./Learnix.Backend

# Push the image
docker push yourusername/learnix-api:latest
```

---

## Step 9 — Create Container App (API)

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

## Step 10 — Deploy Frontend to Azure Static Web Apps

1. Before starting, update `.env.production` locally to include your API URL: `VITE_API_URL=https://learnix-api.xxxx.azurecontainerapps.io/api`.
2. Push your code to GitHub.
3. In the Azure Portal, search for **Static Web Apps** and click **Create**.
4. **Resource group:** `learnix-rg`.
5. **Name:** `learnix-frontend`.
6. **Hosting plan:** Free.
7. **Region:** `West Europe`.
8. **Deployment details:** Select **GitHub**. Authenticate and choose your organization, repository, and branch (`main`).
9. **Build Details:** 
    - **Build Presets:** Custom
    - **App location:** `/learnix-client`
    - **Api location:** (leave blank)
    - **Output location:** `dist`
10. Click **Review + create**, then **Create**.
11. Azure will automatically create a GitHub Action in your repo and start building the frontend.
12. Once deployed, copy the **URL** (e.g., `https://proud-pond-xxx.azurestaticapps.net`).

---

## Step 11 — Update CORS and ClientBaseUrl

1. Go back to your Container App (`learnix-api`).
2. Click **Containers** -> **Edit and deploy**.
3. Click on your container image.
4. Add two new environment variables:
   - `Cors__AllowedOrigins__0` = `https://proud-pond-xxx.azurestaticapps.net`
   - `App__ClientBaseUrl` = `https://proud-pond-xxx.azurestaticapps.net`
5. Save and deploy.

Also add the Static Web Apps URL to **Google Cloud Console → OAuth → Authorized JavaScript origins**.

---

## Step 12 — Update API image (subsequent deploys)

When you make changes to the backend:
1. Build and push the new Docker image locally (see Step 8).
2. Go to your Container App (`learnix-api`) in the Portal.
3. Click **Revision management**.
4. Click **Create new revision**.
5. Keep the settings identical, but make sure the image pulls the latest tag, then save to deploy the new revision.
