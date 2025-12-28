using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using cửa_hàng_sinh_viên_guitar.Models;
using PagedList;

namespace cửa_hàng_sinh_viên_guitar.Controllers
{
    public class DONHANGsController : Controller
    {
        private QLBHTTCCHGTSVEntities1 db = new QLBHTTCCHGTSVEntities1();

        // GET: DONHANGs
        public ActionResult Index(string searchString, int? page)
        {
            // 1. Lấy dữ liệu ban đầu (kèm thông tin Khách và Nhân viên)
            var dONHANGs = db.DONHANGs.Include(d => d.KHACHHANG).Include(d => d.NHANVIEN).AsQueryable();

            // 2. Xử lý tìm kiếm (Nếu có từ khóa nhập vào)
            if (!String.IsNullOrEmpty(searchString))
            {
                // Tìm theo: Mã đơn OR Tên khách hàng OR Số điện thoại (nếu có trong bảng Khách hàng)
                dONHANGs = dONHANGs.Where(d =>
                    d.maDH.Contains(searchString) ||
                    d.KHACHHANG.tenKH.Contains(searchString) ||
                    d.KHACHHANG.soDienThoai.Contains(searchString)
                );
            }

            // Sắp xếp: Đơn mới nhất nằm trên cùng (giảm dần theo ngày tạo)
            dONHANGs = dONHANGs.OrderByDescending(d => d.ngayTao);

            int pageSize = 3;  // Số lượng đơn mỗi trang (Theo yêu cầu của bạn)
            int pageNumber = (page ?? 1); // Nếu page null thì mặc định là trang 1

            //Trả về View dạng PagedList
            return View(dONHANGs.ToPagedList(pageNumber, pageSize));
            
        }

        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            DONHANG dONHANG = db.DONHANGs.Find(id);

            if (dONHANG == null)
            {
                return HttpNotFound();
            }

            return View(dONHANG);
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
