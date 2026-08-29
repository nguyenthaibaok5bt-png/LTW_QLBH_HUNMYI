using LTW_QLBH_HUNMYI.Filters;
using LTW_QLBH_HUNMYI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Web;
using System.Web.Mvc;

namespace LTW_QLBH_HUNMYI.Controllers
{
    [CustomAuthorize(AllowedRoles = new[] { "Nhân viên" })]
    public class StaffController : Controller
    {
        private QLBH_HUNMYI_LTW_FINALEntities1 db = new QLBH_HUNMYI_LTW_FINALEntities1();

        #region TRANG CHỦ *****ĐÃ XONG*****
        public ActionResult Index()
        {
            ViewBag.Title = "Dashboard";

            var pendingOrders = db.HOADON_BAN.Where(o => o.TRANGTHAI == "Chờ xử lý").ToList();
            var todayWork = db.HOADON_BAN
                .Where(o => o.NGAYLAP.Value.Day == DateTime.Now.Day)
                .Count();

            ViewBag.TodayWork = todayWork;

            return View(pendingOrders);
        }
        #endregion

        #region SẢN PHẨM *****ĐÃ XONG*****
        public ActionResult Products(string madm)
        {
            ViewBag.Title = "Quản lý sản phẩm";

            // Lấy danh mục cho dropdown
            ViewBag.DanhMucList = new SelectList(db.DANHMUC.ToList(), "MADM", "TENDM");

            // Query sản phẩm kèm navigation property
            var products = db.SANPHAM.Include("DANHMUC");

            // Lọc nếu có chọn danh mục
            if (!string.IsNullOrEmpty(madm))
                products = (System.Data.Entity.Infrastructure.DbQuery<SANPHAM>)products.Where(p => p.MADM == madm);

            return View(products.ToList());
        }
        #endregion

        #region ĐƠN HÀNG *****ĐÃ XONG*****
        public ActionResult Orders(string status = null, string maHD = null)
        {
            ViewBag.Title = "Quản lý đơn hàng";

            var orders = db.HOADON_BAN.Include("KHACHHANG").Include("NHANVIEN").AsQueryable();

            //Tìm theo mã
            if (!string.IsNullOrEmpty(maHD))
            {
                orders = orders.Where(o => o.MAHD_BAN == maHD);
            }
            //Lọc theo trạng thái
            else if (!string.IsNullOrEmpty(status))
            {
                orders = orders.Where(o => o.TRANGTHAI == status);
            }


            var orderList = orders.OrderByDescending(o => o.NGAYLAP).ToList();
            return View(orderList);
        }

