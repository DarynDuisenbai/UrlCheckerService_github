using Newtonsoft.Json.Linq;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using System.Net;
using System.Net.Mail;
using System.Text;
using Org.BouncyCastle.Crypto.Macs;



namespace UrlCheckerService
{
    public class LinkChecker
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _phoneNumber;
        private readonly string _twilioAccountSid;
        private readonly string _twilioAuthToken;
        private readonly string _twilioFromPhoneNumber;


        public LinkChecker(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _configuration = configuration;
            _phoneNumber = configuration.GetValue<string>("SMSSettings:PhoneNumber");
            _twilioAccountSid = configuration.GetValue<string>("TwilioSettings:AccountSid");
            _twilioAuthToken = configuration.GetValue<string>("TwilioSettings:AuthToken");
            _twilioFromPhoneNumber = configuration.GetValue<string>("TwilioSettings:FromPhoneNumber");

        }

        public async Task CheckLinks()
        {
            var links = _configuration.GetSection("Links").Get<List<string>>();
            var telegramSettings = _configuration.GetSection("TelegramSettings");
            var botToken = telegramSettings["BotToken"];
            var channelName = telegramSettings["ChannelName"];

            foreach (var link in links)
            {
                try
                {
                    var response = await _httpClient.GetAsync(link);
                    var status = response.StatusCode;

                    var fileName = $"{DateTime.Now:yyyyMMdd}_{link.GetHashCode()}.txt";
                    var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "Statuses");
                    Directory.CreateDirectory(directoryPath);
                    var filePath = Path.Combine(directoryPath, fileName);
                    await File.WriteAllTextAsync(filePath, status.ToString());

                    if (!response.IsSuccessStatusCode)
                    {
                        await SendMessageToTelegramChannel($"Ошибка при запросе {link}. Статус: {status}", botToken, channelName);
                        await SendEmailNotification("Ошибка при запросе", $"Ошибка при запросе {link}. Статус: {status}", "duisenbaidaryn2205@gmail.com");
                        await SendSMSNotification($"Ошибка при запросе {link}. Статус: {status}", "+77714026785");
                    }
                }
                catch (Exception ex)
                {
                    await SendMessageToTelegramChannel($"Ошибка при запросе {link}: {ex.Message}", botToken, channelName);
                    await SendEmailNotification("Ошибка при запросе", $"Ошибка при запросе {link}: {ex.Message}", "duisenbaidaryn2205@gmail.com");
                    await SendSMSNotification($"Ошибка при запросе {link}: {ex.Message}", "+77714026785");

                }
            }
        }

        private async Task SendMessageToTelegramChannel(string message, string botToken, string channelName)
        {
            try
            {
                var apiUrl = $"https://api.telegram.org/bot{botToken}/sendMessage";

                var parameters = new Dictionary<string, string>
                {
                    { "chat_id", channelName },
                    { "text", message }
                };

                var encodedContent = new FormUrlEncodedContent(parameters);

                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.PostAsync(apiUrl, encodedContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Ошибка при отправке сообщения в канал Telegram. Код ошибки: {response.StatusCode}");
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Ответ сервера Telegram: " + responseContent);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при отправке сообщения в канал Telegram: {ex.Message}");
            }
        }



        public async Task SendEmailNotification(string subject, string message, string email)
        {
            // Настройки SMTP сервера для Mailtrap
            string smtpServer = "smtp.mailtrap.io";
            int port = 2525; 
            string username = "Soulking"; 
            string password = "qwerty123";

            using (SmtpClient client = new SmtpClient(smtpServer, port))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(username, password);

                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress("duisenbaidaryn2205@gmail.com"); 
                mailMessage.To.Add(email);
                mailMessage.Subject = subject;
                mailMessage.Body = message;

                try
                {
                    await client.SendMailAsync(mailMessage);
                    Console.WriteLine("Письмо отправлено успешно.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка при отправке письма: " + ex.Message);
                }
            }
        }

        private async Task SendSMSNotification(string message, string phoneNumber)
        {
            TwilioClient.Init(_twilioAccountSid, _twilioAuthToken);

            try
            {
                var messageOptions = new CreateMessageOptions(
                    new PhoneNumber(phoneNumber));
                messageOptions.From = new PhoneNumber(_twilioFromPhoneNumber);
                messageOptions.Body = message;

                var messageResult = await MessageResource.CreateAsync(messageOptions);
                Console.WriteLine($"SMS отправлено с SID: {messageResult.Sid}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке SMS: {ex.Message}");
            }
        }
    }
}
