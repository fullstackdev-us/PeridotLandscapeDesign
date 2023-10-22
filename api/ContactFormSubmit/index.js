const nodeMailer = require('nodemailer');

module.exports = async function (context, req) {
    var formData = req.body;

    var transporter = nodemailer.createTransport({
        service: 'gmail',
        auth: {
          user: process.env['smtpUsername'],
          pass: process.env['smtpPassword']
        }
      });
      
      var mailOptions = {
        from: process.env['smtpFromAddress'],
        to: process.env['smtpTargetAddress'],
        subject: `New PLD Contact Form Submission: ${formData.name}`,
        html: `
            <h1>New Contact Request From ${formData.name}</h1>
            <h2>Email:</h2>
            <p>${formData.email}</p>
            <h2>Message:</h2>
            <p>${formData.message}</p>
        `
      };
      
      transporter.sendMail(mailOptions, function(error, info) {
        if (error) {
            context.log(error);
        } else {
            context.log('Email sent: ' + info.response);
        }
      });

    // context.log('JavaScript HTTP trigger function processed a request.');

    // const name = (req.query.name || (req.body && req.body.name));
    // const responseMessage = name
    //     ? "Hello, " + name + ". This HTTP triggered function executed successfully."
    //     : "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response.";

    context.res = {
        // status: 200, /* Defaults to 200 */
        body: "Success"
    };
}