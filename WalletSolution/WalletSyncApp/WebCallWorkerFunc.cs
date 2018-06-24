using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace WalletSyncApp
{
    public static class WebCallWorkerFunc
    {
        static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("PerformWebCall")]
        public async static Task Run(
            [QueueTrigger("XingZenWebCallWorkerQueue", Connection = "AzureWebJobsStorage")]string webCallData,
            TraceWriter log)
        {
            var webCall = JsonConvert.DeserializeObject<WebCall>(webCallData);

            log.Info($"Deposit: {JsonConvert.SerializeObject(webCall)}");

            await httpClient.PostAsync(webCall.Url, new StringContent(""));
        }
    }

    public class WebCall
    {
        public string Url { get; set; }

        public string Method { get; set; }

        public string Data { get; set; }
    }
}
