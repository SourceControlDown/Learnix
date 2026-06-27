resource "azurerm_cosmosdb_mongo_vcore_cluster" "cosmos" {
  name                = "${var.prefix}-cosmos"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location

  administrator_login          = var.mongo_admin_user
  administrator_login_password = var.mongo_admin_password

  # Note: The vCore Free tier corresponds to specific tier/node_type options in Terraform.
  # If "Free" throws an error during plan (due to provider updates), use "M0" or the lowest available tier (e.g., "M20").
  tier      = "Free" 
  node_type = "Free"

  # Networking: Allow public access from any Azure service (matching the UI guide)
  public_network_access_enabled = true
}

# Rule to allow Azure services to connect (equivalent to checking "Allow public access from Azure services" in UI)
resource "azurerm_cosmosdb_mongo_vcore_firewall_rule" "allow_azure" {
  name                = "AllowAzureServices"
  cluster_name        = azurerm_cosmosdb_mongo_vcore_cluster.cosmos.name
  resource_group_name = azurerm_resource_group.rg.name
  
  # 0.0.0.0 represents all Azure-internal IP addresses
  start_ip_address    = "0.0.0.0"
  end_ip_address      = "0.0.0.0"
}

output "mongo_connection_string" {
  value       = "mongodb+srv://${var.mongo_admin_user}:${var.mongo_admin_password}@${azurerm_cosmosdb_mongo_vcore_cluster.cosmos.name}.mongocluster.cosmos.azure.com/?tls=true&authMechanism=SCRAM-SHA-256&retrywrites=false&maxIdleTimeMS=120000"
  sensitive   = true
  description = "The MongoDB connection string. Pass this to Mongo__ConnectionString in the app."
}
