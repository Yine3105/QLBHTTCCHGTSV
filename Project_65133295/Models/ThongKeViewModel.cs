using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Project_65133295.Models
{
    public class ThongKeViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string PaymentMethod { get; set; } // "Tiền mặt", "Chuyển khoản"
        public string OrderStatus { get; set; }   // From DONHANG.trangThaiDonHang

        // Result Data
        public int TotalTransactions { get; set; }
        public decimal TotalRevenue { get; set; }
        public Dictionary<string, int> CountByPaymentMethod { get; set; }
        public Dictionary<string, double> RevenueByPaymentMethod { get; set; }

        public List<HOADON> Transactions { get; set; }

        public ThongKeViewModel()
        {
            Transactions = new List<HOADON>();
            CountByPaymentMethod = new Dictionary<string, int>();
            RevenueByPaymentMethod = new Dictionary<string, double>();
        }
    }
}
