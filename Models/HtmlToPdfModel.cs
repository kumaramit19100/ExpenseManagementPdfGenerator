namespace ExpenseManagementPdfGenerator.Models
{
    public class HtmlToPdfModel
    {
        public List<string> HtmlData { get; set; } = new List<string>();
        public string? FileName { get; set; }
        public string? Format { get; set; }
        public string? Css { get; set; }
    }
}
