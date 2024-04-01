using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ECommerceMVC.Models;

public partial class KhachHang
{
    [Display(Name = "Username")]
    [Required(ErrorMessage = "*")]
    [MaxLength(20, ErrorMessage = "Maximum 20 characters")]
    public string MaKh { get; set; } = null!;

    [Display(Name = "Password")]
    [Required(ErrorMessage = "*")]
    public string? MatKhau { get; set; }

    [Display(Name = "Fullname")]
    [Required(ErrorMessage = "*")]
    [MaxLength(50, ErrorMessage = "Maximum 50 characters")]
    public string HoTen { get; set; } = null!;

    public bool GioiTinh { get; set; }

    public DateTime NgaySinh { get; set; }

    public string? DiaChi { get; set; }

    [MaxLength(24, ErrorMessage = "Maximum 24 characters")]
    [RegularExpression(@"0\d{9}", ErrorMessage = "Incorrect format")]
    public string? DienThoai { get; set; }

    public string Email { get; set; } = null!;

    public string? Hinh { get; set; }

    public bool HieuLuc { get; set; }

    public int VaiTro { get; set; }

    public string? RandomKey { get; set; }

    public virtual ICollection<BanBe> BanBes { get; set; } = new List<BanBe>();

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual ICollection<YeuThich> YeuThiches { get; set; } = new List<YeuThich>();
}
