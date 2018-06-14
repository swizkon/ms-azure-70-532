using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;

using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace WalletSyncApp
{
    public static class DepositFunds
    {
        [FunctionName("DepositFunds")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req, [Table("Wallet", Connection = "AzureWebJobsStorage")]ICollector<Person> outTable, TraceWriter log)
        {
            string name = req.Query["name"];

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            // dynamic data = await req.Content.ReadAsAsync<object>();
            // string name = data?.name;

            if (name == null)
            {
                return new BadRequestObjectResult("Please pass a name in the request body");
            }

            outTable.Add(new Person()
            {
                PartitionKey = "Functions",
                RowKey = Guid.NewGuid().ToString(),
                Name = name
            });

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");



            // return req.CreateResponse(HttpStatusCode.Created);
        }

        public class Person : TableEntity
        {
            public string Name { get; set; }
        }
    }
}
