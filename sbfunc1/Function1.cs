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
