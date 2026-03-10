using System.Net;
using ExpenseManagementPdfGenerator.Models;
using ExpenseManagementPdfGenerator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Playwright;
using System.Text;

namespace ExpenseManagementPdfGenerator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PdfController : ControllerBase
    {
        private const string ApiKeyHeaderName = "X-Pdf-Api-Key";
        private readonly IConfiguration _configuration;

        public PdfController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("Generate")]
        public async Task<IActionResult> Generate([FromBody] HtmlToPdfModel? model)
        {
            var apiKeyValidation = ValidateApiKey();
            if (apiKeyValidation != null)
            {
                return apiKeyValidation;
            }

            if (model == null)
            {
                return BadRequest(new PdfResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "Request body is required."
                });
            }

            if (model.HtmlData == null || model.HtmlData.Count == 0)
            {
                return BadRequest(new PdfResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "HtmlData is required to generate PDF."
                });
            }

            try
            {
                var pdfBytes = await GeneratePdfAsync(model);
                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, new PdfResponse
                    {
                        StatusCode = (int)HttpStatusCode.InternalServerError,
                        Message = "Generated PDF is empty."
                    });
                }

                return File(pdfBytes, "application/pdf", string.IsNullOrWhiteSpace(model.FileName) ? "document.pdf" : model.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new PdfResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Message = "Failed to generate PDF using Playwright: " + ex.Message
                });
            }
        }

        private IActionResult? ValidateApiKey()
        {
            var configuredKey = _configuration["PdfGenerator:ApiKey"];
            if (string.IsNullOrWhiteSpace(configuredKey))
            {
                return Unauthorized(new PdfResponse
                {
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                    Message = "PDF API key is not configured. Set PdfGenerator:ApiKey."
                });
            }

            var providedKey = Request.Headers[ApiKeyHeaderName].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(providedKey) || !string.Equals(configuredKey, providedKey, StringComparison.Ordinal))
            {
                return Unauthorized(new PdfResponse
                {
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                    Message = "Invalid or missing API key. Provide valid key in " + ApiKeyHeaderName + " header."
                });
            }

            return null;
        }

        private static async Task<byte[]> GeneratePdfAsync(HtmlToPdfModel model)
        {
            var html = BuildHtmlDocument(model);
            var browser = await SharedPlaywrightBrowser.GetBrowserAsync();

            await using var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            await page.SetContentAsync(html, new PageSetContentOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(10000);

            //var pdfFormat = string.IsNullOrWhiteSpace(model.Format) ? "A4" : model.Format;
            var pdfFormat = "A4";
            var pdfBytes = await page.PdfAsync(new PagePdfOptions
            {
                Format = pdfFormat,
                PrintBackground = true
            });

            return pdfBytes;
        }

        private static string BuildHtmlDocument(HtmlToPdfModel model)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!doctype html><html><head><meta charset='utf-8'/>");

            if (!string.IsNullOrWhiteSpace(model.Css))
            {
                sb.AppendLine("<style>");
                sb.AppendLine(model.Css);
                sb.AppendLine("</style>");
            }

            sb.AppendLine("</head><body>");

            for (int i = 0; i < model.HtmlData.Count; i++)
            {
                var fragment = Base64Decode(model.HtmlData[i]);
                sb.AppendLine(fragment);

                if (i < model.HtmlData.Count - 1)
                {
                    sb.AppendLine("<div style='page-break-after:always;'>&nbsp;</div>");
                }
            }

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
