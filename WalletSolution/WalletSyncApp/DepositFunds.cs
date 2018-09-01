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
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req, 
            [Table("XingZenDeposits", Connection = "AzureWebJobsStorage")] ICollector<Deposit> depositsTable, 
            [Queue("xingzenadjustbalance", Connection = "AzureWebJobsStorage")] ICollector<string> adjustBalanceQueue,
            TraceWriter log)
        {
            log.Verbose("FunctionName: DepositFunds");

            string wallet = req.Query["wallet"];

            string amount = req.Query["amount"];
            string currency = req.Query["currency"];

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            wallet = wallet ?? data?.wallet;
            amount = amount ?? data?.amount;
            currency = currency ?? data?.currency;


            if (wallet == null || amount == null || currency == null)
            {
                return new BadRequestObjectResult("Please pass wallet, amount and currency in the request body");
            }

            var deposit = new Deposit()
            {
                PartitionKey = "Wallet-" + wallet,
                RowKey = Guid.NewGuid().ToString(),
                Amount = Convert.ToDouble(amount),
                Currency = currency
            };

            depositsTable.Add(deposit);

            adjustBalanceQueue.Add(JsonConvert.SerializeObject(deposit));

            return new OkObjectResult($"Added {amount} {currency} to {wallet}");
        }

        public class Deposit : TableEntity
        {
            public double Amount { get; set; }

            public string Currency { get; set; }
        }
    }
}
