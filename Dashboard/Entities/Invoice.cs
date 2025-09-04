namespace Dashboard.Entities
{
    public class InvoiceData
    {
        public string? SupplierName { get; set; }
        public string? CustomerName { get; set; }
        public string? SupplierId { get; set; }
        public string? CustomerId { get; set; }
        public List<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
        public decimal? TotalBeforeVat { get; set; }
        public decimal? TotalWithVat { get; set; }
        public string? InvoiceDate { get; set; }

        public string? RawText { get; set; }
    }

    public class InvoiceItem
    {
        public string? Description { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice { get; set; }
    }
}