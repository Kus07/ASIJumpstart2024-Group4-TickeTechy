using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using System;

namespace ASI.Basecode.Services.Manager
{
    public class MailManager
    {
        private readonly string _mailSender;
        private readonly string _mailAppPassword;

        public MailManager(IConfiguration configuration)
        {
            _mailSender = configuration["EmailSettings:MailSender"];
            _mailAppPassword = configuration["EmailSettings:MailSenderAppPassword"];
        }

        public bool SendEmail(string recipient, string subject, string msgBody, ref string errResponse)
        {
            try
            {
                using (var message = new MailMessage())
                {
                    var smtp = new SmtpClient
                    {
                        Port = 587,
                        Host = "smtp.gmail.com",
                        EnableSsl = true,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(_mailSender, _mailAppPassword),
                        DeliveryMethod = SmtpDeliveryMethod.Network
                    };

                    message.From = new MailAddress(_mailSender, "TickeTechy");
                    message.To.Add(new MailAddress(recipient));
                    message.Subject = subject;
                    message.IsBodyHtml = true;
                    message.Body = msgBody;

                    smtp.Send(message);
                    errResponse = "Message Sent";
                    return true;
                }
            }
            catch (Exception ex)
            {
                errResponse = ex.Message;
                return false;
            }
        }

        public bool SendWelcomeEmail(string recipientEmail, string firstName, string username, string password, string role, ref string errResponse)
        {
            string welcomeTemplate = $@"<!DOCTYPE html>
                    <html lang='en'>
                    <head>
                        <meta charset='UTF-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <title>Welcome to TickeTechy</title>
                    </head>
                    <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0;'>
                        <div style='max-width: 600px; margin: 20px auto; background-color: #fff; border-radius: 10px; overflow: hidden;'>
                            <div style='background-color: #e1f4fe; color: #333; text-align: center; padding: 20px 0;'>
                                <h1 style='margin: 0; font-size: 24px;'>Welcome to TickeTechy!</h1>
                            </div>
                            <div style='padding: 20px;'>
                                <p style='font-size: 16px;'>Hi, {firstName}! Welcome to TickeTechy! You have successfully registered as a {role}.</p>
                                <p style='font-size: 16px;'>Your username is <strong>{username}</strong> and your default password is <strong>{password}</strong>.</p>
                                <p style='font-size: 16px;'>Thank you for choosing TickeTechy for your ticketing solutions!</p>
                            </div>
                        </div>
                    </body>
                    </html>";

            welcomeTemplate = welcomeTemplate.Replace("{firstName}", firstName).Replace("{role}", role)
                                     .Replace("{username}", username)
                                     .Replace("{password}", password);

            return SendEmail(recipientEmail, "Welcome to TickeTechy", welcomeTemplate, ref errResponse);
        }

        public bool SendOtpForgotPassword(string recipientEmail, string firstName, string otpCode, ref string errResponse)
        {
            string welcomeTemplate = $@"<!DOCTYPE html>
                    <html lang='en'>
                    <head>
                        <meta charset='UTF-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <title>Forgot password?</title>
                    </head>
                    <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0;'>
                        <div style='max-width: 600px; margin: 20px auto; background-color: #fff; border-radius: 10px; overflow: hidden;'>
                            <div style='background-color: #e1f4fe; color: #333; text-align: center; padding: 20px 0;'>
                                <h1 style='margin: 0; font-size: 24px;'>TickeTechy!</h1>
                            </div>
                            <div style='padding: 20px;'>
                                <p style='font-size: 16px;'>Hi, {firstName}! So... you have forgotten your password?</p>
                                <p style='font-size: 16px;'>Here is the OTP code, copy this into the OTP input box and submit it and you can change your password: <strong>{otpCode}</strong>.</p>
                                <p style='font-size: 16px;'>Do not forget your password next time!</p>
                            </div>
                        </div>
                    </body>
                    </html>";

            welcomeTemplate = welcomeTemplate.Replace("{firstName}", firstName)
                                     .Replace("{otpCode}", otpCode);

            return SendEmail(recipientEmail, "TickeTechy - Forgot Password", welcomeTemplate, ref errResponse);
        }


