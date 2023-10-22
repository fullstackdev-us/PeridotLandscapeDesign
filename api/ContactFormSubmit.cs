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

            ContactFormModel? formData = null;
            var json = string.Empty;

            try {
                json = await req.ReadAsStringAsync();
                log.LogInformation("New submission: "+json);
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
                SmtpClient client = new SmtpClient(config["smtpServer"])
                {
                    Port = int.Parse(config["smtpPort"]),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = new NetworkCredential(config["smtpUserName"], config["smtpPassword"]),
                    EnableSsl = true
                };

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
                        <p>{formData.Message}</p>
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
