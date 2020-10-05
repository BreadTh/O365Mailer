using BreadTh.O365;

namespace BreadTh.O365Mailer.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            new Emailer("noreply@company.dk", "password123")
                .SendMail(
                    to: "person@othercompany.com"
                ,   subject: "test!"
                ,   body: @"Hey,<br/><br/>this is a test.");
        }
    }
}
