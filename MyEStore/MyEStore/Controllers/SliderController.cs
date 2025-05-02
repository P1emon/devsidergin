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
using System.Net.Mail;
using System.Net;
using DevSidergin.Entities;
using System.Threading;

namespace DevSidergin.Controllers
{
    [Authorize]
    public class SliderController : Controller
    {
        private readonly MyeStoreContext _ctx;
        private static Timer _weeklyEmailTimer;

        public SliderController(MyeStoreContext ctx)
        {
            _ctx = ctx;
            // Initialize the weekly email timer if it hasn't been initialized
            if (_weeklyEmailTimer == null)
            {
                InitializeWeeklyEmailTimer();
            }
        }

        private void InitializeWeeklyEmailTimer()
        {
            // Calculate the time until next Friday 7 PM
            var now = DateTime.Now;
            var nextFriday = now.AddDays(((int)DayOfWeek.Friday - (int)now.DayOfWeek + 7) % 7);
            var nextFriday7PM = new DateTime(nextFriday.Year, nextFriday.Month, nextFriday.Day, 19, 0, 0);
            
            // If it's already past 7 PM on Friday, schedule for next week
            if (now > nextFriday7PM)
            {
                nextFriday7PM = nextFriday7PM.AddDays(7);
            }

            var timeUntilNextFriday7PM = nextFriday7PM - now;

            // Create a timer that runs every week
            _weeklyEmailTimer = new Timer(async _ =>
            {
                await SendWeeklyEventSummary();
            }, null, timeUntilNextFriday7PM, TimeSpan.FromDays(7));
        }

