using Azure;
using Azure.Data.Tables;

namespace ABCBabyShop_2.Models
{
    public class Contract
    {
        public string? FileName { get; set; }
        public string? FileUrl { get; set; } //For downloading the file
    }
}
