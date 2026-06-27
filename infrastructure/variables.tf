variable "prefix" {
  description = "A prefix used for all resources in this example"
  default     = "learnix"
}

variable "location" {
  description = "The Azure Region in which all resources in this example should be created."
  default     = "West Europe"
}

variable "mongo_admin_user" {
  description = "The administrator login for Cosmos DB Mongo vCore"
  default     = "learnixadmin"
}

variable "mongo_admin_password" {
  description = "The administrator password for Cosmos DB Mongo vCore"
  type        = string
  sensitive   = true
}
