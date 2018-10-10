
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
            var storageHandler = new StorageHandler(logger);
            await storageHandler.ExtractAndUpload(zipStream, name);

            await SendEmail($"Extracted: {name}.zip", storageHandler.GetEmailReport(), messageCollector, logger);
        }

        private static async Task SendEmail(string subject, List<string> body, IAsyncCollector<SendGridMessage> messageCollector, Logger logger)
        {
            var emailToAddresses = System.Environment.GetEnvironmentVariable("EmailTo");
            var emailFromAddress = System.Environment.GetEnvironmentVariable("EmailFrom");

            if (!string.IsNullOrWhiteSpace(emailToAddresses))
            {
                logger.LogTrace("Preparing email notification");

                var message = new SendGridMessage();
                var content = string.Join("\n", body.ToArray());
                message.AddContent("text/plain", content);
                message.SetSubject(subject);
                message.SetFrom(emailFromAddress);

                foreach (var email in emailToAddresses.Split())
                {
                    message.AddTo(email);
                }

                logger.LogTrace($"Sending message to: {emailToAddresses}");
                logger.LogTrace($"Message contents:\n {content}");

                await messageCollector.AddAsync(message);
            }
        }
    }
}
