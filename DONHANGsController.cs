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
        // Khởi tạo kết nối CSDL
        private QLBHTTCCHGTSVEntities1 db = new QLBHTTCCHGTSVEntities1();

        // GET: DONHANGs
        // searchString: Từ khóa nhận từ ô input bên View
        // page: Số trang hiện tại
        public ActionResult Index(string searchString, int? page)
        {
            // 1. Lấy dữ liệu gốc (kèm thông tin Khách & Nhân viên)
            // AsQueryable giúp tối ưu câu lệnh SQL khi chạy
            var dONHANGs = db.DONHANGs.Include(d => d.KHACHHANG).Include(d => d.NHANVIEN).AsQueryable();

            // 2. XỬ LÝ TÌM KIẾM (SEARCH)
            if (!String.IsNullOrEmpty(searchString))
            {
                // Cắt bỏ khoảng trắng thừa ở đầu và cuối (VD: "  DH01 " -> "DH01")
                searchString = searchString.Trim();

                // Logic lọc dữ liệu:
                // Tìm theo Mã đơn HOẶC Tên khách hàng HOẶC Số điện thoại
                dONHANGs = dONHANGs.Where(d =>
                    d.maDH.Contains(searchString) ||
                    d.KHACHHANG.tenKH.Contains(searchString) ||
                    d.KHACHHANG.soDienThoai.Contains(searchString)
                );
            }

            // 3. Sắp xếp: Đơn mới nhất lên đầu (Giảm dần theo ngày tạo)
            dONHANGs = dONHANGs.OrderByDescending(d => d.ngayTao);

            // 4. Cấu hình phân trang
            int pageSize = 3;  // Số dòng mỗi trang
            int pageNumber = (page ?? 1); // Mặc định là trang 1 nếu không có page

            // 5. Trả về View dạng PagedList
            return View(dONHANGs.ToPagedList(pageNumber, pageSize));
        }

        // GET: Details
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

        // Hàm giải phóng bộ nhớ
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