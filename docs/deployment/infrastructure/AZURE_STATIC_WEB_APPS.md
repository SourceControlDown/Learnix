# Azure Static Web Apps (Frontend)

## Environment Variables

By completing this guide, you will determine the following values. Configure them in your GitHub repository (**Settings → Secrets and variables → Actions**):

* **Variables:**
  - `PROD_ALLOWED_ORIGINS` (The generated URL of your Static Web App, e.g., `https://proud-pond-xxx.azurestaticapps.net`)
  - `PROD_CLIENT_BASE_URL` (Same as above)
* **Secrets:**
  - `AZURE_STATIC_WEB_APPS_API_TOKEN` (The deployment token obtained at the end of this guide)

---

## Create Static Web App

Search for **Static Web Apps** in the Azure Portal and click **Create**. Follow the tabs below exactly as shown in the reference screenshots.

### 1. Basics
* **Project Details:**
  * **Subscription:** Select your active subscription (e.g., `Azure subscription 1`)
  * **Resource Group:** `learnix-rg`
* **Static Web App details:**
  * **Name:** Enter a name for your Static Web App (e.g., `learnix-client`)
* **Hosting plan:**
  * **Plan type:** Select `Standard: For general purpose production apps`
* **Deployment details:**
  * **Source:** Select `GitHub`
  * **GitHub account:** Click `Sign in with GitHub` (if prompted).

Click **Next : Deployment configuration >**

### 2. Deployment configuration
* **Deployment authorization policy:** Select `Deployment token`
  *(Note: By selecting Deployment token instead of GitHub, Azure will not automatically create and commit a workflow file to your repository. Our existing `deploy.yml` pipeline will handle the deployment using this token).*

Click **Next : Advanced >**

### 3. Advanced
* **Azure Functions and staging details:**
  * **Region for Azure Functions API and staging environments:** Select your region (e.g., `West Europe`)
* **Enterprise-grade edge:**
  * **Enterprise-grade edge:** Leave unchecked

Click **Review + create**, wait for validation, then click **Create**.

### 4. Post-Creation
Once the deployment is complete, go to the newly created resource.

1. **Get the Application URL:**
   On the **Overview** page, copy the **URL** (e.g., `https://proud-pond-xxx.azurestaticapps.net`). 
   Save this exact value in your GitHub Variables as both `PROD_ALLOWED_ORIGINS` and `PROD_CLIENT_BASE_URL`.

2. **Get the Deployment Token:**
   On the **Overview** page, click **Manage deployment token** in the top menu.
   Copy the token string from the right-side pane.
   Save this value in your GitHub Secrets as `AZURE_STATIC_WEB_APPS_API_TOKEN`.