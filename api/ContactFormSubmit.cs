using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace PeridotFunctions
{
    public static class ContactFormSubmit
    {
        [FunctionName("ContactFormSubmit")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            ContactFormModel? formData = await JsonSerializer.DeserializeAsync<ContactFormModel>(req.Body);

            if (formData == null) {
                _logger.LogError("No contact form data provided.");

                return;
            }

            try {
                SmtpClient mySmtpClient = new SmtpClient(_configuration["smtpServer"])
                {
                    Port = int.Parse(_configuration["smtpPort"]),
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_configuration["smtpUserName"], _configuration["smtpPassword"])
                };

                // add from,to mailaddresses
                var fromTest = _configuration["smtpfromAddress"];
                MailAddress from = new MailAddress(_configuration["smtpFromAddress"], "PLD Contact Form");
                MailAddress to = new MailAddress(_configuration["smtpTargetAddress"], "Peridot Landscape Design LLC");
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
                _logger.LogError(ex, "Failed to submit contact form.");
            }
            // log.LogInformation("C# HTTP trigger function processed a request.");

            // string name = req.Query["name"];

            // string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            // dynamic data = JsonConvert.DeserializeObject(requestBody);
            // name = name ?? data?.name;

            // string responseMessage = string.IsNullOrEmpty(name)
            //     ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            //     : $"Hello, {name}. This HTTP triggered function executed successfully.";

            // return new OkObjectResult(responseMessage);
        }
    }
}
