using LTW_QLBH_HUNMYI.Filters;
using LTW_QLBH_HUNMYI.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LTW_QLBH_HUNMYI.Controllers
{
    //[CustomAuthorize(AllowedRoles = new[] { "Khách" })]
    public class CustomerController : Controller
    {
        private QLBH_HUNMYI_LTW_FINALEntities1 db = new QLBH_HUNMYI_LTW_FINALEntities1();

        #region TRANG CHỦ

        // GET: Customer - Trang chủ
        public ActionResult Index()
        {
            ViewBag.Title = "Trang chủ";

            try
            {
                // Lấy danh mục
                var categories = db.DANHMUC
                    .Where(d => d.TRANGTHAI == "Hiển thị")
                    .ToList();
                ViewBag.Categories = categories;

                // Sản phẩm mới nhất (8 sản phẩm)
                var newProducts = db.DANHMUC
                    .Where(s => s.TRANGTHAI == "Đang bán")
                    .Take(8)
                    .ToList();

                // Sản phẩm bán chạy - giả lập bằng sản phẩm có số lượng tồn thấp
                var hotProducts = db.SANPHAM
                    .Where(s => s.TRANGTHAI == "Đang bán" && s.SOLUONGTON > 0)
                    .OrderBy(s => s.SOLUONGTON)
                    .Take(8)
                    .ToList();

                ViewBag.NewProducts = newProducts;
                ViewBag.HotProducts = hotProducts;

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Có lỗi xảy ra: " + ex.Message;
                return View();
            }
        }
        #endregion

        #region SẢN PHẨM
        // GET: Customer/Products - Danh sách sản phẩm
        public ActionResult Products(string category = null, string search = null, string sortBy = null)
        {
            ViewBag.Title = "Sản phẩm";

            try
            {
                var products = db.SANPHAM
                    .Include(s => s.DANHMUC)
                    .Where(s => s.TRANGTHAI == "Đang bán")
                    .AsQueryable();

                // Lọc theo danh mục
                if (!string.IsNullOrEmpty(category))
                {
                    products = products.Where(p => p.MADM == category);
                    var cat = db.DANHMUC.Find(category);
                    ViewBag.CategoryName = cat?.TENDM;
                    ViewBag.SelectedCategory = category;
                }

                // Tìm kiếm
                if (!string.IsNullOrEmpty(search))
                {
                    products = products.Where(p =>
                        p.TENSP.Contains(search) ||
                        p.MOTA.Contains(search));
                    ViewBag.SearchKeyword = search;
                }

                // Sắp xếp
                switch (sortBy)
                {
                    case "price_asc":
                        products = products.OrderBy(p => p.GIA);
                        ViewBag.SortBy = "price_asc";
                        break;
                    case "price_desc":
                        products = products.OrderByDescending(p => p.GIA);
                        ViewBag.SortBy = "price_desc";
                        break;
                    case "name_asc":
                        products = products.OrderBy(p => p.TENSP);
                        ViewBag.SortBy = "name_asc";
                        break;
                    case "name_desc":
                        products = products.OrderByDescending(p => p.TENSP);
                        ViewBag.SortBy = "name_desc";
                        break;
                    default:
                        products = products.OrderByDescending(p => p.NGAYTAO);
                        ViewBag.SortBy = "newest";
                        break;
                }

                var productList = products.ToList();

                // Lấy danh sách danh mục để hiển thị filter
                ViewBag.Categories = db.DANHMUC
                    .Where(d => d.TRANGTHAI == "Hiển thị")
                    .ToList();

                return View(productList);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Có lỗi xảy ra: " + ex.Message;
                return View();
            }
        }

        // GET: Customer/ProductDetail/{id} - Chi tiết sản phẩm
        public ActionResult ProductDetail(string id)
        {
            ViewBag.Title = "Chi tiết sản phẩm";

            try
            {
                var product = db.SANPHAM
                    .Include(s => s.DANHMUC)
                    .FirstOrDefault(p => p.MASP.Trim() == id.Trim());

                if (product == null)
                {
                    TempData["Error"] = "Không tìm thấy sản phẩm!";
                    return RedirectToAction("Products");
                }

                // Sản phẩm liên quan (cùng danh mục)
                var relatedProducts = db.SANPHAM
                    .Where(s => s.MADM == product.MADM &&
                               s.MASP != id &&
                               s.TRANGTHAI == "Đang bán")
                    .Take(4)
                    .ToList();
                ViewBag.RelatedProducts = relatedProducts;

                return View(product);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Products");
            }
        }

        // GET: Customer/Search - Tìm kiếm sản phẩm
        public ActionResult Search(string keyword)
        {
            return RedirectToAction("Products", new { search = keyword });
        }

        #endregion

        #region GIỚI THIỆU
        public ActionResult About()
        {
            ViewBag.Title = "Giới thiệu";
            return View();
        }

        #endregion

        #region GIỎ HÀNG

        // GET: Customer/Cart - Giỏ hàng
        public ActionResult Cart()
        {
            ViewBag.Title = "Giỏ hàng";

            try
            {
                string customerId = Session["CustomerID"]?.ToString();
                var cart = db.GIOHANG.FirstOrDefault(g => g.MAKH == customerId);

                if (cart == null)
                {
                    ViewBag.CartItems = new System.Collections.Generic.List<CHITIET_GIOHANG>();
                    ViewBag.TotalAmount = 0;
                    return View();
                }

                var cartItems = db.CHITIET_GIOHANG
                    .Include(c => c.SANPHAM)
                    .Include(c => c.SANPHAM.DANHMUC)
                    .Where(c => c.MAGH == cart.MAGH)
                    .ToList();

                // Tính tổng tiền
                decimal totalAmount = (decimal)cartItems.Sum(c => c.SOLUONG * c.SANPHAM.GIA.Value);
                ViewBag.TotalAmount = totalAmount;

                return View(cartItems);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Có lỗi xảy ra: " + ex.Message;
                return View();
            }
        }

        // POST: Customer/AddToCart - Thêm vào giỏ hàng
        [HttpPost]
        public JsonResult AddToCart(string productId, int quantity = 1)
        {
            try
            {
                string customerId = Session["CustomerID"]?.ToString();
                if (string.IsNullOrEmpty(customerId))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });
                }

                // Lấy giỏ hàng của khách
                var cart = db.GIOHANG.FirstOrDefault(g => g.MAKH == customerId);
                if (cart == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy giỏ hàng!" });
                }

                // Kiểm tra sản phẩm
                var product = db.SANPHAM.Find(productId);
                if (product == null || product.TRANGTHAI != "Đang bán")
                {
                    return Json(new { success = false, message = "Sản phẩm không khả dụng!" });
                }

                if (product.SOLUONGTON < quantity)
                {
                    return Json(new { success = false, message = "Số lượng trong kho không đủ!" });
                }

                // Kiểm tra sản phẩm đã có trong giỏ chưa
                var cartItem = db.CHITIET_GIOHANG
                    .FirstOrDefault(c => c.MAGH == cart.MAGH && c.MASP == productId);

                if (cartItem != null)
                {
                    // Kiểm tra tổng số lượng sau khi thêm
                    if (product.SOLUONGTON < (cartItem.SOLUONG + quantity))
                    {
                        return Json(new { success = false, message = "Số lượng vượt quá tồn kho!" });
                    }
                    cartItem.SOLUONG += quantity;
                    cartItem.NGAYTHEM = DateTime.Now;
                }
                else
                {
                    // Thêm mới
                    cartItem = new CHITIET_GIOHANG
                    {
                        MAGH = cart.MAGH,
                        MASP = productId,
                        SOLUONG = quantity,
                        NGAYTHEM = DateTime.Now
                    };
                    db.CHITIET_GIOHANG.Add(cartItem);
                }

                db.SaveChanges();

                // Đếm tổng số lượng sản phẩm trong giỏ
                int cartCount = (int)db.CHITIET_GIOHANG
                    .Where(c => c.MAGH == cart.MAGH)
                    .Sum(c => c.SOLUONG);

                return Json(new
                {
                    success = true,
                    message = "Đã thêm vào giỏ hàng!",
                    cartCount = cartCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
        public ActionResult _Cart()
        {
            string customerId = Session["CustomerID"]?.ToString();
            // Lấy giỏ hàng của khách
            var cart = db.GIOHANG.FirstOrDefault(g => g.MAKH == customerId);
            cart.TONGSL = cart.CHITIET_GIOHANG.Where(c => c.MAGH == cart.MAGH)
                    .Sum(c => c.SOLUONG);
            return PartialView(cart);
        }

        // POST: Customer/UpdateCart - Cập nhật số lượng trong giỏ
        [HttpPost]
        public JsonResult UpdateCart(string productId, int quantity)
        {
            try
            {
                string customerId = Session["CustomerID"]?.ToString();
                var cart = db.GIOHANG.FirstOrDefault(g => g.MAKH == customerId);

                var cartItem = db.CHITIET_GIOHANG
                    .FirstOrDefault(c => c.MAGH == cart.MAGH && c.MASP == productId);

                if (cartItem != null)
                {
                    if (quantity > 0)
                    {
                        // Kiểm tra tồn kho
                        var product = db.SANPHAM.Find(productId);
                        if (product.SOLUONGTON < quantity)
                        {
                            return Json(new
                            {
                                success = false,
                                message = "Số lượng vượt quá tồn kho!"
                            });
                        }
                        cartItem.SOLUONG = quantity;
                    }
                    else
                    {
                        // Xóa khỏi giỏ nếu số lượng = 0
                        db.CHITIET_GIOHANG.Remove(cartItem);
                    }
                    db.SaveChanges();

                    // Tính lại tổng tiền
                    var cartItems = db.CHITIET_GIOHANG
                        .Include(c => c.SANPHAM)
                        .Where(c => c.MAGH == cart.MAGH)
                        .ToList();

                    decimal totalAmount = (decimal)cartItems.Sum(c => c.SOLUONG * c.SANPHAM.GIA.Value);
                    int cartCount = (int)cartItems.Sum(c => c.SOLUONG);

                    return Json(new
                    {
                        success = true,
                        totalAmount = totalAmount,
                        cartCount = cartCount
                    });
                }

                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Customer/RemoveFromCart - Xóa sản phẩm khỏi giỏ
        [HttpPost]
        public JsonResult RemoveFromCart(string productId)
        {
            try
            {
                string customerId = Session["CustomerID"]?.ToString();
                var cart = db.GIOHANG.FirstOrDefault(g => g.MAKH == customerId);

                var cartItem = db.CHITIET_GIOHANG
                    .FirstOrDefault(c => c.MAGH == cart.MAGH && c.MASP == productId);

                if (cartItem != null)
                {
                    db.CHITIET_GIOHANG.Remove(cartItem);
                    db.SaveChanges();

                    // Đếm lại số lượng
                    int cartCount = db.CHITIET_GIOHANG
                        .Where(c => c.MAGH == cart.MAGH)
                        .Sum(c => (int?)c.SOLUONG) ?? 0;

                    return Json(new
                    {
                        success = true,
                        message = "Đã xóa sản phẩm!",
                        cartCount = cartCount
                    });
                }

                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Customer/GetCartCount - Lấy số lượng sản phẩm trong giỏ
        [HttpGet]
        public JsonResult GetCartCount()
        {
            try
            {
                string customerId = Session["CustomerID"]?.ToString();
                if (string.IsNullOrEmpty(customerId))
                {
                    return Json(new { success = true, count = 0 }, JsonRequestBehavior.AllowGet);
                }

                var cart = db.GIOHANG.FirstOrDefault(g => g.MAKH == customerId);
                if (cart == null)
                {
                    return Json(new { success = true, count = 0 }, JsonRequestBehavior.AllowGet);
                }

                int count = db.CHITIET_GIOHANG
                    .Where(c => c.MAGH == cart.MAGH)
                    .Sum(c => (int?)c.SOLUONG) ?? 0;

                return Json(new { success = true, count = count }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region THANH TOÁN

        // GET: Customer/Checkout - Trang thanh toán
        public ActionResult Checkout()
        {
            ViewBag.Title = "Thanh toán";

            try
            {
                string customerId = Session["CustomerID"]?.ToString();
                var cart = db.GIOHANG.FirstOrDefault(g => g.MAKH == customerId);

                if (cart == null)
                {
                    TempData["Error"] = "Giỏ hàng trống!";
                    return RedirectToAction("Cart");
                }

                var cartItems = db.CHITIET_GIOHANG
                    .Include(c => c.SANPHAM)
                    .Where(c => c.MAGH == cart.MAGH)
                    .ToList();

                if (cartItems.Count == 0)
                {
                    TempData["Error"] = "Giỏ hàng trống!";
                    return RedirectToAction("Cart");
                }

                // Kiểm tra tồn kho trước khi thanh toán
                foreach (var item in cartItems)
                {
                    if (item.SANPHAM.SOLUONGTON < item.SOLUONG)
                    {
                        TempData["Error"] = $"Sản phẩm '{item.SANPHAM.TENSP}' không đủ hàng trong kho!";
                        return RedirectToAction("Cart");
                    }
                }

                var customer = db.KHACHHANG.Find(customerId);
                ViewBag.Customer = customer;

                // Tính tổng tiền
                decimal totalAmount = (decimal)cartItems.Sum(c => c.SOLUONG * c.SANPHAM.GIA.Value);
                ViewBag.TotalAmount = totalAmount;

                return View(cartItems);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Cart");
            }
        }

        // POST: Customer/PlaceOrder - Đặt hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(string hoTenNguoiNhan, string sdtNguoiNhan, string diaChiGiaoHang, string ghiChu)
        {
            try
            {
                string customerId = Session["CustomerID"]?.ToString();
                var cart = db.GIOHANG.FirstOrDefault(g => g.MAKH == customerId);

                var cartItems = db.CHITIET_GIOHANG
                    .Include(c => c.SANPHAM)
                    .Where(c => c.MAGH == cart.MAGH)
                    .ToList();

                if (cartItems.Count == 0)
                {
                    TempData["Error"] = "Giỏ hàng trống!";
                    return RedirectToAction("Cart");
                }

                // Kiểm tra tồn kho lần cuối
                foreach (var item in cartItems)
                {
                    var product = db.SANPHAM.Find(item.MASP);
                    if (product.SOLUONGTON < item.SOLUONG)
                    {
                        TempData["Error"] = $"Sản phẩm '{product.TENSP}' không đủ hàng trong kho!";
                        return RedirectToAction("Cart");
                    }
                }

                // Tạo mã hóa đơn mới
                string newMaHD = GenerateNewCode("HB", db.HOADON_BAN.Select(h => h.MAHD_BAN).ToList());
                decimal tongTien = (decimal)cartItems.Sum(c => c.SOLUONG * c.SANPHAM.GIA.Value);

                // Tạo hóa đơn
                HOADON_BAN hoaDon = new HOADON_BAN
                {
                    MAHD_BAN = newMaHD,
                    NGAYLAP = DateTime.Now,
                    MAKH = customerId,
                    TONGTIEN = tongTien,
                    TRANGTHAI = "Chờ xử lý",
                    GHICHU = !string.IsNullOrEmpty(ghiChu) ? ghiChu :
                             $"Người nhận: {hoTenNguoiNhan}, SĐT: {sdtNguoiNhan}, Địa chỉ: {diaChiGiaoHang}"
                };
                db.HOADON_BAN.Add(hoaDon);
                db.SaveChanges();
                // Thêm chi tiết hóa đơn
                foreach (var item in cartItems)
                {
                    CTHD_BAN chiTiet = new CTHD_BAN
                    {
                        MAHD_BAN = newMaHD,
                        MASP = item.MASP,
                        SOLUONG = item.SOLUONG,
                        DONGIA = item.SANPHAM.GIA.Value
                    };
                    db.CTHD_BAN.Add(chiTiet);

                    // Trừ số lượng tồn kho
                    var product = db.SANPHAM.Find(item.MASP);
                    if (product != null)
                    {
                        product.SOLUONGTON -= item.SOLUONG;

                        // Cập nhật trạng thái nếu hết hàng
                        if (product.SOLUONGTON <= 0)
                        {
                            product.TRANGTHAI = "Hết hàng";
                        }
                    }
                }
               
                // Xóa giỏ hàng
                foreach (var item in cartItems)
                {
                    db.CHITIET_GIOHANG.Remove(item);
                }

                db.SaveChanges();

                TempData["Success"] = "Đặt hàng thành công! Cảm ơn bạn đã mua hàng.";
                return RedirectToAction("OrderSuccess", new { id = newMaHD });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Checkout");
            }
        }

        // GET: Customer/OrderSuccess/{id} - Đặt hàng thành công
        public ActionResult OrderSuccess(string id)
        {
            ViewBag.Title = "Đặt hàng thành công";

            try
            {
                string customerId = Session["CustomerID"]?.ToString();
                var order = db.HOADON_BAN
                    .Include(h => h.CTHD_BAN)
                    .Include(h => h.CTHD_BAN.Select(c => c.SANPHAM))
                    .FirstOrDefault(h => h.MAHD_BAN == id && h.MAKH == customerId);

                if (order == null)
                {
                    return HttpNotFound();
                }

                return View(order);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Orders");
            }
        }

        #endregion

        #region ĐƠN HÀNG

        // GET: Customer/Orders - Đơn hàng của tôi
        public ActionResult Orders(string status = null)
        {
            ViewBag.Title = "Đơn hàng của tôi";

            try
            {
                string customerId = Session["CustomerID"]?.ToString();
                var orders = db.HOADON_BAN.Where(h => h.MAKH == customerId).AsQueryable();

                // Lọc theo trạng thái
                if (!string.IsNullOrEmpty(status))
                {
                    orders = orders.Where(h => h.TRANGTHAI == status);
                    ViewBag.SelectedStatus = status;
                }

                var orderList = orders
                    .OrderByDescending(h => h.NGAYLAP)
                    .ToList();

                // Đếm số đơn theo trạng thái
                ViewBag.PendingCount = db.HOADON_BAN.Count(h => h.MAKH == customerId && h.TRANGTHAI == "Chờ xử lý");
                ViewBag.ProcessingCount = db.HOADON_BAN.Count(h => h.MAKH == customerId && h.TRANGTHAI == "Đang xử lý");
                ViewBag.CompletedCount = db.HOADON_BAN.Count(h => h.MAKH == customerId && h.TRANGTHAI == "Hoàn thành");
                ViewBag.CancelledCount = db.HOADON_BAN.Count(h => h.MAKH == customerId && h.TRANGTHAI == "Đã hủy");

                return View(orderList);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Có lỗi xảy ra: " + ex.Message;
                return View();
            }
        }

        // GET: Customer/OrderDetail/{id} - Chi tiết đơn hàng
        public ActionResult OrderDetail(string id)
        {
            ViewBag.Title = "Chi tiết đơn hàng";

            try
            {
                string customerId = Session["CustomerID"]?.ToString();
                var order = db.HOADON_BAN
                    .Include(h => h.CTHD_BAN)
                    .Include(h => h.CTHD_BAN.Select(c => c.SANPHAM))
                    .Include(h => h.NHANVIEN)
                    .FirstOrDefault(h => h.MAHD_BAN == id && h.MAKH == customerId);

                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng!";
                    return RedirectToAction("Orders");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Orders");
            }
        }

        // POST: Customer/CancelOrder - Hủy đơn hàng
        [HttpPost]
        public JsonResult CancelOrder(string orderId)
        {
            try
            {
                string customerId = Session["CustomerID"]?.ToString();
                var order = db.HOADON_BAN
                    .FirstOrDefault(h => h.MAHD_BAN == orderId && h.MAKH == customerId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                // Chỉ cho phép hủy đơn ở trạng thái "Chờ xử lý"
                if (order.TRANGTHAI != "Chờ xử lý")
                {
                    return Json(new { success = false, message = "Không thể hủy đơn hàng này!" });
                }

                // Hoàn lại số lượng tồn kho
                var orderDetails = db.CTHD_BAN.Where(c => c.MAHD_BAN == orderId).ToList();
                foreach (var detail in orderDetails)
                {
                    var product = db.SANPHAM.Find(detail.MASP);
                    if (product != null)
                    {
                        product.SOLUONGTON += detail.SOLUONG;

                        // Cập nhật lại trạng thái nếu cần
                        if (product.TRANGTHAI == "Hết hàng" && product.SOLUONGTON > 0)
                        {
                            product.TRANGTHAI = "Đang bán";
                        }
                    }
                }

                order.TRANGTHAI = "Đã hủy";
                db.SaveChanges();

                return Json(new { success = true, message = "Đã hủy đơn hàng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        #endregion

        #region TÀI KHOẢN

        // GET: Customer/Profile - Thông tin tài khoản
        public ActionResult Profile()
        {
            ViewBag.Title = "Thông tin tài khoản";

            try
            {
                string customerId = Session["CustomerID"]?.ToString();
                var customer = db.KHACHHANG.Find(customerId);

                if (customer == null)
                {
                    return HttpNotFound();
                }

                // Thống kê đơn hàng
                ViewBag.TotalOrders = db.HOADON_BAN.Count(h => h.MAKH == customerId);
                ViewBag.TotalSpent = db.HOADON_BAN
                    .Where(h => h.MAKH == customerId && h.TRANGTHAI == "Hoàn thành")
                    .Sum(h => (decimal?)h.TONGTIEN) ?? 0;

                return View(customer);
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
        public ActionResult UpdateProfile(KHACHHANG model)
        {
            try
            {
                string customerId = Session["CustomerID"]?.ToString();
                var customer = db.KHACHHANG.Find(customerId);

                if (customer != null)
                {
                    customer.HOTENKH = model.HOTENKH;
                    customer.GIOITINH = model.GIOITINH;
                    customer.SDT = model.SDT;
                    customer.EMAIL = model.EMAIL;
                    customer.DIACHI = model.DIACHI;

                    db.SaveChanges();

                    // Cập nhật session
                    Session["CustomerName"] = customer.HOTENKH;

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

        #region DANH MỤC

        // GET: Customer/Category/{id} - Sản phẩm theo danh mục
        public ActionResult Category(string id)
        {
            return RedirectToAction("Products", new { category = id });
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