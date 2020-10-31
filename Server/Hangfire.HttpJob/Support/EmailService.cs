using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace Hangfire.HttpJob.Support
{
    public class SmtpOptions
    {
        private static readonly ILog Logger = LogProvider.For<SmtpOptions>();
        public SmtpClient SmtpClient => lazySmtpClient().Value;

        private Lazy<SmtpClient> lazySmtpClient()
        {
            return new Lazy<SmtpClient>(InitSmtpClient);
        }
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; } = 25;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool UseSsl { get; set; } = false;

        public SmtpClient InitSmtpClient()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(this.Server)) return null;

                var client = new SmtpClient();
                client.Timeout = 5000;
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                if (!this.UseSsl)
                {
                    client.Connect(this.Server, this.Port, SecureSocketOptions.None);
                }
                else
                {
                    client.Connect(this.Server, this.Port, SecureSocketOptions.Auto);
                }

                client.AuthenticationMechanisms.Remove("XOAUTH2");

                if (!string.IsNullOrEmpty(this.User) && !string.IsNullOrEmpty(this.Password))
                {
                    client.Authenticate(this.User, this.Password);
                }

                return client;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.InitSmtpClient", ex);
                return null;
            }
        }
    }
    /// <summary>
    /// 邮件服务
    /// </summary>
    public class EmailService
    {
        private static readonly ILog Logger = LogProvider.For<EmailService>();
        private readonly SmtpOptions SmtpOptions;

        static EmailService InitEmailService()
        {
            return new EmailService(new SmtpOptions
            {
                Server = CodingUtil.HangfireHttpJobOptions.MailOption.Server,
                Port = CodingUtil.HangfireHttpJobOptions.MailOption.Port,
                UseSsl = CodingUtil.HangfireHttpJobOptions.MailOption.UseSsl,
                User = CodingUtil.HangfireHttpJobOptions.MailOption.User,
                Password = CodingUtil.HangfireHttpJobOptions.MailOption.Password
            });
        }

        public static EmailService Instance => lazySmtpClient().Value;

        private static Lazy<EmailService> lazySmtpClient()
        {
            return new Lazy<EmailService>(InitEmailService);
        }

        private EmailService()
        {
        }
      

        private EmailService(SmtpOptions _smtpOptions)
        {
            SmtpOptions = _smtpOptions;
        }

        /// <summary>
        /// send email with UTF-8
        /// </summary>
        /// <param name="mailTo">consignee email,multi split with ","</param>
        /// <param name="subject">subject</param>
        /// <param name="message">email message</param>
        /// <param name="isHtml">is set message as html</param>
        public void Send(string mailTo, string subject, string message, bool isHtml = true)
        {
            SendEmail(mailTo, null, null, subject, message, Encoding.UTF8, isHtml);
        }



        /// <summary>
        /// 发送错误邮件
        /// </summary>
        /// <param name="mailTo"></param>
        /// <param name="title"></param>
        /// <param name="body"></param>
        /// <param name="ex"></param>
        public void SendError(string mailTo, string title, string body, Exception ex)
        {

        }


        /// <summary>
        /// send email
        /// </summary>
        /// <param name="mailTo">consignee email,multi split with ","</param>
        /// <param name="subject">subject</param>
        /// <param name="message">email message</param>
        /// <param name="encoding">email message encoding</param>
        /// <param name="isHtml">is set message as html</param>
        public void Send(string mailTo, string subject, string message, Encoding encoding, bool isHtml = false)
        {
            SendEmail(mailTo, null, null, subject, message, encoding, isHtml);
        }

        /// <summary>
        /// send email with UTF-8
        /// </summary>
        /// <param name="mailTo">consignee email,multi split with ","</param>
        /// <param name="mailCc">send cc,multi split with ","</param>
        /// <param name="mailBcc">send bcc,multi split with ","</param>
        /// <param name="subject">subject</param>
        /// <param name="message">email message</param>
        /// <param name="isHtml">is set message as html</param>
        public void Send(string mailTo, string mailCc, string mailBcc, string subject, string message, bool isHtml = false)
        {
            SendEmail(mailTo, mailCc, mailBcc, subject, message, Encoding.UTF8, isHtml);
        }

        /// <summary>
        /// send email
        /// </summary>
        /// <param name="mailTo">consignee email,multi split with ","</param>
        /// <param name="mailCc">send cc,multi split with ","</param>
        /// <param name="mailBcc">send bcc,multi split with ","</param>
        /// <param name="subject">subject</param>
        /// <param name="message">email message</param>
        /// <param name="encoding">email message encoding</param>
        /// <param name="isHtml">is set message as html</param>
        public void Send(string mailTo, string mailCc, string mailBcc, string subject, string message, Encoding encoding, bool isHtml = false)
        {
            SendEmail(mailTo, mailCc, mailBcc, subject, message, encoding, isHtml);
        }


        /// <summary>
        /// send email with UTF-8 async
        /// </summary>
        /// <param name="mailTo">consignee email,multi split with ","</param>
        /// <param name="subject">subject</param>
        /// <param name="message">email message</param>
        /// <param name="isHtml">is set message as html</param>
        public Task SendAsync(string mailTo, string subject, string message, bool isHtml = false)
        {
            return Task.Factory.StartNew(() =>
            {
                SendEmail(mailTo, null, null, subject, message, Encoding.UTF8, isHtml);
            });
        }

        /// <summary>
        /// send email async
        /// </summary>
        /// <param name="mailTo">consignee email,multi split with ","</param>
        /// <param name="subject">subject</param>
        /// <param name="message">email message</param>
        /// <param name="encoding">email message encoding</param>
        /// <param name="isHtml">is set message as html</param>

        public Task SendAsync(string mailTo, string subject, string message, Encoding encoding, bool isHtml = false)
        {
            return Task.Factory.StartNew(() =>
            {
                SendEmail(mailTo, null, null, subject, message, encoding, isHtml);
            });
        }

        /// <summary>
        /// send email with UTF-8 async
        /// </summary>
        /// <param name="mailTo">consignee email,multi split with ","</param>
        /// <param name="mailCc">send cc,multi split with ","</param>
        /// <param name="mailBcc">send bcc,multi split with ","</param>
        /// <param name="subject">subject</param>
        /// <param name="message">email message</param>
        /// <param name="isHtml">is set message as html</param>
        public Task SendAsync(string mailTo, string mailCc, string mailBcc, string subject, string message, bool isHtml = false)
        {
            return Task.Factory.StartNew(() =>
            {
                SendEmail(mailTo, mailCc, mailBcc, subject, message, Encoding.UTF8, isHtml);
            });
        }

        /// <summary>
        /// send email async
        /// </summary>
        /// <param name="mailTo">consignee email,multi split with ","</param>
        /// <param name="mailCc">send cc,multi split with ","</param>
        /// <param name="mailBcc">send bcc,multi split with ","</param>
        /// <param name="subject">subject</param>
        /// <param name="message">email message</param>
        /// <param name="encoding">email message encoding</param>
        /// <param name="isHtml">is set message as html</param>
        public Task SendAsync(string mailTo, string mailCc, string mailBcc, string subject, string message, Encoding encoding, bool isHtml = false)
        {
            return Task.Factory.StartNew(() =>
            {
                SendEmail(mailTo, mailCc, mailBcc, subject, message, encoding, isHtml);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mailTo"></param>
        /// <param name="mailCc"></param>
        /// <param name="mailBcc"></param>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        /// <param name="encoding"></param>
        /// <param name="isHtml"></param>
        private void SendEmail(string mailTo, string mailCc, string mailBcc, string subject, string message, Encoding encoding, bool isHtml)
        {

            var _to = new string[0];
            var _cc = new string[0];
            var _bcc = new string[0];
            if (!string.IsNullOrEmpty(mailTo))
                _to = mailTo.Split(',').Select(x => x.Trim()).ToArray();
            if (!string.IsNullOrEmpty(mailCc))
                _cc = mailCc.Split(',').Select(x => x.Trim()).ToArray();
            if (!string.IsNullOrEmpty(mailBcc))
                _bcc = mailBcc.Split(',').Select(x => x.Trim()).ToArray();


            var mimeMessage = new MimeMessage();

            //add mail from
            mimeMessage.From.Add(new MailboxAddress("", SmtpOptions.User));

            //add mail to 
            foreach (var to in _to)
            {
                mimeMessage.To.Add(MailboxAddress.Parse(to));
            }

            //add mail cc
            foreach (var cc in _cc)
            {
                mimeMessage.Cc.Add(MailboxAddress.Parse(cc));
            }

            //add mail bcc 
            foreach (var bcc in _bcc)
            {
                mimeMessage.Bcc.Add(MailboxAddress.Parse(bcc));
            }

            //add subject
            mimeMessage.Subject = subject;

            //add email body
            TextPart body = null;

            if (isHtml)
            {

                body = new TextPart(TextFormat.Html);
            }
            else
            {
                body = new TextPart(TextFormat.Text);
            }
            //set email encoding
            body.SetText(encoding, message);

            //set email body
            mimeMessage.Body = body;

            using (var client = SmtpOptions.SmtpClient)
            {
                try
                {
                    client?.Send(mimeMessage);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("SmtpClient.SendEmail", ex);
                }
                finally
                {
                    client?.Disconnect(true);
                }
            }
        }
    }
}
