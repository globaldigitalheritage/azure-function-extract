using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace GDH.ExtractArchiveBlob
{
    public static class ExtractArchiveBlob
    {
        [FunctionName("ExtractArchiveBlob")]
        public static async Task RunAsync([BlobTrigger("arches/uploadedfiles/{name}.zip", Connection = "AzureWebJobsStorage")]Stream zipStream, string name, ILogger log)
        {
            log.LogInformation($"Processing blob\n Name:{name} \n Size: {zipStream.Length} Bytes");

            var storageAccountConnectionString = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);

            var containerName = System.Environment.GetEnvironmentVariable("TargetContainerName");
            log.LogInformation($"Target container: {containerName}");

            var blobPrefix = System.Environment.GetEnvironmentVariable("BlobPrefix");
            var outputFolder = $"{blobPrefix}/{name}";
            log.LogInformation($"Output folder: {outputFolder}");

            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);

            using (ZipArchive zip = new ZipArchive(zipStream)){
                foreach(var entry in zip.Entries){
                    var blobPath = $"{outputFolder}/{entry.FullName}";
                    var blob = container.GetBlockBlobReference(blobPath);
                    using (var entryStream = entry.Open()){
                        if (entry.Length > 0){
                            log.LogTrace("Uploading: blobPath");
                            await blob.UploadFromStreamAsync(entryStream);
                        }
                    }
                }
            }

            log.LogInformation($"Done processing blob: {name}");
        }
    }
}
