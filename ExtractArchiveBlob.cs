using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.WebJobs.Extensions.SendGrid;
using SendGrid.Helpers.Mail;
using System.Linq;
using System.Collections.Generic;

namespace GDH.ExtractArchiveBlob
{
    public static class ExtractArchiveBlob
    {
        [FunctionName("ExtractArchiveBlob")]
        public static async Task RunAsync(
            [BlobTrigger("arches/uploadedfiles/{name}.zip", Connection = "AzureWebJobsStorage")]Stream zipStream,
            [SendGrid(ApiKey = "SendGridApiKey")] IAsyncCollector<SendGridMessage> messageCollector,
            string name,
            ILogger log)
        {
            var logger = new Logger(log);
            var storageHandler = new StorageHandler(logger);
            await storageHandler.ExtractAndUpload(zipStream, name);

            await SendEmail($"Extracted: {name}.zip", storageHandler.GetEmailReport(), messageCollector, logger);
        }

        private static async Task SendEmail(string subject, IList<string> body, IAsyncCollector<SendGridMessage> messageCollector, Logger logger)
        {           
            var emailToAddresses = System.Environment.GetEnvironmentVariable("EmailTo");
            var emailFromAddress = System.Environment.GetEnvironmentVariable("EmailFrom");

            if (!string.IsNullOrWhiteSpace(emailToAddresses))
            {
                logger.LogInformation("Preparing email notification");

                var message = new SendGridMessage();
                var content = string.Join("\n", body.ToArray());
                message.AddContent("text/plain", content);
                message.SetSubject(subject);
                message.SetFrom(emailFromAddress);

                foreach (var email in emailToAddresses.Split())
                {
                    message.AddTo(email);
                }

                logger.LogInformation($"Sending message to: {emailToAddresses}");
                logger.LogInformation($"Message contents:\n {content}");

                await messageCollector.AddAsync(message);
            }
        }
    }

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
            _logger.LogInformation("Processing archive" +
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
            _logger.LogInformation($"Done processing blob: {archiveName}.zip");
        }

        public List<string> GetEmailReport()
        {
            return _logger.EmailReport;
        }
    }

    public class Logger
    {
        private ILogger _log;

        public List<string> EmailReport { get; set; }

        public Logger(ILogger log)
        {
            this._log = log;
            EmailReport = new List<string>();
        }

        public void LogInformation(string logMessage)
        {
            _log.LogInformation(logMessage);
            EmailReport.Add(logMessage);
        }

        public void LogTrace(string logMessage)
        {
            _log.LogTrace(logMessage);
        }
    }
}
