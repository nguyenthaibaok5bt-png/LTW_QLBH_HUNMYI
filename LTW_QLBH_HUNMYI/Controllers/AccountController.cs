using LTW_QLBH_HUNMYI.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace LTW_QLBH_HUNMYI.Controllers
{
    public class AccountController : Controller
    {
        QLBH_HUNMYI_LTW_FINALEntities1 db = new QLBH_HUNMYI_LTW_FINALEntities1();
        #region LOGIN
        // GET: Account/Login
        public ActionResult Login()
        {
            if (Session["UserID"] != null)
            {
                return RedirectToHome();
            }
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin!";
                return View();
            }

            string passwordHash = GetMD5Hash(password);

            var account = db.ACCOUNT.FirstOrDefault(a =>
                a.USERNAME == username &&
                a.PASSWORDHASH == passwordHash &&
                a.TRANGTHAI == "Hoạt động");

            if (account != null)
            {
                // Lưu thông tin vào session
                Session["UserID"] = account.USERID;
                Session["Username"] = account.USERNAME;
                Session["Role"] = account.VAITRO;
                Session["Email"] = account.EMAIL;

                if (account.VAITRO == "Khách")
                {
                    Session["CustomerID"] = account.MAKH;
                    Session["CustomerName"] = db.KHACHHANG.Find(account.MAKH)?.HOTENKH;
                }
                else
                {
                    Session["StaffID"] = account.MANV;
                    var nhanvien = db.NHANVIEN.Find(account.MANV);
                    Session["StaffName"] = nhanvien?.HOTENNV;
                    Session["Position"] = nhanvien?.CHUCVU;
                }

                return RedirectToHome();
            }
            else
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng!";
                return View();
            }
        }
        #endregion

        #region REGISTER
        // GET: Account/Register
        public ActionResult Register()
        {
            if (Session["UserID"] != null)
            {
                return RedirectToHome();
            }
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]

        public ActionResult Register(string username, string password, string confirmPassword,
                             string hoTen, string gioiTinh, string sdt, string email, string diaChi)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp!";
                return View();
            }

            // Trước khi gọi Any() hoặc truy vấn DB, kiểm tra DB có sẵn sàng không
            try
            {
                bool canConnect = db.Database.Exists(); // EF6: kiểm tra DB
                if (!canConnect)
                {
                    ViewBag.Error = "Không thể kết nối tới cơ sở dữ liệu. Vui lòng kiểm tra cấu hình kết nối hoặc dịch vụ SQL Server.";
                    return View();
                }
            }
            catch (SqlException sqlEx)
            {
                // Lỗi kết nối/SQL -> hiện thông báo thân thiện, log chi tiết nếu cần
                // (Không show chi tiết lỗi cho user, chỉ log nội bộ)
                ViewBag.Error = "Lỗi kết nối tới cơ sở dữ liệu. Vui lòng liên hệ quản trị hệ thống.";
                // TODO: log sqlEx.Message / sqlEx.Number vào file log
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Có lỗi xảy ra khi kiểm tra cơ sở dữ liệu.";
                // TODO: log ex
                return View();
            }

            // Kiểm tra username/email tồn tại (bây giờ DB đã sẵn sàng)
            if (db.ACCOUNT.Any(a => a.USERNAME == username))
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại!";
                return View();
            }
            if (db.ACCOUNT.Any(a => a.EMAIL == email))
            {
                ViewBag.Error = "Email đã được sử dụng!";
                return View();
            }

            try
            {
                // Tạo mã KH mới
                string newMaKH = GenerateNewCode("KH", db.KHACHHANG.Select(k => k.MAKH).ToList());

                // Tạo KH
                KHACHHANG khachHang = new KHACHHANG
                {
                    MAKH = newMaKH,
                    HOTENKH = hoTen,
                    GIOITINH = gioiTinh,
                    SDT = sdt,
                    EMAIL = email,
                    DIACHI = diaChi,
                    NGAYDANGKY = DateTime.Now
                };

                // Tạo Account (bắt buộc role = Khách)
                string newAccountID = GenerateNewCode("ACC", db.ACCOUNT.Select(a => a.USERID).ToList());
                ACCOUNT account = new ACCOUNT
                {
                    USERID = newAccountID,
                    USERNAME = username,
                    PASSWORDHASH = GetMD5Hash(password), // NÊN thay MD5 bằng PBKDF2/bcrypt
                    VAITRO = "Khách",
                    MAKH = newMaKH,
                    MANV = null,
                    EMAIL = email,
                    SDT = sdt,
                    TRANGTHAI = "Hoạt động"
                };

                // Tạo giỏ hàng
                string newMaGH = GenerateNewCode("GH", db.GIOHANG.Select(g => g.MAGH).ToList());
                GIOHANG gioHang = new GIOHANG
                {
                    MAGH = newMaGH,
                    MAKH = newMaKH,
                    NGAYTAO = DateTime.Now
                };

                // EF6 tự động wrap SaveChanges() trong transaction, không cần BeginTransaction
                db.KHACHHANG.Add(khachHang);
                db.ACCOUNT.Add(account);
                db.GIOHANG.Add(gioHang);

                db.SaveChanges();

                TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (DbEntityValidationException dbEx)
            {
                // Lỗi validation - hiển thị chi tiết
                StringBuilder errorMessages = new StringBuilder();
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errorMessages.AppendLine($"- {validationError.PropertyName}: {validationError.ErrorMessage}");
                    }
                }
                ViewBag.Error = "Lỗi validation dữ liệu:\n" + errorMessages.ToString();
                // TODO: log dbEx
                return View();
            }
            catch (SqlException sqlEx)
            {
                // Lỗi SQL (ví dụ: unique constraint violation, network, timeout)
                ViewBag.Error = "Lỗi cơ sở dữ liệu khi đăng ký. Vui lòng liên hệ quản trị.";
                // TODO: log sqlEx số lỗi để debug (sqlEx.Number)
                return View();
            }
            catch (Exception ex)
            {
                // Hiển thị cả inner exception để debug
                string errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += "\nChi tiết: " + ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                    {
                        errorMessage += "\n" + ex.InnerException.InnerException.Message;
                    }
                }
                ViewBag.Error = "Có lỗi xảy ra: " + errorMessage;
                // TODO: log ex
                return View();
            }
        }


        // Đăng xuất
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }

        // Trang Access Denied
        public ActionResult AccessDenied()
        {
            return View();
        }

        // Helper methods
        private ActionResult RedirectToHome()
        {
            string role = Session["Role"]?.ToString();
            switch (role)
            {
                case "Chủ shop":
                    return RedirectToAction("Index", "Owner");
                case "Nhân viên":
                    return RedirectToAction("Index", "Staff");
                case "Khách":
                    return RedirectToAction("Index", "Customer");
                default:
                    return RedirectToAction("Login");
            }
        }
        #endregion

        #region HELPER METHODS
        private string GetMD5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

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
        #endregion

        #region DISPOSE
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}