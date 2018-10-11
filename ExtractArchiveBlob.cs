
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;

namespace GDH.ExtractArchiveBlob
{
    public static class ExtractArchiveBlob
    {
        [FunctionName("ExtractArchiveBlob")]
        public static async Task RunAsync(
            [BlobTrigger("%InputContainerName%/%InputPrefix%/{name}.zip", Connection = "AzureWebJobsStorage")]Stream zipStream,
            [SendGrid(ApiKey = "SendGridApiKey")] IAsyncCollector<SendGridMessage> messageCollector,
            string name,
            ILogger log)
        {
            var logger = new Logger(log);
            string emailSubject = "";
            string emailBody = "";

            try
            {
                var storageHandler = new StorageHandler(logger);
                await storageHandler.ExtractAndUpload(zipStream, name);
                emailSubject = $"Extracted: {name}.zip";
                emailBody = storageHandler.GetEmailReport();
            }
            catch (Exception e)
            {
                emailSubject = $"Extraction job failed: {name}.zip";
                emailBody = e.ToString();
            }
            finally
            {
                await SendEmail(emailSubject, emailBody, messageCollector, logger);
            }
        }

        private static async Task SendEmail(string subject, string body, IAsyncCollector<SendGridMessage> messageCollector, Logger logger)
        {
            var emailToAddresses = System.Environment.GetEnvironmentVariable("EmailTo");
            var emailFromAddress = System.Environment.GetEnvironmentVariable("EmailFrom");

            if (!string.IsNullOrWhiteSpace(emailToAddresses))
            {
                logger.LogTrace("Preparing email notification");

                var message = new SendGridMessage();
                message.AddContent("text/plain", body);
                message.SetSubject(subject);
                message.SetFrom(emailFromAddress);

                foreach (var email in emailToAddresses.Split())
                {
                    message.AddTo(email);
                }

                logger.LogTrace($"Sending message to: {emailToAddresses}");
                logger.LogTrace($"Message contents:\n {body}");

                await messageCollector.AddAsync(message);
            }
        }
    }
}
