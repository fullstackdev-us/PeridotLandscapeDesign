using System;
using System.IO;
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
                SmtpClient mySmtpClient = new SmtpClient(config["smtpServer"])
                {
                    Port = int.Parse(config["smtpPort"]),
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(config["smtpUserName"], config["smtpPassword"])
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
                        <p>{formData.Body}</p>
                    ",
                    BodyEncoding = System.Text.Encoding.UTF8,
                    IsBodyHtml = true
                };

                mySmtpClient.Send(myMail);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to submit contact form.");
            }
            // log.LogInformation("C# HTTP trigger function processed a request.");

            // string name = req.Query["name"];

            // string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            // dynamic data = JsonConvert.DeserializeObject(requestBody);
            // name = name ?? data?.name;

            // string responseMessage = string.IsNullOrEmpty(name)
            //     ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            //     : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult("OK");
        }
    }
}
