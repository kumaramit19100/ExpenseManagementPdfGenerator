namespace ExpenseManagementPdfGenerator.Models
{
    public class PdfResponse
    {
        public int StatusCode { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        public bool Success => StatusCode >= 200 && StatusCode < 300;
    }
}
