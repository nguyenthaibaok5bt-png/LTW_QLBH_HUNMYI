using LTW_QLBH_HUNMYI.Filters;
using LTW_QLBH_HUNMYI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Web;
using System.Web.Mvc;

namespace LTW_QLBH_HUNMYI.Controllers
{
    [CustomAuthorize(AllowedRoles = new[] { "Chủ shop" })]
    public class OwnerController : Controller
    {
        private QLBH_HUNMYI_LTW_FINALEntities1 db = new QLBH_HUNMYI_LTW_FINALEntities1();

        // GET: Owner - Dashboard
        #region TRANG CHỦ *****ĐÃ XONG*****
        public ActionResult Index()
        {
            ViewBag.Title = "Dashboard";

            // Thống kê tổng quan
            ViewBag.TotalProducts = db.SANPHAM.Count(s => s.TRANGTHAI == "Đang bán");
            ViewBag.TotalOrders = db.HOADON_BAN.Count();
            ViewBag.TotalCustomers = db.KHACHHANG.Count();
            ViewBag.TotalRevenue = db.HOADON_BAN
                .Where(h => h.TRANGTHAI == "Hoàn thành")
                .Sum(h => (decimal?)h.TONGTIEN) ?? 0;

            // Đơn hàng chờ xử lý
            ViewBag.PendingOrders = db.HOADON_BAN.Count(h => h.TRANGTHAI == "Chờ xử lý");

            // Sản phẩm sắp hết hàng
            ViewBag.LowStockProducts = db.SANPHAM.Count(s => s.SOLUONGTON < 10 && s.TRANGTHAI == "Đang bán");

            // Đơn hàng gần đây
            var recentOrders = db.HOADON_BAN
                .OrderByDescending(h => h.NGAYLAP)
                .Take(5)
                .ToList();
            ViewBag.RecentOrders = recentOrders;

            return View();
        }
        #endregion

        #region SẢN PHẨM *****ĐÃ XONG*****
        // GET: Owner/Products - Quản lý sản phẩm
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

        // GET: Owner/CreateProduct
        public ActionResult CreateProduct()
        {
            ViewBag.Title = "Thêm sản phẩm mới";
            ViewBag.Categories = new SelectList(db.DANHMUC.Where(d => d.TRANGTHAI == "Hiển thị"), "MADM", "TENDM");
            return View();
        }

        // POST: Owner/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateProduct(SANPHAM product, HttpPostedFileBase imageFile)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Tạo mã sản phẩm mới
                    product.MASP = GenerateNewCode("SP", db.SANPHAM.Select(s => s.MASP).ToList());
                    product.NGAYTAO = DateTime.Now;
                    product.TRANGTHAI = "Đang bán";

                    // Xử lý upload hình ảnh (nếu có)
                    if (imageFile != null && imageFile.ContentLength > 0)
                    {
                        string fileName = System.IO.Path.GetFileName(imageFile.FileName);
                        string path = System.IO.Path.Combine(Server.MapPath("~/Content/Images/Products/"), fileName);
                        imageFile.SaveAs(path);
                        product.HINHANH = fileName;
                    }

                    db.SANPHAM.Add(product);
                    db.SaveChanges();

                    TempData["Success"] = "Thêm sản phẩm thành công!";
                    return RedirectToAction("Products");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            }

