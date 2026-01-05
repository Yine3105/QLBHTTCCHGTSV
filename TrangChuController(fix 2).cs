using System;
using System.Collections.Generic;
using System.Linq; // Quan trọng để dùng LINQ
using System.Web.Mvc;
using System.Web.Script.Serialization;
using cửa_hàng_sinh_viên_guitar.Models;

namespace cua_hang_sinh_vien_guitar.Controllers
{
    public class TrangChuController : Controller
    {
        // Khởi tạo Entity Framework
        private QLBHTTCCHGTSVEntities1 db = new QLBHTTCCHGTSVEntities1();

        public ActionResult Index()
        {
            return RedirectToAction("Create");
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        // 1. HÀM KIỂM TRA MÃ KHUYẾN MÃI (Viết lại bằng EF)
        [HttpPost]
        public JsonResult KiemTraKhuyenMai(string maKM)
        {
            decimal tongTienMacDinh = 51000000;

            // Tìm mã KM trong database bằng LINQ
            var km = db.KHUYENMAIs.FirstOrDefault(x => x.maKM == maKM);

            if (km == null)
            {
                return Json(new { success = false, message = "Mã khuyến mãi không tồn tại!" });
            }

            if (DateTime.Now > km.ngayHetHan)
            {
                return Json(new { success = false, message = "Mã đã hết hạn!" });
            }

            if (tongTienMacDinh < km.giaTriDonHangToiThieu)
            {
                return Json(new { success = false, message = "Đơn hàng chưa đủ điều kiện áp dụng!" });
            }

            return Json(new { success = true, message = "Áp dụng thành công!", giamGia = km.giaTriGiam });
        }

        // 2. HÀM LƯU ĐƠN HÀNG (Viết lại bằng EF)
        [HttpPost]
        public ActionResult Create(string TenKH, string SoDT, string Email, string DiaChi, string GhiChu, string CartJson, decimal TienGiamGia = 0)
        {
            if (string.IsNullOrEmpty(CartJson)) return Content("Giỏ hàng rỗng!");

            var serializer = new JavaScriptSerializer();
            List<CartItemJson> cartItems = serializer.Deserialize<List<CartItemJson>>(CartJson);

            // Sử dụng Transaction của Entity Framework để đảm bảo an toàn dữ liệu
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // A. Lưu Khách Hàng
                    string maKH = "KH" + DateTime.Now.ToString("ddHHmmss");
                    KHACHHANG kh = new KHACHHANG();
                    kh.maKH = maKH;
                    kh.tenKH = TenKH;
                    kh.soDienThoai = SoDT;
                    kh.email = Email;
                    kh.diaChi = DiaChi;
                    kh.loaiKH = "Vãng lai";
                    kh.maTaiKhoan = "TK_KH03"; // Tài khoản mặc định

                    db.KHACHHANGs.Add(kh);
                    db.SaveChanges(); // Lưu tạm để có maKH dùng bên dưới

                    // B. Lưu Đơn Hàng
                    string maDH = "DH" + DateTime.Now.ToString("ddHHmmss");
                    DONHANG dh = new DONHANG();
                    dh.maDH = maDH;
                    dh.ngayTao = DateTime.Now;
                    dh.maNV = "NV02";
                    dh.maKH = maKH;
                    dh.trangThaiDonHang = "Chờ xử lý";
                    dh.ghiChu = GhiChu;

                    db.DONHANGs.Add(dh);
                    db.SaveChanges();

                    // C. Lưu Chi Tiết
                    decimal tongTienHang = 0;
                    foreach (var item in cartItems)
                    {
                        CHITIETDONHANG ct = new CHITIETDONHANG();
                        ct.maDH = maDH;
                        ct.maSP = item.id;
                        ct.soLuong = item.qty;
                        ct.donGiaLuuTru = item.price; // Lưu ý kiểu dữ liệu trong Model phải khớp (decimal)

                        db.CHITIETDONHANGs.Add(ct);
                        tongTienHang += (item.price * item.qty);
                    }
                    db.SaveChanges();

                    // D. Lưu Hóa Đơn
                    string maHD = "HD" + DateTime.Now.ToString("ddHHmmss");
                    decimal tongPhaiTra = tongTienHang - TienGiamGia;
                    if (tongPhaiTra < 0) tongPhaiTra = 0;

                    HOADON hd = new HOADON();
                    hd.maHD = maHD;
                    hd.maDH = maDH;
                    hd.ngayThanhToan = DateTime.Now;
                    hd.maNV = "NV02";
                    hd.tongTienTamTinh = tongTienHang;
                    hd.soTienGiamKM = TienGiamGia;
                    hd.tongTienPhaiTra = tongPhaiTra;
                    hd.phuongThucThanhToan = "Tiền mặt";

                    // Nếu có mã KM thì lưu, không thì thôi (xử lý logic lưu mã KM nếu cần)
                    // hd.maKM = ...; 

                    db.HOADONs.Add(hd);
                    db.SaveChanges();

                    transaction.Commit(); // Xác nhận tất cả thành công
                    return Content("<script>alert('🎉 Đặt hàng thành công!'); window.location.href='/TrangChu/Create';</script>");
                }
                catch (Exception ex)
                {
                    transaction.Rollback(); // Có lỗi thì hoàn tác
                    return Content("Lỗi: " + ex.Message + " - Inner: " + (ex.InnerException != null ? ex.InnerException.Message : ""));
                }
            }
        }
    }

    public class CartItemJson
    {
        public string id { get; set; }
        public string name { get; set; }
        public decimal price { get; set; }
        public int qty { get; set; }
    }
}