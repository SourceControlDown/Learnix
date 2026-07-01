# Guide: Running Terraform for Learnix Infrastructure

This document describes how to deploy Azure Blob Storage (with automatic cleanup) and Azure Cosmos DB (Mongo) locally from your computer using Terraform.

> [!IMPORTANT] 
> **Local Development vs Production**
> Do not use Terraform to configure your local development environment (Azurite emulator). Azurite does not support Advanced Azure features like Lifecycle Policies, which will cause Terraform to fail.
> For local development, Blob containers are automatically provisioned via the `Learnix.DbMigrator` project on startup (Zero-Click Setup).
> Terraform should ONLY be used to deploy the real cloud infrastructure for Staging or Production environments.
## 1. Prerequisites

1. Install [Terraform](https://developer.hashicorp.com/terraform/downloads) (e.g., via `winget install Hashicorp.Terraform`).
2. Ensure you have the [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) installed (`winget install Microsoft.AzureCLI`).

## 2. Authentication

Open a terminal (PowerShell) in the `infrastructure/` folder and run the following command:
```powershell
az login
```
Your browser will open. Log in to your Azure account. Terraform will automatically use this active session to access Azure, so **no passwords need to be hardcoded or stored in files**.

## 3. Running Terraform

Navigate to the `infrastructure` folder (if you aren't already there):
```powershell
cd d:\projects\Learnix\infrastructure
```

### Step 1: Initialize
```powershell
terraform init
```
This command downloads the necessary provider plugins (AzureRM) required to interact with the cloud.

### Step 2: Plan
We need to pass the password for the Mongo database administrator. Because this is a `sensitive` variable, it is best to pass it via the command line at runtime:
```powershell
terraform plan -var="mongo_admin_password=YourStrongPassword123!"
```
Terraform will analyze your configuration files and display exactly what it intends to create (Resource Group, Storage Account, Containers, Cosmos DB). It won't create anything yet; this is just a preview.

### Step 3: Apply
If the plan looks correct, apply it:
```powershell
terraform apply -var="mongo_admin_password=YourStrongPassword123!"
```
You will be prompted to confirm the action (type `yes` and press Enter). Terraform will begin creating the infrastructure. This process will take a few minutes (Cosmos DB provisioning usually takes the longest).

### Step 4 (Optional): Applying Only Specific Resources
If you want to create *only* the Blob Storage and ignore the Cosmos DB for now, you have two options:

**Option A: The File Rename Method (Recommended)**
Terraform only reads files ending in `.tf`. If you want to temporarily disable the Cosmos DB deployment, just rename `cosmos.tf` to `cosmos.tf.disabled`. 
```powershell
Rename-Item cosmos.tf cosmos.tf.disabled
terraform apply
```
*(Note: You won't need to pass the Mongo password variable if the Mongo file is disabled).*

**Option B: The Target Flag**
You can tell Terraform to exclusively target specific resources using the `-target` flag:
```powershell
terraform apply -target="azurerm_storage_account.storage"
```
*(Note: HashiCorp recommends Option A for regular use, as targeting is meant for exceptional situations).*

## 4. Results

After Terraform completes successfully, it will output two strings to the console:
- `storage_connection_string` (The connection string for your Blob Storage).
- `mongo_connection_string` (The connection string for your MongoDB database).

Because these outputs are marked as `sensitive`, you must run the following commands to view their actual values:
```powershell
terraform output -raw storage_connection_string
terraform output -raw mongo_connection_string
```
Copy these strings and paste them into your `appsettings.json` (for local development) or your Azure Container Apps environment variables.

---

> [!NOTE] 
> **About Lifecycle Management**
> The `temp-uploads` container is automatically created with a lifecycle cleanup policy (blobs older than 1 day will be automatically deleted). You no longer need to manage this manually or via C# code. 
> Check `storage.tf` to review the configuration and public/private access levels of the other containers.
