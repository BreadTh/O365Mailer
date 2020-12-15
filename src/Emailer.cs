using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;

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

        public class Censors 
        {
            public string[] cookies;
            public string[] headers;
        }

        public async Task<bool> TrySendAspnetExceptionMail(
            string to
        ,   string envName
        ,   string serviceIdentifier
        ,   HttpContext httpContext
        ,   Exception exception,
            Censors censors = null)
        {
            censors ??= new Censors();
            censors.headers ??= new string[] { "Authorization" };
            censors.cookies ??= new string[] {  };

            string body;
            //If something has disposed of the body stream by accident 
            //(like disposing of a streamreader without leaveopen=true), this could throw.
            try
            {
                httpContext.Request.Body.Seek(0, SeekOrigin.Begin);
                using var bodyReader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
                body = await bodyReader.ReadToEndAsync();
                if(body == "")
                    body = "[Empty]";
            }
            catch(Exception bodyReadException)
            {
                body = $"[Body could not be read]\n{bodyReadException}";
            }

            List<string> headers = new List<string>();
            foreach(KeyValuePair<string, StringValues> headerKvp in httpContext.Request.Headers)
                if(headerKvp.Key == "Cookie")
                    continue;
                else if(censors.headers.Contains(headerKvp.Key))
                    headers.Add(HttpUtility.HtmlEncode($"{headerKvp.Key}: [CENSORED]"));
                else
                    headers.Add(HttpUtility.HtmlEncode($"{headerKvp.Key}: {headerKvp.Value}"));

            List<string> cookies = new List<string>();
            foreach(KeyValuePair<string, string> cookie in httpContext.Request.Cookies)
                cookies.Add(HttpUtility.HtmlEncode($"{cookie.Key}: {cookie.Value}"));
                        
            var cookieText = string.Join("</pre><pre>", cookies);
            if(cookieText == "")
                cookieText = "[None]";

            return TrySendMail(
                to
            ,   $"[{envName}] {serviceIdentifier}: Internal Error"
            ,       "<b>Url:</b><br><pre>" + HttpUtility.HtmlEncode(httpContext.Request.GetDisplayUrl())
                +   "</pre><br/><b>Method:</b><br/><pre>" + HttpUtility.HtmlEncode(httpContext.Request.Method)
                +   "</pre><br/><b>Client:</b><br/><pre>" 
                +   HttpUtility.HtmlEncode(httpContext.Connection.RemoteIpAddress + ":" + httpContext.Connection.RemotePort)
                +   "</pre><br/><b>Headers:</b><br/><pre>" + string.Join("</pre><pre>", headers)
                +   "</pre><br/><b>Cookies:</b><br/><pre>" + cookieText
                +   "</pre><br/><b>Body:</b><br><pre>" + HttpUtility.HtmlEncode(body)
                +   "</pre><br/><b>Exception:</b><br><pre>" + HttpUtility.HtmlEncode(exception)
                +   "</pre>");
        }
    }
}