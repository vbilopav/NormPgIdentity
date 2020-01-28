using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace NormPgIdentity.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        private readonly string _nl = Environment.NewLine;

        public EmailSender(ILogger<EmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            _logger.LogDebug($"TO:{email}{_nl}SUBJECT:{subject}{_nl}MESSAGE:{message}{_nl}{_nl}");
            return Task.CompletedTask;
        }
    }
}
