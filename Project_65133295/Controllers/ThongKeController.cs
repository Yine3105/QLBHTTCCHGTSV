using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133295.Models;

namespace Project_65133295.Controllers
{
    public class ThongKeController : Controller
    {
        private QLBHTTCCHGTSVEntities db = new QLBHTTCCHGTSVEntities();

        // GET: ThongKe/ThongKe
        public ActionResult ThongKe(string fromDate, string toDate, string paymentMethod, string status)
        {
            var model = new ThongKeViewModel();

            // 1. Setup Default Filters if null
            // Default to current month if no date provided
            if (string.IsNullOrEmpty(fromDate))
            {
                var now = DateTime.Now;
                model.FromDate = new DateTime(now.Year, now.Month, 1);
            }
            else
            {
                DateTime dt;
                if (DateTime.TryParse(fromDate, out dt))
                     model.FromDate = dt;
            }

            if (string.IsNullOrEmpty(toDate))
            {
                model.ToDate = DateTime.Now;
            }
            else
            {
                 DateTime dt;
                 if (DateTime.TryParse(toDate, out dt))
                     model.ToDate = dt;
            }

            model.PaymentMethod = paymentMethod;
            model.OrderStatus = status;

            // 2. Base Query
            var query = db.HOADONs.Include(h => h.DONHANG).Include(h => h.DONHANG.KHACHHANG).AsQueryable();

            // 3. Apply Filters
            if (model.FromDate.HasValue)
            {
                var fDate = model.FromDate.Value.Date;
                query = query.Where(h => h.ngayThanhToan >= fDate);
            }

            if (model.ToDate.HasValue)
            {
                // Include the whole end day (23:59:59)
                var tDate = model.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(h => h.ngayThanhToan <= tDate);
            }

            if (!string.IsNullOrEmpty(model.PaymentMethod))
            {
                query = query.Where(h => h.phuongThucThanhToan == model.PaymentMethod);
            }

            if (!string.IsNullOrEmpty(model.OrderStatus))
            {
                query = query.Where(h => h.DONHANG.trangThaiDonHang == model.OrderStatus);
            }

            // 4. Execute Query & Calculate Stats
            var transactions = query.OrderByDescending(h => h.ngayThanhToan).ToList();

            model.Transactions = transactions;
            model.TotalTransactions = transactions.Count;
            // Handle null sums if empty
            model.TotalRevenue = transactions.Any() ? transactions.Sum(h => h.tongTienPhaiTra) : 0;

            // Grouping for charts/breakdown
            model.CountByPaymentMethod = transactions
                .GroupBy(h => h.phuongThucThanhToan)
                .ToDictionary(g => g.Key ?? "Khác", g => g.Count());
            
            // ViewBags for Dropdowns
            var methods = db.HOADONs.Select(h => h.phuongThucThanhToan).Distinct().Where(x => x != null).ToList();
            ViewBag.PaymentMethods = new SelectList(methods);

            var statuses = db.DONHANGs.Select(d => d.trangThaiDonHang).Distinct().Where(x => x != null).ToList();
            ViewBag.OrderStatuses = new SelectList(statuses);

            if (!transactions.Any())
            {
                ViewBag.Message = "Không có giao dịch phù hợp với tiêu chí đã chọn";
            }
            else
            {
                 if (model.FromDate > model.ToDate)
                 {
                     ModelState.AddModelError("", "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");
                     ViewBag.Error = "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.";
                 }
            }

            return View(model);
        }

        // GET: ThongKe/GetOrderDetails/HD001
        public ActionResult GetOrderDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return HttpNotFound();
            }

            // Eager load related data: Details, Product, Customer, Staff
            var order = db.HOADONs
                .Include(h => h.CHITIETHOADONs)
                .Include(h => h.CHITIETHOADONs.Select(ct => ct.SANPHAM))
                .Include(h => h.DONHANG)
                .Include(h => h.DONHANG.KHACHHANG)
                .Include(h => h.NHANVIEN)
                .FirstOrDefault(h => h.maHD == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            // Render partial view to string with proper encoding
            var html = RenderPartialToString("_OrderDetailPartial", order);
            return Content(html, "text/html; charset=utf-8");
        }

        // Helper method to render partial view to string
        private string RenderPartialToString(string viewName, object model)
        {
            ViewData.Model = model;
            using (var sw = new System.IO.StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}