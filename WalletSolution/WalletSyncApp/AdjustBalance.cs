using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace WalletSyncApp
{
    public static class AdjustBalance
    {
        [FunctionName("AdjustBalance")]
        public async static Task Run(
            [QueueTrigger("xingzenadjustbalance", Connection = "AzureWebJobsStorage")]string adjustBalanceItem,
            [Table("XingZenFunds", Connection = "AzureWebJobsStorage")] CloudTable fundsTable,
            [Queue("XingZenWebCallWorkerQueue", Connection = "AzureWebJobsStorage")] ICollector<string> webCallWorkerQueue,
            TraceWriter log)
        {
            log.Info($"AdjustBalance: {adjustBalanceItem}");

            var deposit = JsonConvert.DeserializeObject<Deposit>(adjustBalanceItem);

            log.Info($"Deposit: {JsonConvert.SerializeObject(deposit)}");

            var ok = await fundsTable.CreateIfNotExistsAsync();

            TableOperation operation = TableOperation.Retrieve<Fund>(deposit.PartitionKey, deposit.Currency);
            TableResult result = await fundsTable.ExecuteAsync(operation);

            double newBalance = 0.0;

            if (result.Result != null)
            {
                var fund = (Fund)result.Result;
                fund.Balance += deposit.Amount;
                fund.ETag = fund.ETag;

                operation = TableOperation.Replace(fund);
                await fundsTable.ExecuteAsync(operation);
                newBalance = fund.Balance;
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
                newBalance = fund.Balance;
            }

            var wc = new WebCall() { Url = $"http://localhost:5000/transactions/notifyStoreBalance?storeId={deposit.PartitionKey.Replace("Wallet-", "")}&balance={newBalance}&currency={deposit.Currency}" };

            webCallWorkerQueue.Add(JsonConvert.SerializeObject(wc));
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


    /*
    public class WebCall
    {
        public string Url { get; set; }

        public string Method { get; set; }

        public string Data { get; set; }
    }
    */
}
