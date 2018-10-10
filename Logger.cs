using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace GDH.ExtractArchiveBlob
{
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
            EmailReport.Add(logMessage + "\n");
        }

        public void LogTrace(string logMessage)
        {
            _log.LogTrace(logMessage);
        }
    }
}