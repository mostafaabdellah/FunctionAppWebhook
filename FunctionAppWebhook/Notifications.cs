using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FunctionAppWebhook
{
    public static class Notifications
    {
        [FunctionName("Notifications")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            try
            {
                var storageConnection = string.Empty;
                storageConnection = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process);
                StorageAccount storageAccount = StorageAccount.NewFromConnectionString(storageConnection);
                var queueClient = storageAccount.CreateCloudQueueClient();
                var queue = queueClient.GetQueueReference("webhooknotifications");
                queue.CreateIfNotExistsAsync().Wait();
                queue.AddMessageAsync(new Microsoft.Azure.Storage.Queue.CloudQueueMessage(requestBody)).Wait();

            }
            catch (Exception exc)
            {

                log.LogError(exc.Message);
            }
            return new OkResult();
        }
    }
}
