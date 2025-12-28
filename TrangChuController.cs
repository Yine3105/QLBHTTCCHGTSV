using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace cửa_hàng_sinh_viên_guitar.Controllers
{
    public class TrangChuController : Controller
    {
        // GET: TrangChu
        public ActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        // POST: TrangChu/Create
        [HttpPost]
        public ActionResult Create(string TenKH, string SoDT, string Email, string DiaChi, string GhiChu, string CartJson)
        {
            // Test nhanh
            return Content("Đặt hàng thành công: " + TenKH);
        }

    }
}