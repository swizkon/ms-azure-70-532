using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace WalletSyncApp
{
    public static class AdjustBalance
    {
        [FunctionName("AdjustBalance")]
        public static void Run(
            [QueueTrigger("xingzenadjustbalance", Connection = "AzureWebJobsStorage")]string myQueueItem, 
            TraceWriter log)
        {
            log.Info($"C# AdjustBalance: {myQueueItem}");
        }
    }
}
