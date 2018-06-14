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
    public static class AdjustBalance
    {
        [FunctionName("AdjustBalance")]
        public async static Task Run(
            [QueueTrigger("xingzenadjustbalance", Connection = "AzureWebJobsStorage")]string myQueueItem,
            [Table("XingZenFunds", Connection = "AzureWebJobsStorage")] CloudTable fundsTable,
            TraceWriter log)
        {
            log.Info($"AdjustBalance: {myQueueItem}");

            var deposit = JsonConvert.DeserializeObject<Deposit>(myQueueItem);

            log.Info($"Deposit: {JsonConvert.SerializeObject(deposit)}");

            var ok = await fundsTable.CreateIfNotExistsAsync();

            TableOperation operation = TableOperation.Retrieve<Fund>(deposit.PartitionKey, deposit.Currency);
            TableResult result = await fundsTable.ExecuteAsync(operation);

            if (result.Result != null)
            {
                var fund = (Fund)result.Result;
                fund.Balance += deposit.Amount;
                fund.ETag = "*";

                operation = TableOperation.Replace(fund);
                await fundsTable.ExecuteAsync(operation);
            }
            else
            {
                var fund = new Fund
                {
                    PartitionKey = deposit.PartitionKey,
                    RowKey = deposit.Currency,
                    Balance = deposit.Amount,
                    ETag = "*"
                };

                operation = TableOperation.Insert(fund);
                await fundsTable.ExecuteAsync(operation);
            }
        }
    }

    public class Deposit : TableEntity
    {
        public double Amount { get; set; }

        public string Currency { get; set; }
    }

    public class Fund : TableEntity
    {
        public double Balance { get; set; }
    }
}
