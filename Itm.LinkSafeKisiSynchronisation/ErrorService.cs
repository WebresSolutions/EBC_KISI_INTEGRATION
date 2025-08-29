using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Itm.LinkSafeKisiSynchronisation;

/// <summary>
/// A scoped service that collects error logs and sends them via email when requested.
/// Implements IDisposable to ensure pending errors are sent before disposal.
/// </summary>
public class ErrorService : IDisposable
{

    /// <summary>
    /// Initializes a new instance of the ErrorService with email configuration.
    /// </summary>
    /// <param name="config">Configuration options for email settings</param>
    public ErrorService(IOptions<EmailConfig> config)
    {
        _config = config;
    }

    /// <summary>
    /// Gets or sets the collection of error logs with timestamps and messages.
    /// </summary>
    public List<(DateTime TimeStamp, string Message)> Content = new List<(DateTime timeStamp, string Message)>();
    private readonly IOptions<EmailConfig> _config;

    /// <summary>
    /// Adds an error message to the error log with an optional timestamp.
    /// </summary>
    /// <param name="message">The error message to log</param>
    /// <param name="timeStamp">Optional timestamp for the error (defaults to current UTC time)</param>
    public void AddErrorLog(string message, DateTime? timeStamp = null)
    {
        timeStamp ??= DateTime.UtcNow;
        Content.Add((timeStamp.Value, message));
    }

    /// <summary>
    /// Sends all collected error logs via email and clears the content.
    /// </summary>
    /// <returns>A task representing the asynchronous email operation</returns>
    public async Task Send()
    {
        // ToDo: send pending email
        if (Content.Count != 0)
        {
            var smtpClient = new SmtpClient(_config.Value.Smtp)
            {
                Port = 587,
                Credentials = new NetworkCredential(_config.Value.Username, _config.Value.Password),
                EnableSsl = true,
            };
            var mailMessage = new MailMessage
            {
                From = new MailAddress("dev@itm.dev"),
                Subject = "LinkSafe Kisi Synchronisation Error",
                Body = string.Join("/n", Content.Select(i => i.Message)),
                IsBodyHtml = true,
            };
            mailMessage.To.Add("recipient");

            smtpClient.Send(mailMessage);
            
            Content.Clear();
        }
    }

    /// <summary>
    /// Disposes the service and sends any pending error logs before cleanup.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Content.Count != 0)
        {
            Send().Wait();
        }
    }
}

/// <summary>
/// Configuration options for email settings used by the ErrorService.
/// </summary>
public class EmailConfig
{
    /// <summary>
    /// Gets or sets the SMTP server address for sending error emails.
    /// </summary>
    public string Smtp { get; set; }
    
    /// <summary>
    /// Gets or sets the username for SMTP authentication.
    /// </summary>
    public string Username { get; set; }
    
    /// <summary>
    /// Gets or sets the password for SMTP authentication.
    /// </summary>
    public string Password { get; set; }
}