using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Collections;

namespace EMailManejador
{
    public class EmailSmtp
    {
        public string ultimoMensaje = "";
        private SmtpClient client = null;
        public EmailSmtp(string smtp, int port, string user, string pwd, bool enableSsl)
        {
            // create smtp client at mail server location
            client = new SmtpClient(smtp);
            client.Port = port;             // 587 gmail;
            client.UseDefaultCredentials = true;
            if (!user.Equals(string.Empty))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential(user, pwd);
            }
            client.EnableSsl = enableSsl;   //true gmail
        }
        /// <summary>
        /// Envía mensaje email con o sin adjunto
        /// </summary>
        /// <param name="sendTo">Dirección email destinatario</param>
        /// <param name="sendFrom">Dirección email remitente</param>
        /// <param name="sendSubject">Asunto del email</param>
        /// <param name="sendMessage">Mensaje del email</param>
        /// <param name="attachments">Array de strings que apunta a la ruta de cada adjunto</param>
        /// <returns>True si envió sin problemas</returns>
        public bool SendMessage(string sendTo, string sendFrom, string sendSubject, string sendMessage, 
                            string carbonCopy, string blindCarbonCopy, string replyToEmail, List<string> attachments)
        {
            try
            {
                // if the email address is bad, return message
                if (!ValidateEmailAddress(sendTo))
                {
                    ultimoMensaje = "Dirección email inválida del destinatario: " + sendTo;
                    return false;
                }

                // if the email address is bad, return message
                if (!carbonCopy.Equals(string.Empty))
                    if (!ValidateEmailAddress(carbonCopy))
                    {
                        ultimoMensaje = "Dirección email inválida del CC: " + carbonCopy;
                        return false;
                    }

                // if the email address is bad, return message
                if (!blindCarbonCopy.Equals(string.Empty))
                    if (!ValidateEmailAddress(blindCarbonCopy))
                    {
                        ultimoMensaje = "Dirección email inválida del BCC: " + carbonCopy;
                        return false;
                    }

                // if the email address is bad, return message
                if (!replyToEmail.Equals(string.Empty))
                    if (!ValidateEmailAddress(replyToEmail))
                    {
                        ultimoMensaje = "Dirección email inválida de Reply To: " + replyToEmail;
                        return false;
                    }

                // create the email message
                using (MailMessage message = new MailMessage(sendFrom, sendTo, sendSubject, sendMessage))
                {

                    // Add a carbon copy recipient.
                    if (!carbonCopy.Equals(string.Empty))
                        message.CC.Add(carbonCopy);

                    // Add a blind carbon copy recipient.
                    if (!blindCarbonCopy.Equals(string.Empty))
                        message.Bcc.Add(blindCarbonCopy);

                    // Agregar replyTo
                    if (!replyToEmail.Equals(string.Empty))
                        message.ReplyTo = new MailAddress(replyToEmail);

                    // The attachments array should point to a file location where
                    // the attachment resides - add the attachments to the message
                    if (attachments.Count > 0)
                        foreach (string attach in attachments)
                        {
                            Attachment attached = new Attachment(attach, MediaTypeNames.Application.Octet);
                            message.Attachments.Add(attached);
                        }
                    client.Send(message);
                }
                ultimoMensaje = "";
                return true; //"Message sent to " + sendTo + " at " + DateTime.Now.ToString() + ".";
            }
            catch (Exception ex)
            {
                ultimoMensaje = "Error al enviar email. Contacte a su administrador. [SendMessage] De:" +
                                sendFrom + " A:" + sendTo + " " + ex.Message.ToString();
                return false;
            }
        }

        /// <summary>
        /// Confirma la validez del formato de la dirección email
        /// </summary>
        /// <param name="emailAddress">Dirección de email completa para validar</param>
        /// <returns>True si la dirección es válida</returns>
        public bool ValidateEmailAddress(string emailAddress)
        {
            try
            {
                ultimoMensaje = "";
                string TextToValidate = emailAddress;
                Regex expression = new Regex(@"\w+@[a-zA-Z_]+?\.[a-zA-Z]{2,3}");

                // test email address with expression
                return (expression.IsMatch(TextToValidate));
            }
            catch (Exception ve)
            {
                ultimoMensaje = "Error al validar dirección email. [ValidateEmailAddress] " + ve.Message.ToString();
                return false;
            }
        }
    }
}
