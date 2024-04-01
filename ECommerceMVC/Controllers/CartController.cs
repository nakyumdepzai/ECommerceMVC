using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using ECommerceMVC.Helpers;
using Microsoft.AspNetCore.Authorization;
using ECommerceMVC.Services;
using ECommerceMVC.Models;
using ChiTietHd = ECommerceMVC.Models.ChiTietHd;
using KhachHang = ECommerceMVC.Models.KhachHang;
using HoaDon = ECommerceMVC.Models.HoaDon;
using Hshop2023Context = ECommerceMVC.Models.Hshop2023Context;
using System.Text.RegularExpressions;

namespace ECommerceMVC.Controllers
{
	public class CartController : Controller
	{
		private readonly Hshop2023Context db;
		private readonly IVnPayService _vnPayService;
		private readonly PaypalClient _paypalClient;

		public CartController(Hshop2023Context context, IVnPayService vnPayService, PaypalClient paypalClient)
		{
			db = context;
			_vnPayService = vnPayService;
			_paypalClient = paypalClient;
		}

		public List<CartItem> Cart => HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();

		public IActionResult Index()
		{
			return View(Cart);
		}

		public IActionResult AddToCart(int id, int quantity = 1)
		{
			var gioHang = Cart;
			var item = gioHang.SingleOrDefault(p => p.MaHh == id);
			if (item == null)
			{
				var hangHoa = db.HangHoas.SingleOrDefault(p => p.MaHh == id);
				if (hangHoa == null)
				{
					TempData["Message"] = $"Không tìm thấy hàng hóa có mã {id}";
					return Redirect("/404");
				}
				item = new CartItem
				{
					MaHh = hangHoa.MaHh,
					TenHH = hangHoa.TenHh,
					DonGia = hangHoa.DonGia ?? 0,
					Hinh = hangHoa.Hinh ?? string.Empty,
					SoLuong = quantity
				};
				gioHang.Add(item);
			}
			else
			{
				item.SoLuong += quantity;
			}

			HttpContext.Session.Set(MySetting.CART_KEY, gioHang);

			return RedirectToAction("Index");
		}

		public IActionResult RemoveCart(int id)
		{
			var gioHang = Cart;
			var item = gioHang.SingleOrDefault(p => p.MaHh == id);
			if (item != null)
			{
				gioHang.Remove(item);
				HttpContext.Session.Set(MySetting.CART_KEY, gioHang);
			}
			return RedirectToAction("Index");
		}

		[Authorize]
		[HttpGet]
		public IActionResult CheckOut()
		{
			if (Cart.Count == 0)
			{
				return Redirect("/");
			}
			ViewBag.PaypalClientId = _paypalClient.ClientId;
			return View(Cart);
		}

		[Authorize]
		[HttpPost]
		public IActionResult CheckOut(CheckOutVM model, string payment = "COD")
		{
			if (ModelState.IsValid)
			{

				if (payment == "VNPay")
				{
					var vnPayModel = new VnPaymentRequestModel
					{
						Amount = Cart.Sum(p => p.ThanhTien * 1000),
						CreatedDate = DateTime.Now,
						Description = $"{model.HoTen} {model.DienThoai}",
						FullName = model.HoTen,
						OrderId = new Random().Next(1000, 10000)
					};
					return Redirect(_vnPayService.CreatePaymentUrl(HttpContext, vnPayModel));
				}

				var customerId = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID).Value;
				HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID);
				var khachhang = new KhachHang();
				if (model.GiongKhachHang)
				{
					khachhang = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == customerId);
				}
				var hoadon = new HoaDon
				{
					MaKh = customerId,
					HoTen = model.HoTen ?? khachhang.HoTen,
					DiaChi = model.DiaChi ?? khachhang.DiaChi,
					DienThoai = model.DienThoai ?? khachhang.DienThoai,
					NgayDat = DateTime.Now,
					CachThanhToan = "COD",
					CachVanChuyen = "grab",
					MaTrangThai = 0,
					GhiChu = model.GhiChu
				};
				db.Database.BeginTransaction();

				try
				{
					db.Database.CommitTransaction();
					db.Add(hoadon);
					db.SaveChanges();

					var cthd = new List<ChiTietHd>();
					foreach (var item in Cart)
					{
						cthd.Add(new ChiTietHd
						{
							MaHd = hoadon.MaHd,
							SoLuong = item.SoLuong,
							DonGia = item.DonGia,
							MaHh = item.MaHh,
							GiamGia = 0
						});
					}
					db.AddRange(cthd);
					db.SaveChanges();
					HttpContext.Session.Set<List<CartItem>>(MySetting.CART_KEY, new List<CartItem>());
					return View("Success");
				}
				catch
				{
					db.Database.RollbackTransaction();
				}

			}
			return View(Cart);
		}

		[Authorize]
		public IActionResult PaymentSuccess()
		{
			return View("Success", Cart);
		}

		[Authorize]
		public IActionResult PaymentFail()
		{
			return View();
		}


		[Authorize]
		public IActionResult PaymentCallBack()
		{
			var response = _vnPayService.PaymentExecute(Request.Query);

			if (response == null || response.VnPayResponseCode != "00")
			{
				TempData["Message"] = $"Loi thanh toan: {response.VnPayResponseCode}";
				return RedirectToAction("PaymentFail");
			}


			TempData["Message"] = $"Thanh toan thanh cong: {response.VnPayResponseCode}";
			return View("PaymentSuccess");
		}

		#region Paypal payment
		[Authorize]
		[HttpPost("/Cart/create-paypal-order")]
		public async Task<IActionResult> CreatePaypalOrder(CancellationToken cancellationToken)
		{
			var tongTien = Cart.Sum(p => p.ThanhTien).ToString();
			var donViTienTe = "USD";
			var maDonHangThamChieu = "DH" + DateTime.Now.Ticks.ToString();
			try
			{
				var response = await _paypalClient.CreateOrder(tongTien, donViTienTe, maDonHangThamChieu);
				return Ok(response);
			}
			catch (Exception ex)
			{
				var error = new { ex.GetBaseException().Message };
				return BadRequest(error);
			}
		}
		[Authorize]
		[HttpPost("/Cart/capture-paypal-order")]

		public async Task<IActionResult> CapturePaypalOrder(string orderID, CancellationToken cancellationToken)
		{
			try
			{
				var response = await _paypalClient.CaptureOrder(orderID);
				return Ok(response);
			}
			catch(Exception ex)
			{
                var error = new { ex.GetBaseException().Message };
                return BadRequest(error);
            }
		}
        #endregion
    }
}