        public ActionResult OrderDetail(string id)
        {
            ViewBag.Title = "Chi tiết đơn hàng";

            var order = db.HOADON_BAN.Find(id);
            var details = db.CTHD_BAN.Where(ct => ct.MAHD_BAN == id).ToList();

            ViewBag.Order = order;
            return View(details);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateOrderStatus(string id, string status)
        {
            var order = db.HOADON_BAN.Find(id);
            if (order == null)
                return HttpNotFound();

            string currentStatus = order.TRANGTHAI;

            // ❌ Không cho sửa nếu đơn đã kết thúc
            if (currentStatus == "Hoàn thành" || currentStatus == "Đã hủy")
            {
                TempData["Error"] = "Đơn hàng đã kết thúc, không thể thay đổi trạng thái!";
                return RedirectToAction("OrderDetail", new { id });
            }

            // ❌ Không cho quay lui trạng thái
            if (!IsValidNextStatus(currentStatus, status))
            {
                TempData["Error"] = "Chuyển trạng thái không hợp lệ!";
                return RedirectToAction("OrderDetail", new { id });
            }

            // ✅ Cập nhật hợp lệ
            order.TRANGTHAI = status;
            db.SaveChanges();

            TempData["Success"] = "Cập nhật trạng thái thành công!";
            return RedirectToAction("OrderDetail", new { id });
        }
        #endregion

        // ======= CUSTOMERS ========
        public ActionResult Customers(string khach = null)
        {
            ViewBag.Title = "Quản lý khách hàng";
            var customers = db.KHACHHANG.AsQueryable();

             // 🟢 Tìm theo khách hàng (tên hoặc mã)
            if (!string.IsNullOrEmpty(khach))
            {
                customers = customers.Where(c =>
                    c.HOTENKH.Contains(khach) ||
                    c.MAKH.Contains(khach));
            }
          
            return View(customers.ToList());
        }

        public ActionResult CustomerDetail(string id)
        {
            var kh = db.KHACHHANG.Find(id);
            return View(kh);
        }

        #region TÀI KHOẢN

        // GET: Customer/Profile - Thông tin tài khoản
        public ActionResult Profile()
        {
            ViewBag.Title = "Thông tin tài khoản";

            try
            {
                string staffId = Session["StaffID"]?.ToString();
                var staff = db.NHANVIEN.Find(staffId);

                if (staff == null)
                {
                    return HttpNotFound();
                }

                //// Thống kê đơn hàng
                //ViewBag.TotalOrders = db.HOADON_BAN.Count(h => h.MAKH == customerId);
                //ViewBag.TotalSpent = db.HOADON_BAN
                //    .Where(h => h.MAKH == customerId && h.TRANGTHAI == "Hoàn thành")
                //    .Sum(h => (decimal?)h.TONGTIEN) ?? 0;

                return View(staff);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Có lỗi xảy ra: " + ex.Message;
                return View();
            }
        }

        // POST: Customer/UpdateProfile - Cập nhật thông tin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(NHANVIEN model)
        {
            try
            {
                string staffId = Session["StaffID"]?.ToString();
                var staff = db.NHANVIEN.Find(staffId);

                if (staff != null)
                {
                    staff.HOTENNV = model.HOTENNV;
                    staff.GIOITINH = model.GIOITINH;
                    staff.NGAYSINH = model.NGAYSINH;
                    staff.NGAYVAOLAM = model.NGAYVAOLAM;
                    staff.SDT = model.SDT;
                    staff.EMAIL = model.EMAIL;
                    staff.DIACHI = model.DIACHI;

                    db.SaveChanges();

                    // Cập nhật session
                    Session["StaffName"] = staff.HOTENNV;

                    TempData["Success"] = "Cập nhật thông tin thành công!";
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy thông tin khách hàng!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("Profile");
        }

        // GET: Customer/ChangePassword - Đổi mật khẩu
        public ActionResult ChangePassword()
        {
            ViewBag.Title = "Đổi mật khẩu";
            return View();
        }

        // POST: Customer/ChangePassword - Đổi mật khẩu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            try
            {
                if (newPassword != confirmPassword)
                {
                    TempData["Error"] = "Mật khẩu mới không khớp!";
                    return View();
                }

                string userId = Session["UserID"]?.ToString();
                var account = db.ACCOUNT.Find(userId);

                if (account != null)
                {
                    string oldPasswordHash = GetMD5Hash(oldPassword);

                    if (account.PASSWORDHASH != oldPasswordHash)
                    {
                        TempData["Error"] = "Mật khẩu cũ không đúng!";
                        return View();
                    }

                    account.PASSWORDHASH = GetMD5Hash(newPassword);
                    db.SaveChanges();

                    TempData["Success"] = "Đổi mật khẩu thành công!";
                    return RedirectToAction("Profile");
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy tài khoản!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return View();
        }

        #endregion

        #region HELPER METHODS

        // Tạo mã tự động
        private string GenerateNewCode(string prefix, System.Collections.Generic.List<string> existingCodes)
        {
            int maxNumber = 0;
            foreach (var code in existingCodes)
            {
                if (code.StartsWith(prefix))
                {
                    string numberPart = code.Substring(prefix.Length);
                    if (int.TryParse(numberPart, out int number))
                    {
                        if (number > maxNumber)
                            maxNumber = number;
                    }
                }
            }
            return prefix + (maxNumber + 1).ToString("D3");
        }

        // Mã hóa MD5
        private string GetMD5Hash(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        // Kiếm tra luồng tiến độ xử lý đơn hàng
        private bool IsValidNextStatus(string current, string next)
        {
            var flow = new Dictionary<string, List<string>>
    {
        { "Chờ xử lý", new List<string> { "Đang xử lý", "Đã hủy" } },
        { "Đang xử lý", new List<string> { "Hoàn thành", "Đã hủy" } }
    };

            return flow.ContainsKey(current) && flow[current].Contains(next);
        }

        #endregion
    }
}