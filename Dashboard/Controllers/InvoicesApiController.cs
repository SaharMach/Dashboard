using Docnet.Core;
using Docnet.Core.Models;
using DotNetEnv;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tesseract;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;


namespace Dashboard.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesApiController : ControllerBase
    {
        private readonly HttpClient _httpClient;



        private readonly IConfiguration _config;

        public InvoicesApiController(IConfiguration config)
        {
            _httpClient = new HttpClient();
            _config = config; 
        }

        [HttpPost("upload")]
        public async Task<IActionResult> ParseInvoice([FromForm] IFormFile invoiceFile)
        {
            var apiKey = Env.GetString("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
                return BadRequest("OPENAI_API_KEY לא מוגדר.");

            if (invoiceFile == null || invoiceFile.Length == 0)
                return BadRequest("לא הועלה קובץ.");

            try
            {
                string rawText = await ExtractTextFromPdfOcr(invoiceFile);

                string cleanedText = CleanPdfText(rawText);

                var invoiceJson = await ParseWithChatGptHebrew(cleanedText, apiKey);

                return Ok(invoiceJson);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        [HttpPost("send-email")]

        public async Task<IActionResult> SendEmail([FromForm] IFormFile invoiceFile, [FromForm] string to, [FromForm] string body)
        {
            if (invoiceFile == null || invoiceFile.Length == 0)
                return BadRequest("לא נבחר קובץ לשליחה.");

            if (string.IsNullOrWhiteSpace(to))
                return BadRequest("לא צוינה כתובת מייל.");

            SmtpClient client = null;
            try
            {
                var smtpHost = _config["Smtp:Host"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["Smtp:Port"] ?? "587");
                var smtpUser = _config["Smtp:User"];
                var smtpPass = _config["Smtp:Password"];

                if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
                    return BadRequest("SMTP User או Password חסרים");

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Invoice System", smtpUser));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = $"חשבונית - {invoiceFile.FileName}";

                var builder = new BodyBuilder
                {
                    TextBody = body
                };

                using var ms = new MemoryStream();
                await invoiceFile.CopyToAsync(ms);
                builder.Attachments.Add(invoiceFile.FileName, ms.ToArray(), ContentType.Parse("application/pdf"));

                message.Body = builder.ToMessageBody();

                client = new SmtpClient();

                if (smtpPort == 465)
                {
                    await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.SslOnConnect);
                }
                else
                {
                    await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                }

                await client.AuthenticateAsync(smtpUser, smtpPass);

                await client.SendAsync(message);

                return Ok(new { message = "המייל נשלח בהצלחה!" });
            }
            catch (MailKit.Security.AuthenticationException authEx)
            {
                return StatusCode(500, new
                {
                    error = "Authentication failed",
                    details = authEx.Message,
                    suggestion = "בדוק שהשתמשת ב-App Password הנכון מ-Google"
                });
            }
            catch (MailKit.Net.Smtp.SmtpCommandException smtpEx)
            {
                return StatusCode(500, new
                {
                    error = "SMTP Command failed",
                    details = smtpEx.Message,
                    statusCode = smtpEx.StatusCode,
                    suggestion = GetMailKitErrorSuggestion(smtpEx.StatusCode)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = ex.Message,
                    type = ex.GetType().Name
                });
            }
            finally
            {
                if (client?.IsConnected == true)
                    await client.DisconnectAsync(true);
                client?.Dispose();
            }
        }

        private string GetMailKitErrorSuggestion(MailKit.Net.Smtp.SmtpStatusCode statusCode)
        {
            return statusCode switch
            {
                MailKit.Net.Smtp.SmtpStatusCode.AuthenticationRequired => "בדוק App Password",
                MailKit.Net.Smtp.SmtpStatusCode.MailboxBusy => "נסה שוב מאוחר יותר",
                MailKit.Net.Smtp.SmtpStatusCode.InsufficientStorage => "אין מקום בשרת",
                _ => "בדוק הגדרות SMTP"
            };
        }



        private async Task<string> ExtractTextFromPdfOcr(IFormFile file)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            byte[] pdfBytes = ms.ToArray();

            var ocrText = new StringBuilder();

            using var engine = new TesseractEngine(@"./tessdata", "heb+eng", EngineMode.Default);
            using var lib = DocLib.Instance;
            using var docReader = lib.GetDocReader(pdfBytes, new PageDimensions(1080, 1920));

            for (int i = 0; i < docReader.GetPageCount(); i++)
            {
                using var pageReader = docReader.GetPageReader(i);
                var rawBytes = pageReader.GetImage();

                using var img = Image.LoadPixelData<Bgra32>(
                    rawBytes,
                    pageReader.GetPageWidth(),
                    pageReader.GetPageHeight()
                );

                using var msImg = new MemoryStream();
                img.Save(msImg, new PngEncoder());
                msImg.Position = 0;

                using var pix = Pix.LoadFromMemory(msImg.ToArray());
                using var page = engine.Process(pix);
                ocrText.AppendLine(page.GetText());
            }

            return ocrText.ToString();
        }

        private string CleanPdfText(string rawText)
        {
            string cleaned = Regex.Replace(rawText, @"[^\w\s₪\.,:/-]", "");
            cleaned = Regex.Replace(cleaned, @"\s+", " ");
            return cleaned;
        }

        private async Task<string> ParseWithChatGptHebrew(string text, string apiKey)
        {
            var systemPrompt = @"
                אתה מנתח חשבוניות בעברית.
                המטרה שלך היא לחלץ את כל השדות החשבונית מהטקסט.

                חייב להחזיר **רק JSON תקין** עם השדות הבאים בעברית:
                'ספק', 'לקוח', 'ח.פ ספק', 'ח.פ לקוח', 'תאריך חשבונית',
                'פריטים' (מערך של פריטים עם 'תיאור', 'כמות', 'מחיר יחידה', 'סה״כ'),
                'סה״כ לפני מע״מ', 'סה״כ כולל מע״מ'.

                הנחיות חשובות:
                1. אם שדה לא קיים בטקסט, כתוב `null`.
                2. עבור פריטים:
                   - אם כמות או מחיר יחידה לא ידועים, כתוב `לא נמצא`.
                   - אם אין פריטים כלל, החזר מערך ריק `[]`.
                3. ודא שהפלט הוא JSON חוקי – בלי סימונים ```json, בלי הסברים, רק JSON.
                4. אל תשנה את שמות השדות.
                5. המודל צריך לזהות גם ערכים קרובים או דומים אם השם המדויק שונה מעט.
                6. כל הערכים המספריים חייבים להיות מספרים (לא טקסט), ואם לא ידוע – `null`.

                **דוגמת JSON למבנה הפלט (לשם מבנה בלבד, לא לערכים ספציפיים):**
                {
                  ""ספק"": null,
                  ""לקוח"": null,
                  ""ח.פ ספק"": null,
                  ""ח.פ לקוח"": null,
                  ""תאריך חשבונית"": null,
                  ""פריטים"": [
                    { ""תיאור"": null, ""כמות"": null, ""מחיר יחידה"": null, ""סהכ"": null }
                  ],
                  ""סה״כ לפני מע״מ"": null,
                  ""סה״כ כולל מע״מ"": null
                }
                ";


            var request = new
            {
                model = "gpt-4.1",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = text }
                }
            };

            var json = JsonSerializer.Serialize(request);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync();
            var root = JsonDocument.Parse(responseContent);
            var messageContent = root.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return messageContent; 
        }
    }
}

