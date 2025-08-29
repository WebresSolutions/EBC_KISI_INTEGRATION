using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Itm.LinkSafeKisiSynchronisation;

/// <summary>
/// a scoped service
/// </summary>
public class ErrorService : IDisposable
{

    public ErrorService(IOptions<EmailConfig> config)
    {
        _config = config;
    }

    public List<(DateTime TimeStamp, string Message)> Content = new List<(DateTime timeStamp, string Message)>();
    private readonly IOptions<EmailConfig> _config;

    public void AddErrorLog(string message, DateTime? timeStamp = null)
    {
        timeStamp ??= DateTime.UtcNow;
        Content.Add((timeStamp.Value, message));
    }

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

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Content.Count != 0)
        {
            Send().Wait();
        }
    }
}


public class EmailConfig
{
    public string Smtp { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}