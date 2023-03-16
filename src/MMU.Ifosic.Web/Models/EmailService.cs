using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MMU.Ifosic.Models;

public static class EmailExtension
{
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration config)
        => services.AddTransient(p => new EmailService(config));
}

public class EmailOptions
{
    public const string Section = "Email";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 25;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "";
    public SecureSocketOptions Security
        => Port == 587 ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
}

public class EmailService
{
    public EmailOptions Options { get; } = new();
    private readonly MailboxAddress? _from;
    private readonly MimeMessage _message = new();
    private readonly BodyBuilder _body = new();

    public EmailService(IConfiguration cfg)
    {
        cfg.GetSection(EmailOptions.Section).Bind(Options);
        if (Options.From is null || !MailboxAddress.TryParse(Options.From, out var from))
            return;
        _from = from;
        _message.From.Add(_from);
    }

    public void AddAttachment(string fileName) => _body.Attachments.Add(fileName);

    public void AddReplyTo(params MailboxAddress[] replyto) => _message.ReplyTo.AddRange(replyto);

    public async Task SendAsync(string subject, string body, params string[] to)
    {
        if (to is null || to.Length == 0)
            return;
        var address = new List<MailboxAddress>();
        for (int i = 0; i < to.Length; i++)
        {
            if (MailboxAddress.TryParse(to[i], out var toA))
                address.Add(toA);
        }
        await SendAsync(subject, body, address.ToArray());
    }

    public async Task SendAsync(string subject, string body, params MailboxAddress[] to)
    {
        if (to is null || to?.Length < 1)
            return;
        if (to?.Length > 0)
            _message.To.AddRange(to);
        _body.HtmlBody = body;
        _message.Subject = subject;
        _message.Body = _body.ToMessageBody();
        await SendAsync();
    }

    public async Task SendAsync()
    {
        if (Options.From is null)
            return;
        using var client = new SmtpClient();
        if (Options.Security != SecureSocketOptions.None)
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        await client.ConnectAsync(Options.Host, Options.Port, Options.Security);
        if (!string.IsNullOrEmpty(Options.Username))
        {
            await client.AuthenticateAsync(Options.Username, Options.Password);
        }
        await client.SendAsync(_message);
        await client.DisconnectAsync(true);
    }
}