using System;
using System.Threading.Tasks;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using SQE.SqeHttpApi.DataAccess.Helpers;

namespace SQE.SqeHttpApi.Server.Helpers
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly IHostingEnvironment _env;

        public EmailSender(IConfiguration config, IHostingEnvironment env)
        {
            _config = config;
            _env = env;
        }
        
        /// <summary>
        /// Sends an email.  This uses the settings in appsettings.json to send the email via an external SMTP server.
        /// When this program is run in its docker container, the environment variables can be used to provide custom
        /// settings on container start (automatically done via startup.sh).  The environment variables are:
        /// MAILER_EMAIL_ADDRESS, MAILER_EMAIL_USERNAME, MAILER_EMAIL_PASSWORD, MAILER_EMAIL_SMTP_URL,
        /// MAILER_EMAIL_SMTP_PORT, MAILER_EMAIL_SMTP_SECURITY (this should be a string corresponding to one of the
        /// options in the SecureSocketOptions enum).
        /// </summary>
        /// <param name="email">Email address to send the message to.</param>
        /// <param name="subject"></param>
        /// <param name="htmlMessage"></param>
        /// <returns></returns>
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var senderEmail = _config.GetConnectionString("MailerEmailAddress");
            var user = _config.GetConnectionString("MailerEmailUsername");
            var pwd = _config.GetConnectionString("MailerEmailPassword");
            var smtp = _config.GetConnectionString("MailerEmailSmtpUrl");
            var port = _config.GetConnectionString("MailerEmailSmtpPort");
            var security = _config.GetConnectionString("MailerEmailSmtpSecurity");
            var securityEnum = (SecureSocketOptions)Enum.Parse(typeof(SecureSocketOptions), security);
            
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress("SQE Webadmin", senderEmail));
            
            // First catch any parsing errors for the submitted email address
            try
            {
                mimeMessage.To.Add(new MailboxAddress("Microsoft ASP.NET Core", email));
            }
            catch (ParseException)
            {
                // Throw a more helpful error when running in production
                if (_env.IsProduction())
                    throw new StandardErrors.EmailAddressImproperlyFormatted(email);

                throw;
            }
            
            mimeMessage.Subject = subject; //Subject  
            mimeMessage.Body = new TextPart("html") {Text = htmlMessage};

            using (var client = new SmtpClient())
            {
                try
                {
                    client.Connect(
                        smtp,
                        int.TryParse(port, out var intValue) ? intValue : 0,
                        securityEnum);
                    client.Authenticate(user, pwd);
                    client.Verify(email); // Check first if the email address is deliverable
                    await client.SendAsync(mimeMessage);
                    await client.DisconnectAsync(true);
                }
                catch (SmtpCommandException e)
                {
                    // If the status code indicates that the email address is undeliverable, throw a descriptive error
                    if (_env.IsProduction() && e.StatusCode == SmtpStatusCode.CannotVerifyUserWillAttemptDelivery)
                        throw new StandardErrors.EmailAddressUndeliverable(email);

                    throw;
                }
                catch (Exception)
                {
                    // Throw a less revealing error when running in production
                    if (_env.IsProduction())
                        throw new StandardErrors.EmailNotSent(email);

                    throw;
                }
            }
        }
    }
    
    // When running integration tests, we do not actually send out emails. Startup.cs looks at the ASPNETCORE_ENVIRONMENT
    // and if it is "IntegrationTests", then this Faker for IEmailSender is used instead of the real one.
    public class FakeEmailSender : IEmailSender
    {
        public FakeEmailSender(){}
        
        /// <summary>
        /// Sends an email.  This uses the settings in appsettings.json to send the email via an external SMTP server.
        /// When this program is run in its docker container, the environment variables can be used to provide custom
        /// settings on container start (automatically done via startup.sh).  The environment variables are:
        /// MAILER_EMAIL_ADDRESS, MAILER_EMAIL_USERNAME, MAILER_EMAIL_PASSWORD, MAILER_EMAIL_SMTP_URL,
        /// MAILER_EMAIL_SMTP_PORT, MAILER_EMAIL_SMTP_SECURITY (this should be a string corresponding to one of the
        /// options in the SecureSocketOptions enum).
        /// </summary>
        /// <param name="email">Email address to send the message to.</param>
        /// <param name="subject"></param>
        /// <param name="htmlMessage"></param>
        /// <returns></returns>
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            await Task.CompletedTask;
        }
    }
}