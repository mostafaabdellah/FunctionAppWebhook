using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.SharePoint.Client;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;

namespace FunctionAppWebhook
{
    public static class RemoveRemoteEventReceiver
    {
        private const string AppId = "3e16ec1a-e298-44b6-82b0-bb5fc5847a1c";
        private const string AppSecret = "vhRPs/WQ3MJjeuZQJqcZfF+UiiqLNuu9cG0i4inookY=";
        private const string ListId = "9a9a75d6-ae2c-48ee-b3aa-30d7abbeaff5";
        private const string SiteUrl = "https://mmoustafa.sharepoint.com/sites/Demo2/";
        private const string ReceiverUrl = "https://webhook20211.azurewebsites.net/api/Notifications?code=ubGveAGHF2sRFuSpYLR1IQir9Cw2699dqYbq8cUhLBa5S/bPd3wrfA==";

        [FunctionName("RemoveRemoteEventReceiver")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            RemoveRemoteEventReceiverBySpApp(log);
            //RemoveRemoteEventReceiversByAzureApp();
            return new OkObjectResult("Done");
        }

        private static void RemoveRemoteEventReceiverBySpApp(ILogger log)
        {
            try
            {
                using (var cc = new PnP.Framework.AuthenticationManager().GetACSAppOnlyContext(SiteUrl, AppId, AppSecret))
                {
                    var list = cc.Web.Lists.GetById(new System.Guid(ListId));
                    cc.ExecuteQuery();
                    list.Context.Load(list.EventReceivers);
                    list.Context.ExecuteQueryRetry();
                    foreach (var eventReceiver in list.EventReceivers.Where(w => w.ReceiverName.Contains("OpenText")))
                        list.EventReceivers.GetById(eventReceiver.ReceiverId).DeleteObject();
                    list.Context.ExecuteQueryRetry();
                };

            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
            log.LogInformation("C# HTTP trigger function processed a request.");
        }

        private static void RemoveRemoteEventReceiversByAzureApp()
        {
            var tenantId = "0d4ca527-dc44-43d1-84c1-b63d1b1e024d";
            var clientId = "850e0eec-9575-48b0-9fd2-5d57f9948ad3";
            X509Certificate2 certificate = new X509Certificate2("C:\\SharePoint\\mmoustafa.onmicrosoft.com.pfx", "Opentext1!", X509KeyStorageFlags.MachineKeySet);
            var auth = new PnP.Framework.AuthenticationManager(clientId, certificate, tenantId);
            using (var cc = auth.GetContext(SiteUrl))
            {
                var list = cc.Web.Lists.GetById(new System.Guid(ListId));
                cc.ExecuteQuery();
                list.Context.Load(list.EventReceivers);
                list.Context.ExecuteQueryRetry();
                foreach (var eventReceiver in list.EventReceivers.Where(w => w.ReceiverName.Contains("OpenText")))
                    list.EventReceivers.GetById(eventReceiver.ReceiverId).DeleteObject();
                list.Context.ExecuteQueryRetry();
            };
        }
    }
}
