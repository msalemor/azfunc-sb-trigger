# Azure Function SB Trigger

## local.settings.json file

> Note: Not checked in

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
## Function 2 - Robust Function
