using System;
using System.Threading.Tasks;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace SQE.SqeHttpApi.Server.Helpers
{
    public class DevEmailSender : IEmailSender
    {
        /// <summary>
        /// Sends an email.  This relies on the environment variables SQE_GMAIL_USERNAME and SQE_GMAIL_PASSWORD.
        /// If you do not set these, the function will error.
        /// TODO: Figure out what email server we might use in production.  Alter this to be more flexible regarding
        /// email provider and account.
        /// </summary>
        /// <param name="email">Email address to send the message to.</param>
        /// <param name="subject"></param>
        /// <param name="htmlMessage"></param>
        /// <returns></returns>
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Don't bother trying to send an email unless we have an email username and password
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SQE_GMAIL_USERNAME")) &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SQE_GMAIL_PASSWORD")))
                return;
            
            try
            {
                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress
                ("SQE Webadmin",
                    Environment.GetEnvironmentVariable("SQE_GMAIL_USERNAME") + "@gmail.com"
                ));
                mimeMessage.To.Add(new MailboxAddress
                ("Microsoft ASP.NET Core",
                    email
                ));
                mimeMessage.Subject = subject; //Subject  
                mimeMessage.Body = new TextPart("html")
                {
                    Text = htmlMessage
                };

                using (var client = new SmtpClient())
                {
                    client.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTlsWhenAvailable);
                    client.Authenticate(
                        Environment.GetEnvironmentVariable("SQE_GMAIL_USERNAME"),
                        Environment.GetEnvironmentVariable("SQE_GMAIL_PASSWORD")
                    );
                    await client.SendAsync(mimeMessage);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}