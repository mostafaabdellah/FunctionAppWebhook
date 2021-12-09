using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json;


namespace FunctionAppWebhook
{
    public static class CreateRemoteEventReceiver
    {
        private const string AppId = "3e16ec1a-e298-44b6-82b0-bb5fc5847a1c";
        private const string AppSecret = "vhRPs/WQ3MJjeuZQJqcZfF+UiiqLNuu9cG0i4inookY=";
        private const string ListId = "9a9a75d6-ae2c-48ee-b3aa-30d7abbeaff5";
        private const string SiteUrl = "https://mmoustafa.sharepoint.com/sites/Demo2/";
        private const string ReceiverUrl = "https://webhook20211.azurewebsites.net/api/Notifications?code=ubGveAGHF2sRFuSpYLR1IQir9Cw2699dqYbq8cUhLBa5S/bPd3wrfA==";

        [FunctionName("CreateRemoteEventReceiver")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            CreateRemoteEventReceiversBySpAppOnly();

            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }

        private static void CreateRemoteEventReceiversBySpAppOnly()
        {
            using (var cc = new PnP.Framework.AuthenticationManager().GetACSAppOnlyContext(SiteUrl, AppId, AppSecret))
            {
                var list = cc.Web.Lists.GetById(new System.Guid(ListId));
                cc.ExecuteQuery();
                List<EventReceiverType> eventsList = new List<EventReceiverType>
                {EventReceiverType.ItemAdded, EventReceiverType.ItemUpdated,
                    EventReceiverType.ItemCheckedIn, EventReceiverType.ItemCheckedOut,
                    EventReceiverType.ItemUncheckedOut, EventReceiverType.ItemFileMoved,
                    EventReceiverType.ItemDeleted, EventReceiverType.ItemVersionDeleted};

                foreach (EventReceiverType eventReceiverType in eventsList)
                    AddEventReceiver(list, ReceiverUrl, eventReceiverType);
            };
        }
        private static void CreateRemoteEventReceiversByAzureApp()
        {
            var tenantId = "0d4ca527-dc44-43d1-84c1-b63d1b1e024d";
            var clientId= "850e0eec-9575-48b0-9fd2-5d57f9948ad3";
            X509Certificate2 certificate = new X509Certificate2("C:\\SharePoint\\mmoustafa.onmicrosoft.com.pfx", "Opentext1!", X509KeyStorageFlags.MachineKeySet);
            var auth=new PnP.Framework.AuthenticationManager(clientId, certificate, tenantId);
            using (var cc =auth.GetContext(SiteUrl))
            {
                var list = cc.Web.Lists.GetById(new System.Guid(ListId));
                cc.ExecuteQuery();
                List<EventReceiverType> eventsList = new List<EventReceiverType>
                {EventReceiverType.ItemAdded, EventReceiverType.ItemUpdated,
                    EventReceiverType.ItemCheckedIn, EventReceiverType.ItemCheckedOut,
                    EventReceiverType.ItemUncheckedOut, EventReceiverType.ItemFileMoved,
                    EventReceiverType.ItemDeleted, EventReceiverType.ItemVersionDeleted};

                foreach (EventReceiverType eventReceiverType in eventsList)
                    AddEventReceiver(list, ReceiverUrl, eventReceiverType);
            };
        }

        private static void AddEventReceiver(List list, string url, EventReceiverType EventReceiverType)
        {
            var eventReceiver =
                new EventReceiverDefinitionCreationInformation
                {
                    EventType = EventReceiverType,
                    ReceiverName = "OpenText"+EventReceiverType,
                    ReceiverUrl = url,
                    SequenceNumber = 1000,
                    
                };

            var receiver=list.EventReceivers.Add(eventReceiver);
            list.Context.ExecuteQuery();
        }
    }
}

