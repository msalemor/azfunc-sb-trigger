# Azure Function - Service Bus Trigger


## Testing

- Create an Azure Service Bus Namespace
- Create two queues called:
  - simplequeue (1GB, 1 Hour TTL, auto deadletter)
  - robustqueue (1GB, 1 Hour TTL, auto deadletter)
- Create a shared access key for Listen only
- Clone this repo
- Add the local.settings.json and modify the settings
- Modify the host.json file

## Terraform script

```terraform
provider "azurerm" {
  features {}
}

var location = "eastus"
var rgName = "demonamespace"
var simpleQueue = "simplequeue"
var robustQueue = "robustqueue"

resource "azurerm_resource_group" "rg" {
  name     = var.rgName
  location = var.location
}

resource "azurerm_servicebus_namespace" "sbnamespace" {
  name                = "tfex-servicebus-namespace"
  location            = azurerm_resource_group.example.location
  resource_group_name = azurerm_resource_group.example.name
  sku                 = "Standard"

  tags = {
    source = "terraform"
  }
}

resource "azurerm_servicebus_queue" "example" {
  name                = var.simpleQueue
  resource_group_name = azurerm_resource_group.rg.name
  namespace_name      = azurerm_servicebus_namespace.sbnamespace.name
  default_message_ttl = "00:30:00"
  dead_lettering_on_message_expiration = true
  # enable_partitioning = true
}

resource "azurerm_servicebus_queue" "example" {
  name                = var.robutQueue
  resource_group_name = azurerm_resource_group.rg.name
  namespace_name      = azurerm_servicebus_namespace.sbnamespace.name
  dead_lettering_on_message_expiration = true
  # enable_partitioning = true
}
```

## Function 1 - Simple Function


Features:

- Message is marked completed when the funcion finishes executing
- If an exception is thrown, the message is abandoned and the function retries the message
- After certain number of retries, the message automatically deadletters
- By default, messages need to complete within 5 minutes, but this can be configured

```c#
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace sbfunc1
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Run([ServiceBusTrigger("simplequeue", Connection = "SBConnectionString")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

            // throw an error to retry
            // eventually message deadletters
            // modify host.json: "messageHandlerOptions": "autoComplete": true
        }
    }
}

```


## Function 2 - Robust Function


Features:

- Access to the message body and properties
- Access to the lock tocken
- Ability to deadleatter, abandon and complete a queue or topic
- Access to configuration
- By default, messages need to complete within 5 minutes, but this can be configured


```c#
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;


namespace sbfunc1
{
    public static class Function2
    {

        // IsSessionsEnabled = false is not needed, create a queue that supports sessions and set it to true to process messages in order
        [FunctionName("Function2")]
        public static async Task Run([ServiceBusTrigger("robustqueue", Connection = "SBConnectionString", IsSessionsEnabled = false)] Message myQueueItem, ILogger log, MessageReceiver messageReceiver, ExecutionContext context)
        {
            var content = Encoding.UTF8.GetString(myQueueItem.Body, 0, myQueueItem.Body.Length);
            log.LogInformation($"Message received: {content}");

            // Read configuration

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // You can use external libraries
            await Shared.Library.Work.DoWorkAsync();

            var rnd = (new Random()).Next(1, 3);

            switch (rnd)
            {
                case 1:
                    log.LogInformation("Message completed");
                    await messageReceiver.CompleteAsync(myQueueItem.SystemProperties.LockToken);
                    break;
                case 2:
                    log.LogInformation("Message abandoned");
                    await messageReceiver.AbandonAsync(myQueueItem.SystemProperties.LockToken);
                    break;
                case 3:
                    log.LogInformation("Message dead letterred");
                    await messageReceiver.DeadLetterAsync(myQueueItem.SystemProperties.LockToken);
                    break;
                default:
                    log.LogInformation("Message completed");
                    await messageReceiver.CompleteAsync(myQueueItem.SystemProperties.LockToken);
                    break;
            }
        }
    }
}
```

## local.settings.json file

> Note: This file contains the local settings and is not checked in by default. These settings need to be deployed as application settings.

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "SBConnection": "<ADD CONNECTION STRING CREDENTIALS>"
  }
}
```

## Modified host.json file

> For the second Function2, autocomplete is set to false. This gives control over what can be completed, abandoned, and dead letter.

```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingExcludedTypes": "Request",
      "samplingSettings": {
        "isEnabled": true
      }
    }
  },
  "extensions": {
    "serviceBus": {
      "messageHandlerOptions": { "autoComplete": false }
    }
  }
}
```
