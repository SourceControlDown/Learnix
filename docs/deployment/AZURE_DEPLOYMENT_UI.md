# Learnix — Azure Deployment Guide (Azure Portal UI)

> Application deployment walkthrough using the Azure Portal UI.  
> **Stack:** Azure Container Apps (API) + Azure Static Web Apps (frontend)  

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

dotnet run --project Learnix.DbMigrator --no-launch-profile -- --seed-demo
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