            ViewBag.Categories = new SelectList(db.DANHMUC.Where(d => d.TRANGTHAI == "Hiển thị"), "MADM", "TENDM");
            return View(product);
        }

        // GET: Owner/EditProduct/{id}
        public ActionResult EditProduct(string id)
        {
            ViewBag.Title = "Chỉnh sửa sản phẩm";
            var product = db.SANPHAM.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            ViewBag.Categories = new SelectList(db.DANHMUC.Where(d => d.TRANGTHAI == "Hiển thị"), "MADM", "TENDM", product.MADM);
            return View(product);
        }

        // POST: Owner/EditProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProduct(SANPHAM product, HttpPostedFileBase imageFile)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var existingProduct = db.SANPHAM.Find(product.MASP);
                    if (existingProduct != null)
                    {
                        existingProduct.TENSP = product.TENSP;
                        existingProduct.MOTA = product.MOTA;
                        existingProduct.GIA = product.GIA;
                        existingProduct.SOLUONGTON = product.SOLUONGTON;
                        existingProduct.MADM = product.MADM;
                        existingProduct.TRANGTHAI = product.TRANGTHAI;

                        // Xử lý upload hình ảnh mới (nếu có)
                        if (imageFile != null && imageFile.ContentLength > 0)
                        {
                            string fileName = System.IO.Path.GetFileName(imageFile.FileName);
                            string path = System.IO.Path.Combine(Server.MapPath("~/Content/Images/Products/"), fileName);
                            imageFile.SaveAs(path);
                            existingProduct.HINHANH = fileName;
                        }

                        db.SaveChanges();
                        TempData["Success"] = "Cập nhật sản phẩm thành công!";
                        return RedirectToAction("Products");
                    }
                }
            }


            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            }

            ViewBag.Categories = new SelectList(db.DANHMUC.Where(d => d.TRANGTHAI == "Hiển thị"), "MADM", "TENDM", product.MADM);
            return View(product);
        }

        // GET: Owner/StopSelling/{id}
        public ActionResult StopSelling(string id)
        {
            if (string.IsNullOrEmpty(id))
                return HttpNotFound();

            var product = db.SANPHAM
                            .Include("DANHMUC")
                            .FirstOrDefault(p => p.MASP == id);

            if (product == null)
                return HttpNotFound();

            return View(product);
        }

        // POST: Owner/StopSelling
        [HttpPost, ActionName("StopSelling")]
        [ValidateAntiForgeryToken]
        public ActionResult StopSellingProduct(string id)
        {
            var product = db.SANPHAM.Find(id);
            if (product != null)
            {
                // 🔴 XÓA MỀM
                product.TRANGTHAI = "Ngừng bán";
                db.SaveChanges();

                TempData["Success"] = "Ngừng bán sản phẩm thành công!";
            }

            return RedirectToAction("Products");
        }

        // GET: Owner/DeleteProduct/{id}
        public ActionResult DeleteProduct(string id)
        {
            if (string.IsNullOrEmpty(id))
                return HttpNotFound();

            var product = db.SANPHAM
                            .Include("DANHMUC")
                            .FirstOrDefault(p => p.MASP == id);

            if (product == null)
                return HttpNotFound();

            return View(product);
        }

        // POST: Owner/DeleteProduct
        [HttpPost, ActionName("DeleteProduct")]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmDeleteProduct(string id)
        {
            try
            {
                var product = db.SANPHAM.Find(id);

                if (product != null)
                {
                    // ⚠️ HARD DELETE - Xóa hẳn khỏi database
                    // Cần kiểm tra ràng buộc FK trước khi xóa
                    db.SANPHAM.Remove(product);
                    db.SaveChanges();

                    TempData["Success"] = "Xóa sản phẩm thành công!";
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy sản phẩm!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Không thể xóa sản phẩm! " + ex.Message;
            }

            return RedirectToAction("Products");
        }
        #endregion

        #region DANH MỤC *****ĐÃ XONG*****
        // GET: Owner/Categories
        public ActionResult Categories()
        {
            ViewBag.Title = "Quản lý danh mục";
            var categories = db.DANHMUC.ToList();
            return View(categories);
        }

        // GET: Owner/CreateCategory
        public ActionResult CreateCategory()
        {
            ViewBag.Title = "Thêm danh mục";
            return View();
        }

        // POST: Owner/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCategory(DANHMUC model)
        {
            if (ModelState.IsValid)
            {
                model.MADM = GenerateNewCode("DM", db.DANHMUC.Select(d => d.MADM).ToList());
                model.TRANGTHAI = "Hiển thị";

                db.DANHMUC.Add(model);
                db.SaveChanges();

                TempData["Success"] = "Thêm danh mục thành công!";
                return RedirectToAction("Categories");
            }

            return View(model);
        }

        // GET: Owner/EditCategory/{id}
        public ActionResult EditCategory(string id)
        {
            var category = db.DANHMUC.Find(id);
            if (category == null)
                return HttpNotFound();

            return View(category);
        }

        // POST: Owner/EditCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCategory(DANHMUC model)
        {
            if (ModelState.IsValid)
            {
                var category = db.DANHMUC.Find(model.MADM);
                if (category != null)
                {
                    category.TENDM = model.TENDM;
                    category.TRANGTHAI = model.TRANGTHAI;
                    db.SaveChanges();

                    TempData["Success"] = "Cập nhật danh mục thành công!";
                    return RedirectToAction("Categories");
                }
            }
            return View(model);
        }

        // GET: Owner/DeleteCategory/{id}
        public ActionResult DeleteCategory(string id)
        {
            var category = db.DANHMUC.Find(id);
            if (category == null)
                return HttpNotFound();

            return View(category);
        }

        // POST: Owner/DeleteCategory
        [HttpPost, ActionName("DeleteCategory")]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmDeleteCategory(string id)
        {
            var category = db.DANHMUC.Find(id);

            if (category != null)
            {
                // 🔴 XÓA MỀM
                category.TRANGTHAI = "Ẩn";
                db.SaveChanges();

                TempData["Success"] = "Ẩn danh mục thành công!";
            }

            return RedirectToAction("Categories");
        }

        // GET: Owner/ShowCategory - Hiển thị lại danh mục
        public ActionResult ShowCategory(string id)
        {
            var category = db.DANHMUC.Find(id);

            if (category != null)
            {
                category.TRANGTHAI = "Hiển thị";
                db.SaveChanges();

                TempData["Success"] = "Hiển thị danh mục thành công!";
            }

            return RedirectToAction("Categories");
        }
        #endregion

        #region ĐƠN HÀNG *****ĐÃ XONG*****
        // GET: Owner/Orders - Quản lý đơn hàng
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

        // GET: Owner/OrderDetails/{id}
        public ActionResult OrderDetails(string id)
        {
            ViewBag.Title = "Chi tiết đơn hàng";

            var order = db.HOADON_BAN
                .Include("KHACHHANG")
                .FirstOrDefault(o => o.MAHD_BAN == id);

            var details = db.CTHD_BAN
                .Include("SANPHAM")
                .Where(ct => ct.MAHD_BAN == id)
                .ToList();

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
                return RedirectToAction("OrderDetails", new { id });
            }

            // ❌ Không cho quay lui trạng thái
            if (!IsValidNextStatus(currentStatus, status))
            {
                TempData["Error"] = "Chuyển trạng thái không hợp lệ!";
                return RedirectToAction("OrderDetails", new { id });
            }

            // ✅ Cập nhật hợp lệ
            order.TRANGTHAI = status;
            db.SaveChanges();

            TempData["Success"] = "Cập nhật trạng thái thành công!";
            return RedirectToAction("OrderDetails", new { id });
        }
        #endregion

        #region KHÁCH HÀNG *****ĐÃ XONG*****
        // GET: Owner/Customers - Quản lý khách hàng
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
        #endregion

        #region NHÂN VIÊN *****ĐÃ XONG*****
        // GET: Owner/Staff - Quản lý nhân viên
        public ActionResult Staff()
        {
            ViewBag.Title = "Quản lý nhân viên";
            var staff = db.NHANVIEN.ToList();
            return View(staff);
        }

        public ActionResult StaffDetail(string id)
        {
            var kh = db.NHANVIEN.Find(id);
            return View(kh);
        }

        // =============================
        // 3. THÊM NHÂN VIÊN (GET)
        // =============================
        public ActionResult CreateStaff()
        {
            NHANVIEN nv = new NHANVIEN();
            nv.MANV = GenerateNewCode("NV", db.NHANVIEN.Select(s => s.MANV).ToList());//tạo mã random
            return View(nv);
        }

        // =============================
        // 3. THÊM NHÂN VIÊN (POST) - Kèm tài khoản
        // =============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateStaff(NHANVIEN nv, string username, string password)
        {
            if (ModelState.IsValid)
            {
                // Validate thông tin tài khoản
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError("", "Vui lòng nhập tên đăng nhập và mật khẩu cho nhân viên!");
                    return View(nv);
                }

                // Kiểm tra username đã tồn tại chưa
                if (db.ACCOUNT.Any(a => a.USERNAME == username))
                {
                    ModelState.AddModelError("", "Tên đăng nhập đã tồn tại!");
                    return View(nv);
                }

                // Kiểm tra email nhân viên đã tồn tại chưa
                if (!string.IsNullOrEmpty(nv.EMAIL) && db.ACCOUNT.Any(a => a.EMAIL == nv.EMAIL))
                {
                    ModelState.AddModelError("", "Email đã được sử dụng!");
                    return View(nv);
                }

                try
                {
                    // DISABLE TRIGGERS
                    db.Database.ExecuteSqlCommand("ALTER TABLE NHANVIEN DISABLE TRIGGER ALL");
                    db.Database.ExecuteSqlCommand("ALTER TABLE ACCOUNT DISABLE TRIGGER ALL");

                    try
                    {
                        // 1. INSERT NHÂN VIÊN
                        nv.NGAYSINH = nv.NGAYSINH?.Date;
                        nv.NGAYVAOLAM = DateTime.Now;
                        nv.TRANGTHAI = "Đang làm";

                        string sqlNV = @"INSERT INTO NHANVIEN (MANV, HOTENNV, GIOITINH, NGAYSINH, NGAYVAOLAM, SDT, EMAIL, DIACHI, CHUCVU, LUONGCOBAN, TRANGTHAI) 
                                        VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10)";
                        db.Database.ExecuteSqlCommand(sqlNV,
                            nv.MANV, nv.HOTENNV, nv.GIOITINH, nv.NGAYSINH, nv.NGAYVAOLAM,
                            nv.SDT, nv.EMAIL, nv.DIACHI ?? "", nv.CHUCVU, nv.LUONGCOBAN, nv.TRANGTHAI);

                        // 2. INSERT ACCOUNT
                        string newAccountID = GenerateNewCode("ACC", db.ACCOUNT.Select(a => a.USERID).ToList());
                        string vaiTro = nv.CHUCVU == "Chủ shop" ? "Chủ shop" : "Nhân viên";

                        string sqlAccount = @"INSERT INTO ACCOUNT (USERID, USERNAME, PASSWORDHASH, VAITRO, MANV, MAKH, EMAIL, SDT, TRANGTHAI) 
                                             VALUES (@p0, @p1, @p2, @p3, @p4, NULL, @p5, @p6, @p7)";
                        db.Database.ExecuteSqlCommand(sqlAccount,
                            newAccountID, username, GetMD5Hash(password), vaiTro, nv.MANV, nv.EMAIL, nv.SDT, "Hoạt động");
                    }
                    finally
                    {
                        // ENABLE LẠI TRIGGERS
                        db.Database.ExecuteSqlCommand("ALTER TABLE NHANVIEN ENABLE TRIGGER ALL");
                        db.Database.ExecuteSqlCommand("ALTER TABLE ACCOUNT ENABLE TRIGGER ALL");
                    }

                    TempData["Success"] = "Thêm nhân viên và tài khoản thành công!";
                    return RedirectToAction("Staff");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                    return View(nv);
                }
            }
            return View(nv);
        }

        // =============================
        // 4. SỬA NHÂN VIÊN (GET)
        // =============================
        public ActionResult EditStaff(string id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var nv = db.NHANVIEN.Find(id);
            if (nv == null) return HttpNotFound();

            // Lấy thông tin tài khoản (nếu có)
            ViewBag.Account = db.ACCOUNT.FirstOrDefault(a => a.MANV == id);

            return View(nv);
        }

        // =============================
        // 4. SỬA NHÂN VIÊN (POST)
        // =============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditStaff(NHANVIEN nv, bool resetPassword = false)
        {
            try
            {
                var existingNV = db.NHANVIEN.Find(nv.MANV);
                if (existingNV == null) return HttpNotFound();

                // 🔒 Validation: Ngăn owner tự nghỉ việc chính mình
                string currentStaffId = Session["StaffID"]?.ToString();
                if (currentStaffId == nv.MANV && nv.TRANGTHAI == "Nghỉ việc")
                {
                    TempData["Error"] = "Bạn không thể tự nghỉ việc! Vui lòng liên hệ quản trị viên khác.";
                    return RedirectToAction("EditStaff", new { id = nv.MANV });
                }

                // DISABLE chỉ ACCOUNT trigger (không disable NHANVIEN để trigger auto-sync chạy)
                db.Database.ExecuteSqlCommand("ALTER TABLE ACCOUNT DISABLE TRIGGER ALL");


                try
                {
                    // ✅ UPDATE NHÂN VIÊN - Trigger sẽ tự động sync ACCOUNT
                    string sqlUpdateNV = @"UPDATE NHANVIEN 
                                          SET HOTENNV = @p0, GIOITINH = @p1, NGAYSINH = @p2, 
                                              SDT = @p3, EMAIL = @p4, DIACHI = @p5, 
                                              CHUCVU = @p6, LUONGCOBAN = @p7, TRANGTHAI = @p8
                                          WHERE MANV = @p9";

                    db.Database.ExecuteSqlCommand(sqlUpdateNV,
                        nv.HOTENNV, nv.GIOITINH, nv.NGAYSINH?.Date,
                        nv.SDT, nv.EMAIL, nv.DIACHI ?? "",
                        nv.CHUCVU, nv.LUONGCOBAN, nv.TRANGTHAI,
                        nv.MANV);

                    // Reset mật khẩu nếu được yêu cầu
                    if (resetPassword)
                    {
                        var account = db.ACCOUNT.FirstOrDefault(a => a.MANV == nv.MANV);
                        if (account != null)
                        {
                            string newPasswordHash = GetMD5Hash("123456");
                            string sqlResetPassword = @"UPDATE ACCOUNT SET PASSWORDHASH = @p0 WHERE MANV = @p1";
                            db.Database.ExecuteSqlCommand(sqlResetPassword, newPasswordHash, nv.MANV);

                            TempData["Success"] = "Cập nhật nhân viên và reset mật khẩu về 123456 thành công!";
                        }
                        else
                        {
                            TempData["Success"] = "Cập nhật nhân viên thành công!";
                        }
                    }
                    else
                    {
                        TempData["Success"] = "Cập nhật nhân viên thành công!";
                    }
                }
                finally
                {
                    // ENABLE LẠI ACCOUNT TRIGGER
                    db.Database.ExecuteSqlCommand("ALTER TABLE ACCOUNT ENABLE TRIGGER ALL");
                }

                return RedirectToAction("Staff");
            }
            catch (Exception ex)
            {
                // ENSURE TRIGGERS ARE RE-ENABLED EVEN ON ERROR
                try
                {
                    db.Database.ExecuteSqlCommand("ALTER TABLE ACCOUNT ENABLE TRIGGER ALL");
                }
                catch { }

                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("EditStaff", new { id = nv.MANV });
            }
        }

        // =============================
        // 5. XÓA (GET)
        // =============================
        public ActionResult DeleteStaff(string id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var nv = db.NHANVIEN.Find(id);
            if (nv == null) return HttpNotFound();
            return View(nv);
        }

        // =============================
        // 5. XÓA (POST)
        // =============================
        [HttpPost, ActionName("DeleteStaff")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            try
            {
                var nv = db.NHANVIEN.Find(id);
                if (nv != null)
                {
                    // Kiểm tra xem có tài khoản liên kết không
                    var account = db.ACCOUNT.FirstOrDefault(a => a.MANV == id);
                    if (account != null)
                    {
                        // Xóa tài khoản trước
                        db.ACCOUNT.Remove(account);
                    }

                    // Xóa nhân viên
                    db.NHANVIEN.Remove(nv);
                    db.SaveChanges();

                    TempData["Success"] = "Xóa nhân viên thành công!";
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy nhân viên!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Không thể xóa nhân viên! " + ex.Message;
            }

            return RedirectToAction("Staff");
        }
        #endregion

        #region XƯỞNG IN *****ĐÃ XONG*****
        // GET: Owner/Suppliers - Quản lý xưởng in
        public ActionResult Suppliers()
        {
            ViewBag.Title = "Quản lý xưởng in";
            var suppliers = db.XUONGIN.ToList();
            return View(suppliers);
        }

        // GET: Owner/SupplierDetail/{id} - Chi tiết xưởng in
        public ActionResult SupplierDetail(string id)
        {
            if (string.IsNullOrEmpty(id))
                return HttpNotFound();

            var supplier = db.XUONGIN.Find(id);
            if (supplier == null)
                return HttpNotFound();

            // Lấy lịch sử phiếu nhập từ xưởng in này (Include NHANVIEN để tránh lazy loading)
            var importHistory = db.PHIEUNHAP
                .Include("NHANVIEN")
                .Where(p => p.MAXI == id)
                .OrderByDescending(p => p.NGAYNHAP)
                .ToList();

            ViewBag.ImportHistory = importHistory;
            ViewBag.Title = "Chi tiết xưởng in";

            return View(supplier);
        }

        public ActionResult CreateSupplier()
        {
            XUONGIN xi = new XUONGIN();
            xi.MAXI = GenerateNewCode("XI", db.XUONGIN.Select(s => s.MAXI).ToList());//tạo mã random
            return View(xi);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateSupplier(XUONGIN model)
        {
            if (ModelState.IsValid)
            {
                // kiểm tra trùng mã
                var check = db.XUONGIN.Find(model.MAXI);
                if (check != null)
                {
                    ModelState.AddModelError("", "Mã xưởng đã tồn tại");
                    return View(model);
                }

                // mặc định trạng thái
                model.TRANGTHAI = "Hoạt động";

                db.XUONGIN.Add(model);
                db.SaveChanges();

                return RedirectToAction("Suppliers");
            }

            return View(model);
        }

        public ActionResult EditSupplier(string id)
        {
            if (id == null) return HttpNotFound();

            var xi = db.XUONGIN.Find(id);
            if (xi == null) return HttpNotFound();

            return View(xi);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditSupplier(XUONGIN model)
        {
            if (ModelState.IsValid)
            {
                var xi = db.XUONGIN.Find(model.MAXI);
                if (xi == null) return HttpNotFound();

                xi.TENXI = model.TENXI;
                xi.DIACHI = model.DIACHI;
                xi.SDT = model.SDT;
                xi.EMAIL = model.EMAIL;
                xi.NGUOILIENHE = model.NGUOILIENHE;
                xi.TRANGTHAI = model.TRANGTHAI;
                xi.GHICHU = model.GHICHU;

                db.SaveChanges();
                return RedirectToAction("Suppliers");
            }
            return View(model);
        }


        // GET: Owner/StopSupplier - NGỪNG HỢP TÁC (Hiển thị form nhập nguyên nhân)
        public ActionResult StopSupplier(string id)
        {
            var supplier = db.XUONGIN.Find(id);
            if (supplier == null)
                return HttpNotFound();

            return View(supplier);
        }

        // POST: Owner/StopSupplier - NGỪNG HỢP TÁC (Lưu nguyên nhân)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmStopSupplier(string id, string ghichu)
        {
            var supplier = db.XUONGIN.Find(id);
            if (supplier == null)
                return HttpNotFound();

            // Validate: Bắt buộc phải nhập nguyên nhân
            if (string.IsNullOrWhiteSpace(ghichu))
            {
                ModelState.AddModelError("", "Vui lòng nhập nguyên nhân ngừng hợp tác");
                return View("StopSupplier", supplier);
            }

            // Cập nhật trạng thái và ghi chú
            supplier.TRANGTHAI = "Ngừng hợp tác";
            supplier.GHICHU = ghichu.Trim();
            db.SaveChanges();

            TempData["Success"] = "Ngừng hợp tác với xưởng in thành công!";
            return RedirectToAction("Suppliers");
        }

        // GET: Owner/DeleteSupplier - XÓA VĨNH VIỄN (Hard Delete)
        public ActionResult DeleteSupplier(string id)
        {
            if (string.IsNullOrEmpty(id))
                return HttpNotFound();

            var supplier = db.XUONGIN
                            .FirstOrDefault(x => x.MAXI == id);

            if (supplier == null)
                return HttpNotFound();

            return View(supplier);
        }

        // POST: Owner/DeleteSupplier - XÓA VĨNH VIỄN
        [HttpPost, ActionName("DeleteSupplier")]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmDeleteSupplier(string id)
        {
            try
            {
                var supplier = db.XUONGIN.Find(id);

                if (supplier != null)
                {
                    // Hard Delete - Xóa khỏi database
                    db.XUONGIN.Remove(supplier);
                    db.SaveChanges();

                    TempData["Success"] = "Xóa xưởng in thành công!";
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy xưởng in!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Không thể xóa xưởng in! " + ex.Message;
            }

            return RedirectToAction("Suppliers");
        }
        #endregion

        #region PHIẾU NHẬP *****ĐÃ XONG*****
        //GET: Owner/ImportReceipts - Quản lý phiếu nhập
        // ======= IMPORT RECEIPTS ========
        public ActionResult ImportReceipts()
        {
            ViewBag.Title = "Phiếu nhập hàng";
            var imports = db.PHIEUNHAP.ToList();
            return View(imports);
        }

        // GET: Owner/CreateImport
        public ActionResult CreateImport()
        {
            ViewBag.XuongIn = db.XUONGIN.ToList();           // nhà cung cấp
            ViewBag.DanhMucList = db.DANHMUC.ToList();       // danh mục sản phẩm
            ViewBag.Products = db.SANPHAM.ToList();          // tất cả sản phẩm
            return View();
        }

        // POST: Owner/CreateImport
        [HttpPost]
        public ActionResult CreateImport(PHIEUNHAP model, string[] productId, int[] qty, decimal[] price)
        {
            try
            {
                // Validate Session
                if (Session["StaffID"] == null)
                {
                    TempData["Error"] = "Phiên đăng nhập đã hết hạn!";
                    return RedirectToAction("Login", "Account");
                }

                // Validate input
                if (productId == null || productId.Length == 0)
                {
                    TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm!";
                    ViewBag.XuongIn = db.XUONGIN.ToList();
                    ViewBag.DanhMucList = db.DANHMUC.ToList();
                    ViewBag.Products = db.SANPHAM.ToList();
                    return View(model);
                }

                if (qty == null || price == null || productId.Length != qty.Length || productId.Length != price.Length)
                {
                    TempData["Error"] = "Dữ liệu không hợp lệ!";
                    ViewBag.XuongIn = db.XUONGIN.ToList();
                    ViewBag.DanhMucList = db.DANHMUC.ToList();
                    ViewBag.Products = db.SANPHAM.ToList();
                    return View(model);
                }

                // Generate unique MAPN
                model.MAPN = GenerateNewCode("PN", db.PHIEUNHAP.Select(p => p.MAPN).ToList());
                model.MANV = Session["StaffID"].ToString();
                model.NGAYNHAP = DateTime.Now;

                db.PHIEUNHAP.Add(model);

                decimal tongTien = 0;

                for (int i = 0; i < productId.Length; i++)
                {
                    // Validate quantity and price
                    if (qty[i] <= 0 || price[i] <= 0)
                    {
                        TempData["Error"] = "Số lượng và đơn giá phải lớn hơn 0!";
                        ViewBag.XuongIn = db.XUONGIN.ToList();
                        ViewBag.DanhMucList = db.DANHMUC.ToList();
                        ViewBag.Products = db.SANPHAM.ToList();
                        return View(model);
                    }

                    var ct = new CHITIETPHIEUNHAP
                    {
                        MAPN = model.MAPN,
                        MASP = productId[i],
                        SOLUONG = qty[i],
                        DONGIA = price[i]
                    };
                    db.CHITIETPHIEUNHAP.Add(ct);

                    var sp = db.SANPHAM.Find(productId[i]);
                    if (sp != null)
                        sp.SOLUONGTON = (sp.SOLUONGTON ?? 0) + qty[i];

                    tongTien += qty[i] * price[i];
                }

                model.TONGTIEN = tongTien;

                db.SaveChanges();

                TempData["Success"] = "Thêm phiếu nhập thành công!";
                return RedirectToAction("ImportReceipts");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                ViewBag.XuongIn = db.XUONGIN.ToList();
                ViewBag.DanhMucList = db.DANHMUC.ToList();
                ViewBag.Products = db.SANPHAM.ToList();
                return View(model);
            }
        }
        #endregion

        #region BÁO CÁO *****ĐÃ XONG*****
        // GET: Owner/Reports - Báo cáo
        public ActionResult Reports()
        {
            ViewBag.Title = "Báo cáo thống kê";

            //// Doanh thu theo tháng
            //var monthlyRevenue = db.HOADON_BAN
            //    .Where(h => h.TRANGTHAI == "Hoàn thành")
            //    .GroupBy(h => new { h.NGAYLAP.Value.Year, h.NGAYLAP.Value.Month })
            //    .Select(g => new
            //    {
            //        Year = g.Key.Year,
            //        Month = g.Key.Month,
            //        Revenue = g.Sum(h => h.TONGTIEN)
            //    })
            //    .OrderByDescending(x => x.Year)
            //    .ThenByDescending(x => x.Month)
            //    .Take(12)
            //    .ToList();

            //ViewBag.MonthlyRevenue = monthlyRevenue;

            return View();
        }

        [HttpGet]
        public JsonResult GetRevenue(string type, string fromDate, string toDate)
        {
            DateTime from, to;

            // Xử lý theo type
            if (type == "day") // yyyy-MM-dd
            {
                if (!DateTime.TryParse(fromDate, out from)) from = DateTime.Today;
                if (!DateTime.TryParse(toDate, out to)) to = DateTime.Today;

                from = from.Date;
                to = to.Date.AddDays(1).AddTicks(-1);
            }
            else if (type == "month") // yyyy-MM
            {
                try
                {
                    var fromParts = fromDate.Split('-'); // ["2025", "01"]
                    var toParts = toDate.Split('-');

                    from = new DateTime(int.Parse(fromParts[0]), int.Parse(fromParts[1]), 1);
                    to = new DateTime(int.Parse(toParts[0]), int.Parse(toParts[1]), 1)
                             .AddMonths(1).AddTicks(-1);
                }
                catch
                {
                    var today = DateTime.Today;
                    from = new DateTime(today.Year, today.Month, 1);
                    to = from.AddMonths(1).AddTicks(-1);
                }
            }
            else if (type == "year") // yyyy
            {
                try
                {
                    int fromYear = int.Parse(fromDate);
                    int toYear = int.Parse(toDate);
                    from = new DateTime(fromYear, 1, 1);
                    to = new DateTime(toYear, 12, 31, 23, 59, 59, 999);
                }
                catch
                {
                    int year = DateTime.Today.Year;
                    from = new DateTime(year, 1, 1);
                    to = new DateTime(year, 12, 31, 23, 59, 59, 999);
                }
            }
            else
            {
                from = DateTime.Today;
                to = DateTime.Today.AddDays(1).AddTicks(-1);
            }

            // Lấy dữ liệu
            var query = db.HOADON_BAN
                .Where(h => h.TRANGTHAI == "Hoàn thành"
                            && h.NGAYLAP.HasValue
                            && h.NGAYLAP.Value >= from
                            && h.NGAYLAP.Value <= to)
                .ToList();

            var result = new List<RevenueDto>();

            if (query.Any())
            {
                if (type == "day")
                {
                    result = query
                        .GroupBy(h => h.NGAYLAP.Value.Date)
                        .Select(g => new RevenueDto
                        {
                            Label = g.Key.ToString("yyyy-MM-dd"),
                            Total = g.Sum(h => h.TONGTIEN ?? 0)
                        })
                        .OrderBy(r => r.Label)
                        .ToList();
                }
                else if (type == "month")
                {
                    result = query
                        .GroupBy(h => new { h.NGAYLAP.Value.Year, h.NGAYLAP.Value.Month })
                        .Select(g => new RevenueDto
                        {
                            Label = g.Key.Year + "-" + g.Key.Month.ToString("D2"),
                            Total = g.Sum(h => h.TONGTIEN ?? 0)
                        })
                        .OrderBy(r => r.Label)
                        .ToList();
                }
                else if (type == "year")
                {
                    result = query
                        .GroupBy(h => h.NGAYLAP.Value.Year)
                        .Select(g => new RevenueDto
                        {
                            Label = g.Key.ToString(),
                            Total = g.Sum(h => h.TONGTIEN ?? 0)
                        })
                        .OrderBy(r => r.Label)
                        .ToList();
                }
            }

            return Json(new
            {
                labels = result.Select(r => r.Label).ToArray(),
                data = result.Select(r => r.Total).ToArray()
            }, JsonRequestBehavior.AllowGet);
        }



        [HttpGet]
        public JsonResult GetTopProducts(string type, string fromDate, string toDate)
        {
            DateTime from, to;

            if (type == "day") // yyyy-MM-dd
            {
                if (!DateTime.TryParse(fromDate, out from)) from = DateTime.Today;
                if (!DateTime.TryParse(toDate, out to)) to = DateTime.Today;

                from = from.Date;
                to = to.Date.AddDays(1).AddTicks(-1);
            }
            else if (type == "month") // yyyy-MM
            {
                try
                {
                    var fromParts = fromDate.Split('-'); // ["2025", "01"]
                    var toParts = toDate.Split('-');

                    from = new DateTime(int.Parse(fromParts[0]), int.Parse(fromParts[1]), 1);
                    to = new DateTime(int.Parse(toParts[0]), int.Parse(toParts[1]), 1)
                             .AddMonths(1).AddTicks(-1);
                }
                catch
                {
                    var today = DateTime.Today;
                    from = new DateTime(today.Year, today.Month, 1);
                    to = from.AddMonths(1).AddTicks(-1);
                }
            }
            else if (type == "year") // yyyy
            {
                try
                {
                    int fromYear = int.Parse(fromDate);
                    int toYear = int.Parse(toDate);
                    from = new DateTime(fromYear, 1, 1);
                    to = new DateTime(toYear, 12, 31, 23, 59, 59, 999);
                }
                catch
                {
                    int year = DateTime.Today.Year;
                    from = new DateTime(year, 1, 1);
                    to = new DateTime(year, 12, 31, 23, 59, 59, 999);
                }
            }
            else
            {
                from = DateTime.Today;
                to = DateTime.Today.AddDays(1).AddTicks(-1);
            }

            var topProducts = db.CTHD_BAN
                .Where(c => c.HOADON_BAN.NGAYLAP.HasValue
                            && c.HOADON_BAN.NGAYLAP.Value >= from
                            && c.HOADON_BAN.NGAYLAP.Value <= to)
                .ToList()
                .GroupBy(c => new { c.MASP, c.SANPHAM.TENSP })
                .Select(g => new
                {
                    Name = g.Key.TENSP,
                    Quantity = g.Sum(c => c.SOLUONG ?? 0)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(10)
                .ToList();

            return Json(new
            {
                labels = topProducts.Select(p => p.Name).ToArray(),
                data = topProducts.Select(p => p.Quantity).ToArray()
            }, JsonRequestBehavior.AllowGet);
        }


        #endregion

        #region PROFILE *****ĐÃ XONG*****
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
            return prefix + (maxNumber + 1).ToString();
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