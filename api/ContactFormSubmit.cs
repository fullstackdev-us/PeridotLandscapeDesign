using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace PeridotFunctions
{
    public static class ContactFormSubmit
    {        
        [FunctionName("ContactFormSubmit")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            //for degbug
            var obj = new {
                smtpServer=config["smtpServer"],
                smtpPort=config["smtpPort"],
                smtpUserName=config["smtpUserName"],
                smtpPassword=config["smtpPassword"].Substring(0,5),
                smtpfromAddress=config["smtpfromAddress"],
                smtpTargetAddress=config["smtpTargetAddress"]
            };
            log.LogInformation(JsonSerializer.Serialize(obj));
            //end debug

            ContactFormModel? formData = null;
            var json = string.Empty;

            try {
                json = await req.ReadAsStringAsync();
                formData = JsonSerializer.Deserialize<ContactFormModel>(json);
            }
            catch (Exception e) {
                log.LogInformation(json);
                log.LogError(e, "Invalid Request");
            }

            if (formData == null) {
                log.LogError("No contact form data provided.");

                return new BadRequestObjectResult("Form data is required.");
            }

            try {
                SmtpClient client = new SmtpClient
                {
                    Host = config["smtpServer"],
                    Port = int.Parse(config["smtpPort"]),
                    UseDefaultCredentials = false,
                    EnableSsl = true
                };
                client.Credentials = new NetworkCredential(config["smtpUserName"], config["smtpPassword"]);

                // add from,to mailaddresses
                var fromTest = config["smtpfromAddress"];
                MailAddress from = new MailAddress(config["smtpFromAddress"], "PLD Contact Form");
                MailAddress to = new MailAddress(config["smtpTargetAddress"], "Peridot Landscape Design LLC");
                MailMessage myMail = new MailMessage(from, to)
                {
                    Subject = $"New PLD Contact Form Submission: {formData.Name}",
                    SubjectEncoding = System.Text.Encoding.UTF8,
                    Body = @$"
                        <h1>New Contact Request From {formData.Name}</h1>
                        <h2>Email:</h2>
                        <p>{formData.Email}</p>
                        <h2>Message:</h2>
                        <p>{formData.Body}</p>
                    ",
                    BodyEncoding = System.Text.Encoding.UTF8,
                    IsBodyHtml = true
                };

                client.Send(myMail);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to submit contact form.");
            }

            return new OkObjectResult("OK");
        }
    }
}
