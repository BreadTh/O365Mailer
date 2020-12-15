using System;
using System.Net;
using System.Net.Mail;

namespace BreadTh.O365
{
    public class Emailer
    {
        private readonly NetworkCredential _networkCredentials;

        public Emailer(string sender, string password)
        {
            if (string.IsNullOrWhiteSpace(sender) || string.IsNullOrWhiteSpace(password))
                throw new ArgumentException($"You must provide a valid sender email address and password for BreadTh.O365Mailer.Emailer. Sender was {sender}.");

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            _networkCredentials = new NetworkCredential(sender, password);
        }

        public bool TrySendMail(string to, string subject, string body)
        {

            try
            {
                MailMessage message = new MailMessage()
                {   From = new MailAddress(_networkCredentials.UserName)
                ,   Subject = subject
                ,   Body = body
                ,   IsBodyHtml = true
                };

                message.To.Add(new MailAddress(to));

                using(SmtpClient smtpClient = new SmtpClient()
                {   UseDefaultCredentials = false
                ,   Credentials = _networkCredentials
                ,   Host = "smtp.office365.com"
                ,   Port = 587
                ,   DeliveryMethod = SmtpDeliveryMethod.Network
                ,   EnableSsl = true
                })
                    smtpClient.Send(message);
                
                return true;
            }
            catch(Exception e)
            {
                return false;    
            }
            
        }

        public bool IsValidEmailAddress(string emailAddressCandidate)
        {
            try
            {
                _ = new MailAddress(emailAddressCandidate);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryParseEmailAddress(string emailAddressCandidate, out MailAddress result)
        {
            try
            {
                result = new MailAddress(emailAddressCandidate);

                //RFC 1035, section 3.1 + RFC 5321, section 2.3.11: Domain part isn't case sensitive, but user part may be - it's up to the individual mail providers.
                result = new MailAddress(string.Format("{0}@{1}", result.User, result.Host.ToLower()));
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
    }
}