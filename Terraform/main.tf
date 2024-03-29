terraform {
  backend "azurerm" {
    resource_group_name  = "Terraform"
    storage_account_name = "terraformtaxyz"
    container_name       = "tstate"
    key                  = "peepquote.terraform.tfstate"

  }

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">= 3.0.0"
    }
  }

}

provider "azurerm" {
  features {}
  skip_provider_registration = true
}

data "azurerm_client_config" "current" {}

variable "access_token" {}
variable "access_token_secret" {}
variable "consumer_key" {}
variable "consumer_secret" {}


resource "azurerm_resource_group" "rg" {
  name     = "PeepQuote"
  location = "uksouth"
}


resource "azurerm_storage_account" "functionstorage" {
  name                     = "peepquotestorage"
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_resource_group.rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_account" "scriptstorage" {
  name                     = "peepqscriptstorage"
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_resource_group.rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  access_tier              = "Cool"
}

resource "azurerm_storage_container" "scriptcontainer" {
  name                  = "scripts"
  storage_account_name  = azurerm_storage_account.scriptstorage.name
  container_access_type = "private"
}

resource "azurerm_app_service_plan" "serviceplan" {
  name                = "peepquotesp"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  kind                = "FunctionApp"
  sku {
    tier = "Dynamic"
    size = "Y1"
  }
}

resource "azurerm_function_app" "function" {
  name                       = "peepquote"
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  version                    = "~4"
  app_service_plan_id        = azurerm_app_service_plan.serviceplan.id
  storage_account_name       = azurerm_storage_account.functionstorage.name
  storage_account_access_key = azurerm_storage_account.functionstorage.primary_access_key
  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME"        = "dotnet",
    "WEBSITE_RUN_FROM_PACKAGE"        = "1",
    "WEBSITE_ENABLE_SYNC_UPDATE_SITE" = "true",
    "ConnectionString"                = "",
  }
  site_config {
    cors {
      allowed_origins = [
        "*"
      ]
    }
  }

}

resource "azurerm_storage_account" "imagestorage" {
  name                            = "peepimagestorage"
  resource_group_name             = azurerm_resource_group.rg.name
  location                        = azurerm_resource_group.rg.location
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  access_tier                     = "Cool"
  allow_nested_items_to_be_public = true
  enable_https_traffic_only       = false
  custom_domain {
    name = "images.peepquote.com"
  }
}

resource "azurerm_storage_container" "imagecontainer" {
  name                  = "imagecontainer"
  storage_account_name  = azurerm_storage_account.imagestorage.name
  container_access_type = "container"
}
