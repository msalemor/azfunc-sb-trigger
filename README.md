# Azure Function 
## Service Bus Trigger

## local.settings.json file

> Note: This file is not checked in by default

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "SBConnection": "<ADD PRIMERY ENDPOINT>"
  }
}
```

## Modified host.json file

> For the second Function2, autoclote is set to false. This gives control over what can be completed, abandoned, and dead letter.

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

## Function 1 - Simple Function


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

            // Do some work
            await Task.Delay(1000);

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