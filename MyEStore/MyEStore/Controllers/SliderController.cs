using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyEStore.Entities;
using MyEStore.Models;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DevSidergin.Controllers
{
    [Authorize]
    public class SliderController : Controller
    {
        private readonly MyeStoreContext _ctx;

        public SliderController(MyeStoreContext ctx)
        {
            _ctx = ctx;
        }

        // GET: /Slider
        public async Task<IActionResult> Index()
        {
            var sliders = await _ctx.Sliders
                .Include(s => s.NhanVien)
                .Select(s => new Slider
                {
                    MaSlider = s.MaSlider,
                    TieuDe = s.TieuDe,
                    MoTa = s.MoTa,
                    HinhAnh = s.HinhAnh,
                    LinkQuangCao = s.LinkQuangCao,
                    NgayTao = s.NgayTao,
                    NgayBatDau = s.NgayBatDau,
                    NgayKetThuc = s.NgayKetThuc,
                    HieuLuc = s.HieuLuc,
                    MaNV = s.MaNV,
                    NhanVien = new NhanVien { HoTen = s.NhanVien.HoTen }
                })
                .ToListAsync();
            return View(sliders);
        }

        // GET: /Slider/Create
        public IActionResult Create()
        {
            return View(new Slider
            {
                NgayBatDau = DateTime.Now,
                NgayKetThuc = DateTime.Now.AddDays(7),
                HieuLuc = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Slider model, IFormFile hinhAnh)
        {
            // Loại bỏ validation tự động cho các trường không cần thiết
            ModelState.Remove("NhanVien");
            ModelState.Remove("HinhAnh");
            ModelState.Remove("MaNV");

            // Handle image upload
            if (hinhAnh == null || hinhAnh.Length == 0)
            {
                ModelState.AddModelError("HinhAnh", "Vui lòng tải lên một ảnh.");
            }
            else
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(hinhAnh.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("HinhAnh", "Chỉ hỗ trợ các định dạng ảnh: .jpg, .jpeg, .png, .gif.");
                }
                else if (hinhAnh.Length > 5 * 1024 * 1024) // 5MB limit
                {
                    ModelState.AddModelError("HinhAnh", "Kích thước ảnh không được vượt quá 5MB.");
                }
                else
                {
                    var fileName = Guid.NewGuid().ToString() + extension;
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/sliders", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await hinhAnh.CopyToAsync(stream);
                    }
                    model.HinhAnh = $"/images/sliders/{fileName}";
                }
            }

            // Validate dates
            if (model.NgayKetThuc <= model.NgayBatDau)
            {
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc phải sau ngày bắt đầu.");
            }

            // Set MaNV from Claims
            string maNV = GetLoggedInNhanVienId();
            Console.WriteLine($"MaNV from Claims: {maNV}");
            if (string.IsNullOrEmpty(maNV))
            {
                ModelState.AddModelError("MaNV", "Không thể xác định nhân viên hiện tại.");
            }
            else
            {
                // Kiểm tra nhân viên có tồn tại trong database không
                bool nhanVienExists = await _ctx.NhanViens.AnyAsync(nv => nv.MaNv == maNV);
                Console.WriteLine($"NhanVien exists: {nhanVienExists}");
                if (!nhanVienExists)
                {
                    ModelState.AddModelError("MaNV", "Nhân viên không tồn tại trong hệ thống.");
                }
                else
                {
                    model.MaNV = maNV;
                }
            }

            // Log ModelState errors
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                Console.WriteLine("ModelState errors: " + string.Join(", ", errors));
                return View(model);
            }

            model.NgayTao = DateTime.Now;

            try
            {
                _ctx.Sliders.Add(model);
                await _ctx.SaveChangesAsync();
                TempData["ThongBao"] = "Thêm slider thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                ModelState.AddModelError("", $"Lỗi khi lưu slider: {ex.InnerException?.Message ?? ex.Message}");
                return View(model);
            }
        }

        // GET: /Slider/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var slider = await _ctx.Sliders.FindAsync(id);
            if (slider == null)
            {
                return NotFound();
            }
            return View(slider);
        }

        // POST: /Slider/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaSlider,TieuDe,MoTa,LinkQuangCao,NgayBatDau,NgayKetThuc,HieuLuc")] Slider model, IFormFile hinhAnh)
        {
            if (id != model.MaSlider)
            {
                return NotFound();
            }

            // Remove navigation property and fields from validation
            ModelState.Remove("NhanVien");
            ModelState.Remove("HinhAnh");
            ModelState.Remove("MaNV");

            var slider = await _ctx.Sliders.FindAsync(id);
            if (slider == null)
            {
                return NotFound();
            }

            // Handle image upload
            if (hinhAnh != null && hinhAnh.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(hinhAnh.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("HinhAnh", "Chỉ hỗ trợ các định dạng ảnh: .jpg, .jpeg, .png, .gif.");
                }
                else if (hinhAnh.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("HinhAnh", "Kích thước ảnh không được vượt quá 5MB.");
                }
                else
                {
                    // Delete old image
                    if (!string.IsNullOrEmpty(slider.HinhAnh))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", slider.HinhAnh.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    var fileName = Guid.NewGuid().ToString() + extension;
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/sliders", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await hinhAnh.CopyToAsync(stream);
                    }
                    slider.HinhAnh = $"/images/sliders/{fileName}";
                }
            }
            // Note: Removed the else if (string.IsNullOrEmpty(slider.HinhAnh)) check
            // In Edit, the existing image can be retained if no new image is uploaded

            // Validate dates
            if (model.NgayKetThuc <= model.NgayBatDau)
            {
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc phải sau ngày bắt đầu.");
            }

            // Set MaNV from Claims
            string maNV = GetLoggedInNhanVienId();
            Console.WriteLine($"MaNV from Claims: {maNV}");
            if (string.IsNullOrEmpty(maNV))
            {
                ModelState.AddModelError("MaNV", "Không thể xác định nhân viên hiện tại.");
            }
            else
            {
                // Kiểm tra nhân viên có tồn tại trong database không
                bool nhanVienExists = await _ctx.NhanViens.AnyAsync(nv => nv.MaNv == maNV);
                Console.WriteLine($"NhanVien exists: {nhanVienExists}");
                if (!nhanVienExists)
                {
                    ModelState.AddModelError("MaNV", "Nhân viên không tồn tại trong hệ thống.");
                }
            }

            // Log ModelState errors
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                Console.WriteLine("ModelState errors: " + string.Join(", ", errors));
                return View(model);
            }

            // Update fields
            slider.TieuDe = model.TieuDe;
            slider.MoTa = model.MoTa;
            slider.LinkQuangCao = model.LinkQuangCao;
            slider.NgayBatDau = model.NgayBatDau;
            slider.NgayKetThuc = model.NgayKetThuc;
            slider.HieuLuc = model.HieuLuc;
            slider.MaNV = maNV;

            try
            {
                await _ctx.SaveChangesAsync();
                TempData["ThongBao"] = "Cập nhật slider thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                ModelState.AddModelError("", $"Lỗi khi cập nhật slider: {ex.InnerException?.Message ?? ex.Message}");
                return View(model);
            }
        }

        // GET: /Slider/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var slider = await _ctx.Sliders
                .Include(s => s.NhanVien)
                .FirstOrDefaultAsync(s => s.MaSlider == id);
            if (slider == null)
            {
                return NotFound();
            }
            return View(slider);
        }

        // POST: /Slider/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var slider = await _ctx.Sliders.FindAsync(id);
            if (slider == null)
            {
                return NotFound();
            }

            // Delete image
            if (!string.IsNullOrEmpty(slider.HinhAnh))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", slider.HinhAnh.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _ctx.Sliders.Remove(slider);
            await _ctx.SaveChangesAsync();
            TempData["ThongBao"] = "Xóa slider thành công!";
            return RedirectToAction(nameof(Index));
        }

        // Phương thức tiện ích lấy MaNV từ Claims
        private string GetLoggedInNhanVienId()
        {
            return User.FindFirst("UserId")?.Value;
        }

        // Phương thức lấy thông tin đầy đủ của nhân viên
        private async Task<NhanVien> GetLoggedInNhanVienAsync()
        {
            var maNV = GetLoggedInNhanVienId();
            if (string.IsNullOrEmpty(maNV))
            {
                return null;
            }

            return await _ctx.NhanViens.FirstOrDefaultAsync(nv => nv.MaNv == maNV);
        }
    }
}

