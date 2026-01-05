using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization; // Thư viện xử lý JSON

namespace cua_hang_sinh_vien_guitar.Controllers
{
    public class TrangChuController : Controller
    {
        // CHUỖI KẾT NỐI DÙNG CHUNG (Đã sửa tên Server đúng theo máy bạn)
        string connStr = @"Data Source=LAPTOP-5TR6S8MK\TUANNHA;Initial Catalog=QLBHTTCCHGTSV;Integrated Security=True";

        // 1. Chuyển hướng trang chủ vào trang đặt hàng
        public ActionResult Index()
        {
            return RedirectToAction("Create");
        }

        // 2. GET: Hiển thị giao diện
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        // 3. POST: Xử lý nút "Áp dụng" mã khuyến mãi (AJAX gọi vào đây)
        [HttpPost]
        public JsonResult KiemTraKhuyenMai(string maKM)
        {
            decimal tongTienHienTai = 51000000; // Số tiền cứng (theo ảnh của bạn)
            string message = "";
            decimal soTienGiam = 0;
            bool success = false;

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = "SELECT * FROM KHUYENMAI WHERE maKM = @maKM";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@maKM", maKM ?? "");
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                decimal minBill = Convert.ToDecimal(reader["giaTriDonHangToiThieu"]);
                                DateTime hetHan = Convert.ToDateTime(reader["ngayHetHan"]);
                                decimal val = Convert.ToDecimal(reader["giaTriGiam"]);

                                // Kiểm tra hạn sử dụng và giá trị đơn
                                if (DateTime.Now > hetHan)
                                    message = "Mã đã hết hạn!";
                                else if (tongTienHienTai < minBill)
                                    message = "Đơn chưa đủ giá trị tối thiểu!";
                                else
                                {
                                    success = true;
                                    soTienGiam = val;
                                    message = "Áp dụng thành công!";
                                }
                            }
                            else
                            {
                                message = "Mã khuyến mãi không tồn tại!";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Nếu lỗi kết nối, trả về thông báo lỗi chi tiết để dễ sửa
                return Json(new { success = false, message = "Lỗi Server: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = success, message = message, giamGia = soTienGiam }, JsonRequestBehavior.AllowGet);
        }

        // 4. POST: Xử lý LƯU ĐƠN HÀNG (Submit Form)
        [HttpPost]
        public ActionResult Create(string TenKH, string SoDT, string Email, string DiaChi, string GhiChu, string CartJson, decimal TienGiamGia = 0)
        {
            if (string.IsNullOrEmpty(CartJson)) return Content("Lỗi: Giỏ hàng rỗng!");

            // Dịch ngược JSON giỏ hàng
            var serializer = new JavaScriptSerializer();
            List<CartItemJson> cartItems = serializer.Deserialize<List<CartItemJson>>(CartJson);

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();

                try
                {
                    // A. Tạo Khách Hàng
                    string maKH = "KH" + DateTime.Now.ToString("ddHHmmss");
                    string sqlKH = @"INSERT INTO KHACHHANG (maKH, tenKH, soDienThoai, email, diaChi, loaiKH, maTaiKhoan)
                                     VALUES (@maKH, @ten, @sdt, @email, @dc, N'Vãng lai', 'TK_KH03')";
                    SqlCommand cmdKH = new SqlCommand(sqlKH, conn, trans);
                    cmdKH.Parameters.AddWithValue("@maKH", maKH);
                    cmdKH.Parameters.AddWithValue("@ten", TenKH);
                    cmdKH.Parameters.AddWithValue("@sdt", SoDT);
                    cmdKH.Parameters.AddWithValue("@email", Email ?? "");
                    cmdKH.Parameters.AddWithValue("@dc", DiaChi);
                    cmdKH.ExecuteNonQuery();

                    // B. Tạo Đơn Hàng
                    string maDH = "DH" + DateTime.Now.ToString("ddHHmmss");
                    string sqlDH = @"INSERT INTO DONHANG (maDH, ngayTao, maNV, maKH, trangThaiDonHang, ghiChu)
                                     VALUES (@maDH, GETDATE(), 'NV02', @maKH, N'Chờ xử lý', @ghiChu)";
                    SqlCommand cmdDH = new SqlCommand(sqlDH, conn, trans);
                    cmdDH.Parameters.AddWithValue("@maDH", maDH);
                    cmdDH.Parameters.AddWithValue("@maKH", maKH);
                    cmdDH.Parameters.AddWithValue("@ghiChu", GhiChu ?? "");
                    cmdDH.ExecuteNonQuery();

                    // C. Lưu Chi Tiết & Tính Tổng
                    decimal tongTienHang = 0;
                    foreach (var item in cartItems)
                    {
                        string sqlCT = @"INSERT INTO CHITIETDONHANG (maDH, maSP, soLuong, donGiaLuuTru) 
                                         VALUES (@maDH, @maSP, @sl, @gia)";
                        SqlCommand cmdCT = new SqlCommand(sqlCT, conn, trans);
                        cmdCT.Parameters.AddWithValue("@maDH", maDH);
                        cmdCT.Parameters.AddWithValue("@maSP", item.id); // Lưu ý: View phải gửi mã SP001, SP002...
                        cmdCT.Parameters.AddWithValue("@sl", item.qty);
                        cmdCT.Parameters.AddWithValue("@gia", item.price);
                        cmdCT.ExecuteNonQuery();

                        tongTienHang += (item.price * item.qty);
                    }

                    // D. Tạo Hóa Đơn
                    string maHD = "HD" + DateTime.Now.ToString("ddHHmmss");
                    decimal tongPhaiTra = tongTienHang - TienGiamGia;
                    if (tongPhaiTra < 0) tongPhaiTra = 0;

                    string sqlHD = @"INSERT INTO HOADON (maHD, maDH, ngayThanhToan, maNV, tongTienTamTinh, soTienGiamKM, tongTienPhaiTra, phuongThucThanhToan)
                                     VALUES (@maHD, @maDH, GETDATE(), 'NV02', @tamTinh, @giamGia, @phaiTra, N'Tiền mặt')";
                    SqlCommand cmdHD = new SqlCommand(sqlHD, conn, trans);
                    cmdHD.Parameters.AddWithValue("@maHD", maHD);
                    cmdHD.Parameters.AddWithValue("@maDH", maDH);
                    cmdHD.Parameters.AddWithValue("@tamTinh", tongTienHang);
                    cmdHD.Parameters.AddWithValue("@giamGia", TienGiamGia);
                    cmdHD.Parameters.AddWithValue("@phaiTra", tongPhaiTra);
                    cmdHD.ExecuteNonQuery();

                    trans.Commit();
                    return Content("<script>alert('🎉 Đặt hàng thành công! Mã đơn: " + maDH + "'); window.location.href='/TrangChu/Create';</script>");
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    return Content("Lỗi lưu đơn hàng: " + ex.Message);
                }
            }
        }
    }

    // Class hỗ trợ đọc JSON giỏ hàng
    public class CartItemJson
    {
        public string id { get; set; }
        public string name { get; set; }
        public decimal price { get; set; }
        public int qty { get; set; }
    }
}