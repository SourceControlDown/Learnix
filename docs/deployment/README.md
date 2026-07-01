# Learnix — Azure Deployment Guide

The deployment guides have been split into two separate documents depending on your preferred approach:

- [**Azure Portal UI Guide**](./AZURE_PROVISIONING_UI.md) — Step-by-step instructions for creating resources manually using the Azure Web Portal.
- [**Azure CLI Guide**](./AZURE_DEPLOY_CLI.md) — Script-based instructions for creating resources using the Azure CLI (`az`).

---

## Order of Operations: The "Chicken and Egg" Problem

When deploying the full stack, you might notice a cyclic dependency:
- The **Frontend** needs the Backend URL during the build (`VITE_API_URL`) to know where to send API requests.
- The **Backend** needs the Frontend URL during deployment (`PROD_ALLOWED_ORIGINS`) to configure CORS security.

To resolve this, you must separate **infrastructure creation** from **code deployment**. Follow this exact order:

### 1. Provision Infrastructure First (Empty Resources)
Create the resources in Azure using either the UI or CLI guides linked above. **Do not run GitHub Actions yet.**
- Create the **Azure Static Web App**. Once created, Azure immediately assigns it a permanent public URL (e.g., `https://lively-river-012345.azurestaticapps.net`).
- Create the **Azure Container App**. Azure assigns it a permanent public URL (e.g., `https://learnix-api.calmpond-xyz.eastus.azurecontainerapps.io`).

*At this stage, the resources are empty (or show a default placeholder), but the URLs are locked in.*

### 2. Configure GitHub Secrets and Variables
Now that you have both URLs, go to your repository on GitHub: **Settings → Secrets and variables → Actions**.
Populate the URLs into the **Variables** tab:
- Set `VITE_API_URL` to your Container App URL.
- Set `PROD_ALLOWED_ORIGINS` and `PROD_CLIENT_BASE_URL` to your Static Web App URL.

### 3. Deploy the Code
With all secrets and variables in place, trigger the GitHub Actions workflow (`deploy.yml`) by pushing to `main` or running it manually.
- The frontend build process will correctly embed the backend URL.
- The backend deployment process will correctly configure CORS for the frontend URL.