        public bool EmailRespond(string recipientEmail, string firstName, int ticketId, string ticketMessage, ref string errResponse)
        {
            string respondTemplate = $@"<!DOCTYPE html>
            <html lang='en'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Ticket Message from TickeTechy</title>
            </head>
            <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0;'>
                <div style='max-width: 600px; margin: 20px auto; background-color: #fff; border-radius: 10px; overflow: hidden;'>
                    <div style='background-color: #e1f4fe; color: #333; text-align: center; padding: 20px 0;'>
                        <h1 style='margin: 0; font-size: 24px;'>Message for Ticket #{ticketId}</h1>
                    </div>
                    <div style='padding: 20px;'>
                        <p style='font-size: 16px;'>Hi, {firstName}!</p>
                        <p style='font-size: 16px;'>You have received a new message for Ticket #{ticketId}:</p>
                        <blockquote style='font-size: 16px; color: #333;'>{ticketMessage}</blockquote>
                        <p style='font-size: 16px;'>Thank you for using TickeTechy for your ticketing solutions!</p>
                    </div>
                </div>
            </body>
            </html>";

            return SendEmail(recipientEmail, $"New Message for Ticket #{ticketId}", respondTemplate, ref errResponse);
        }

        public bool ResolveNotif(string recipientEmail, string firstName, int ticketId, string ticketMessage, ref string errResponse)
        {
            string ResolveTemplate = $@"<!DOCTYPE html>
            <html lang='en'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Ticket Message from TickeTechy</title>
            </head>
            <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0;'>
                <div style='max-width: 600px; margin: 20px auto; background-color: #fff; border-radius: 10px; overflow: hidden;'>
                    <div style='background-color: #e1f4fe; color: #333; text-align: center; padding: 20px 0;'>
                        <h1 style='margin: 0; font-size: 24px;'>Message for Ticket #{ticketId}</h1>
                    </div>
                    <div style='padding: 20px;'>
                        <p style='font-size: 16px;'>Hi, {firstName}!</p>
                        <p style='font-size: 16px;'>Please Confirm Ticket#{ticketId}, if your problem has been resolved.</p>
                        <p style='font-size: 16px;'>Please login to the system to confirm!</p>
                        <p style='font-size: 16px;'>Thank you for using TickeTechy for your ticketing solutions!</p>
                    </div>
                </div>
            </body>
            </html>";

            return SendEmail(recipientEmail, $"New Message for Ticket #{ticketId}", ResolveTemplate, ref errResponse);
        }
        public bool ResolveNotifToClient(string recipientEmail, string firstName, int ticketId, string ticketMessage, ref string errResponse)
        {
            string ResolveTemplate = $@"<!DOCTYPE html>
            <html lang='en'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Ticket Message from TickeTechy</title>
            </head>
            <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0;'>
                <div style='max-width: 600px; margin: 20px auto; background-color: #fff; border-radius: 10px; overflow: hidden;'>
                    <div style='background-color: #e1f4fe; color: #333; text-align: center; padding: 20px 0;'>
                        <h1 style='margin: 0; font-size: 24px;'>Message for Ticket #{ticketId}</h1>
                    </div>
                    <div style='padding: 20px;'>
                        <p style='font-size: 16px;'>Hi, {firstName}!</p>
                        <p style='font-size: 16px;'>The user confirmed the problem has been resolved </p>
                        <p style='font-size: 16px;'>Thank you for using TickeTechy for your ticketing solutions!</p>
                    </div>
                </div>
            </body>
            </html>";

            return SendEmail(recipientEmail, $"New Message for Ticket #{ticketId}", ResolveTemplate, ref errResponse);
        }

        public bool AssignedToAgentEmail(string recipientEmail, string firstName, string description, ref string errResponse)
        {
            string ResolveTemplate = $@"<!DOCTYPE html>
            <html lang='en'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Ticket Assigned to you from TickeTechy</title>
            </head>
            <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0;'>
                <div style='max-width: 600px; margin: 20px auto; background-color: #fff; border-radius: 10px; overflow: hidden;'>
                    <div style='background-color: #e1f4fe; color: #333; text-align: center; padding: 20px 0;'>
                        <h1 style='margin: 0; font-size: 24px;'>A new ticket has arrived!</h1>
                    </div>
                    <div style='padding: 20px;'>
                        <p style='font-size: 16px;'>Hi, Agent {firstName}!</p>
                        <p style='font-size: 16px;'>A ticket has been assigned to you!</p>                        
                        <p style='font-size: 16px;'>Ticket Description: {description}</p>
                        <p style='font-size: 16px;'>Thank you for using TickeTechy for your ticketing solutions!</p>
                    </div>
                </div>
            </body>
            </html>";

            return SendEmail(recipientEmail, $"Ticket Assigned to you", ResolveTemplate, ref errResponse);
        }
    }
}
