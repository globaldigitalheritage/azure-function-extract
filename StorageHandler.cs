using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace GDH.ExtractArchiveBlob
{
    public class StorageHandler
    {
        private Logger _logger;

        public StorageHandler(Logger logger)
        {
            _logger = logger;
        }

        public async Task ExtractAndUpload(Stream zipStream, string archiveName)
        {
            CloudStorageAccount storageAccount = GetStorageAccount();
            string containerName = GetContainerName();
            string outputFolder = GetOutputFolder(archiveName);

            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);

            await UploadZipContents(zipStream, outputFolder, container, archiveName);

        }

        private CloudStorageAccount GetStorageAccount()
        {
            var storageAccountConnectionString = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
            _logger.LogInformation($"Using storage account: {storageAccount.ToString()}");
            return storageAccount;
        }

        private string GetContainerName()
        {
            var containerName = System.Environment.GetEnvironmentVariable("OutputContainerName");
            _logger.LogInformation($"Output container: {containerName}");
            return containerName;
        }

        private string GetOutputFolder(string name)
        {
            var outputPrefix = System.Environment.GetEnvironmentVariable("OutputPrefix");
            var outputFolder = $"{outputPrefix}/{name}.zip_extracted";
            _logger.LogInformation($"Output folder: {outputFolder}");
            return outputFolder;
        }

        private async Task UploadZipContents(Stream zipStream, string outputFolder, CloudBlobContainer container, string archiveName)
        {
            _logger.LogInformation("Processing archive:" +
                $"\n Name:{archiveName} \n Size: {zipStream.Length} Bytes");

            using (ZipArchive zip = new ZipArchive(zipStream))
            {
                _logger.LogInformation($"Total files in archive: {zip.Entries.Count}");

                foreach (var entry in zip.Entries)
                {
                    _logger.LogTrace($"Processing: {entry.FullName}");

                    var blobPath = $"{outputFolder}/{entry.FullName}";

                    using (var entryStream = entry.Open())
                    {
                        if (entry.Length > 0)
                        {
                            _logger.LogTrace($"Uploading: {blobPath}");
                            var blob = container.GetBlockBlobReference(blobPath);
                            await blob.UploadFromStreamAsync(entryStream);
                        }
                    }
                }
            }
            _logger.LogTrace($"Done processing archive");
        }

        public List<string> GetEmailReport()
        {
            return _logger.EmailReport;
        }
    }
}