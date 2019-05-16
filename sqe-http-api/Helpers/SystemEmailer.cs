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
        /// Sends an email
        /// </summary>
        /// <param name="email">Email address to send the message to.</param>
        /// <param name="subject"></param>
        /// <param name="htmlMessage"></param>
        /// <returns></returns>
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
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