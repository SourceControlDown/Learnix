# Deploy Frontend to Azure Static Web Apps

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

### AZURE_STATIC_WEB_APPS_API_TOKEN
Це токен для розгортання (deployment token). Він дозволяє GitHub Actions підключитися до вашого Static Web App і завантажити туди готову збірку фронтенду (папку `dist`).

**Звідки брати:**

1. Відкрийте портал Azure.
2. Знайдіть у списку ресурсів ваш **Static Web App** (зазвичай має назву типу `learnix-frontend-staging` або `learnix-frontend-prod`).
3. Перейдіть до нього.
4. На панелі зліва знайдіть розділ **Settings** і натисніть **Deployment tokens** (Токени розгортання).
5. Відкриється права панель з токеном. Скопіюйте його повністю.
6. Збережіть отриманий рядок у **GitHub Secrets** як **AZURE_STATIC_WEB_APPS_API_TOKEN**.


This is the deployment token used to upload your built React frontend to Azure Static Web Apps.

**How to get the token:**
1. Go to the [Azure Portal](https://portal.azure.com/).
2. Navigate to your Static Web App resource (the one created for the frontend).
3. On the **Overview** page, click **Manage deployment token** in the top menu.
4. Copy the token string from the right-side pane. Save it in GitHub Secrets as **AZURE_STATIC_WEB_APPS_API_TOKEN**.

---