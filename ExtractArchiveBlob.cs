using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace GDH.ExtractArchiveBlob
{
    public static class ExtractArchiveBlob
    {
        [FunctionName("ExtractArchiveBlob")]
        public static void Run([BlobTrigger("arches/uploadedfiles/{name}.zip", Connection = "AzureWebJobsStorage")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
        }
    }
}