        private async Task SendWeeklyEventSummary()
        {
            try
            {
                // Get all upcoming events (not yet started)
                var upcomingEvents = await _ctx.Sliders
                    .Where(s => s.NgayBatDau > DateTime.Now && s.HieuLuc)
                    .OrderBy(s => s.NgayBatDau)
                    .ToListAsync();

                if (!upcomingEvents.Any())
                {
                    return; // No upcoming events to send
                }

                // Get all customers with email notifications enabled
                var customers = await _ctx.KhachHangs
                    .Where(k => k.AcEmailNoti && k.HieuLuc)
                    .ToListAsync();

                foreach (var customer in customers)
                {
                    try
                    {
                        using (var message = new MailMessage())
                        {
                            message.From = new MailAddress("phannguyendangkhoa0915@gmail.com", "MyEStore");
                            message.To.Add(customer.Email);
                            message.Subject = "Tổng hợp sự kiện sắp tới từ MyEStore";

                            // Create beautiful HTML email template for weekly summary
                            message.Body = $@"
                                <!DOCTYPE html>
                                <html>
                                <head>
                                    <meta charset='UTF-8'>
                                    <style>
                                        body {{
                                            font-family: Arial, sans-serif;
                                            line-height: 1.6;
                                            color: #333;
                                            max-width: 600px;
                                            margin: 0 auto;
                                            padding: 20px;
                                        }}
                                        .header {{
                                            background: linear-gradient(135deg, #4a90e2, #357abd);
                                            color: white;
                                            padding: 20px;
                                            text-align: center;
                                            border-radius: 8px 8px 0 0;
                                        }}
                                        .content {{
                                            background: #ffffff;
                                            padding: 20px;
                                            border: 1px solid #e0e0e0;
                                            border-radius: 0 0 8px 8px;
                                        }}
                                        .title {{
                                            color: #2c3e50;
                                            font-size: 24px;
                                            margin-bottom: 20px;
                                        }}
                                        .event-list {{
                                            margin: 20px 0;
                                        }}
                                        .event-item {{
                                            background: #f8f9fa;
                                            padding: 15px;
                                            margin-bottom: 10px;
                                            border-radius: 4px;
                                            border-left: 4px solid #4a90e2;
                                        }}
                                        .event-title {{
                                            color: #2c3e50;
                                            font-size: 18px;
                                            margin-bottom: 5px;
                                        }}
                                        .event-date {{
                                            color: #e74c3c;
                                            font-weight: bold;
                                            margin: 5px 0;
                                        }}
                                        .event-description {{
                                            color: #34495e;
                                            margin: 5px 0;
                                        }}
                                        .button {{
                                            display: inline-block;
                                            padding: 10px 20px;
                                            background: #4a90e2;
                                            color: white;
                                            text-decoration: none;
                                            border-radius: 4px;
                                            margin-top: 10px;
                                        }}
                                        .footer {{
                                            text-align: center;
                                            margin-top: 20px;
                                            padding-top: 20px;
                                            border-top: 1px solid #e0e0e0;
                                            color: #7f8c8d;
                                            font-size: 14px;
                                        }}
                                    </style>
                                </head>
                                <body>
                                    <div class='header'>
                                        <h1>Tổng hợp sự kiện sắp tới</h1>
                                    </div>
                                    <div class='content'>
                                        <h2 class='title'>Các sự kiện sắp diễn ra</h2>
                                        <div class='event-list'>
                                            {string.Join("", upcomingEvents.Select(e => $@"
                                                <div class='event-item'>
                                                    <h3 class='event-title'>{e.TieuDe}</h3>
                                                    <p class='event-date'>Ngày diễn ra: {e.NgayBatDau:dd/MM/yyyy HH:mm}</p>
                                                    <p class='event-description'>{e.MoTa}</p>
                                                    <a href='{e.LinkQuangCao}' class='button'>Xem chi tiết</a>
                                                </div>
                                            "))}
                                        </div>
                                        <div class='footer'>
                                            <p>Trân trọng,<br>Đội ngũ MyEStore</p>
                                            <p>Đây là email tự động, vui lòng không trả lời.</p>
                                        </div>
                                    </div>
                                </body>
                                </html>
                            ";
                            message.IsBodyHtml = true;

                            using (var client = new SmtpClient("smtp.gmail.com", 587))
                            {
                                client.EnableSsl = true;
                                client.Credentials = new NetworkCredential("phannguyendangkhoa0915@gmail.com", "iagqpgyvbegvfdoh");
                                await client.SendMailAsync(message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending weekly summary to {customer.Email}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in weekly email summary: {ex.Message}");
            }
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

                // Get all customers with email notifications enabled
                var customers = await _ctx.KhachHangs
                    .Where(k => k.AcEmailNoti && k.HieuLuc)
                    .ToListAsync();

                // Create notifications for each customer
                foreach (var customer in customers)
                {
                    var thongBao = new ThongBao
                    {
                        TieuDe = model.TieuDe,
                        NoiDung = model.MoTa,
                        NgayTao = DateTime.Now,
                        MaSlider = model.MaSlider.ToString(),
                        MaMv = GetLoggedInNhanVienId(),
                        MaKh = customer.MaKh // Set the customer ID
                    };

                    _ctx.ThongBaos.Add(thongBao);
                }

                await _ctx.SaveChangesAsync();

                // Check if we should send emails immediately or schedule them
                var daysUntilEvent = (model.NgayBatDau - DateTime.Now).TotalDays;

                if (daysUntilEvent <= 7)
                {
                    // Send emails immediately for events within 7 days
                    await SendEmailNotifications(model);
                    TempData["ThongBao"] = "Thêm slider thành công và đã gửi thông báo!";
                }
                else
                {
                    // Schedule email sending for events more than 7 days away
                    ScheduleEmailNotification(model, daysUntilEvent);
                    TempData["ThongBao"] = "Thêm slider thành công! Thông báo sẽ được gửi tự động trước 7 ngày sự kiện.";
                }

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

        private void ScheduleEmailNotification(Slider slider, double daysUntilEvent)
        {
            // Calculate when to send the email (7 days before the event)
            var sendDate = slider.NgayBatDau.AddDays(-7);
            
            // Create a background task to send the email at the scheduled time
            Task.Run(async () =>
            {
                try
                {
                    // Wait until it's time to send the email
                    var delay = sendDate - DateTime.Now;
                    if (delay.TotalMilliseconds > 0)
                    {
                        await Task.Delay(delay);
                    }

                    // Send the email
                    await SendEmailNotifications(slider);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in scheduled email sending: {ex.Message}");
                }
            });
        }

        private async Task SendEmailNotifications(Slider slider)
        {
            var customers = await _ctx.KhachHangs
                .Where(k => k.AcEmailNoti && k.HieuLuc)
                .ToListAsync();

            foreach (var customer in customers)
            {
                try
                {
                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress("phannguyendangkhoa0915@gmail.com", "MyEStore");
                        message.To.Add(customer.Email);
                        message.Subject = $"Thông báo mới: {slider.TieuDe}";
                        
                        // Create beautiful HTML email template
                        message.Body = $@"
                            <!DOCTYPE html>
                            <html>
                            <head>
                                <meta charset='UTF-8'>
                                <style>
                                    body {{
                                        font-family: Arial, sans-serif;
                                        line-height: 1.6;
                                        color: #333;
                                        max-width: 600px;
                                        margin: 0 auto;
                                        padding: 20px;
                                    }}
                                    .header {{
                                        background: linear-gradient(135deg, #4a90e2, #357abd);
                                        color: white;
                                        padding: 20px;
                                        text-align: center;
                                        border-radius: 8px 8px 0 0;
                                    }}
                                    .content {{
                                        background: #ffffff;
                                        padding: 20px;
                                        border: 1px solid #e0e0e0;
                                        border-radius: 0 0 8px 8px;
                                    }}
                                    .title {{
                                        color: #2c3e50;
                                        font-size: 24px;
                                        margin-bottom: 20px;
                                    }}
                                    .description {{
                                        color: #34495e;
                                        font-size: 16px;
                                        margin-bottom: 20px;
                                    }}
                                    .date {{
                                        color: #7f8c8d;
                                        font-size: 14px;
                                        margin-bottom: 20px;
                                    }}
                                    .event-date {{
                                        color: #e74c3c;
                                        font-weight: bold;
                                        margin: 20px 0;
                                    }}
                                    .footer {{
                                        text-align: center;
                                        margin-top: 20px;
                                        padding-top: 20px;
                                        border-top: 1px solid #e0e0e0;
                                        color: #7f8c8d;
                                        font-size: 14px;
                                    }}
                                    .button {{
                                        display: inline-block;
                                        padding: 10px 20px;
                                        background: #4a90e2;
                                        color: white;
                                        text-decoration: none;
                                        border-radius: 4px;
                                        margin-top: 20px;
                                    }}
                                </style>
                            </head>
                            <body>
                                <div class='header'>
                                    <h1>Thông báo mới từ MyEStore</h1>
                                </div>
                                <div class='content'>
                                    <h2 class='title'>{slider.TieuDe}</h2>
                                    <p class='description'>{slider.MoTa}</p>
                                    <p class='event-date'>Sự kiện diễn ra vào: {slider.NgayBatDau:dd/MM/yyyy HH:mm}</p>
                                    <div style='text-align: center;'>
                                        <a href='{slider.LinkQuangCao}' class='button'>Xem chi tiết</a>
                                    </div>
                                    <div class='footer'>
                                        <p>Trân trọng,<br>Đội ngũ MyEStore</p>
                                        <p>Đây là email tự động, vui lòng không trả lời.</p>
                                    </div>
                                </div>
                            </body>
                            </html>
                        ";
                        message.IsBodyHtml = true;

                        using (var client = new SmtpClient("smtp.gmail.com", 587))
                        {
                            client.EnableSsl = true;
                            client.Credentials = new NetworkCredential("phannguyendangkhoa0915@gmail.com", "iagqpgyvbegvfdoh");
                            await client.SendMailAsync(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue with other customers
                    Console.WriteLine($"Error sending email to {customer.Email}: {ex.Message}");
                }
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

