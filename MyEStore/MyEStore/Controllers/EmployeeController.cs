using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using MyEStore.Entities;
using MyEStore.Models;
using System.Security.Claims;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Net.Http;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace MyEStore.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly MyeStoreContext _ctx;
        private readonly HttpClient _httpClient = new HttpClient();

        public EmployeeController(MyeStoreContext ctx)
        {
            _ctx = ctx;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM model, string? ReturnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ViewBag.ReturnUrl = ReturnUrl;

            var nv = await _ctx.NhanViens.SingleOrDefaultAsync(p => p.MaNv == model.UserName);
            if (nv == null)
            {
                ViewBag.ThongBao = "Tên tài khoản không tồn tại.";
                return View(model);
            }

            if (!BCrypt.Net.BCrypt.Verify(model.Password + nv.RandomKey, nv.MatKhau))
            {
                ViewBag.ThongBao = "Mật khẩu không đúng.";
                return View(model);
            }

            nv.DangNhapLanCuoi = DateTime.Now;
            await _ctx.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, nv.HoTen),
                new Claim(ClaimTypes.Email, nv.Email),
                new Claim("UserId", nv.MaNv),
                new Claim(ClaimTypes.Role, "Administrator")
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(claimPrincipal);

            if (!string.IsNullOrEmpty(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (_ctx.NhanViens.Any(nv => nv.MaNv == model.UserName || nv.Email == model.Email))
            {
                ViewBag.ThongBao = "Tên tài khoản hoặc email đã tồn tại.";
                return View(model);
            }

            if (!await IsRealEmail(model.Email))
            {
                ViewBag.ThongBao = "Email không hợp lệ hoặc không tồn tại. Vui lòng sử dụng địa chỉ email thực.";
                return View(model);
            }

            var randomKey = GenerateRandomKey();
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password + randomKey);

            var newEmployee = new NhanVien
            {
                MaNv = model.UserName,
                MatKhau = hashedPassword,
                HoTen = model.FullName,
                Email = model.Email,
                DienThoai = model.PhoneNumber,
                DiaChi = model.Address,
                RandomKey = randomKey,
                NgayTaoTaiKhoan = DateTime.Now,
                GioiTinh = model.GioiTinh
            };

            _ctx.NhanViens.Add(newEmployee);
            await _ctx.SaveChangesAsync();

            var message = $@"
              <html>
              <head>
                <meta http-equiv='Content-Type' content='text/html; charset=UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Chào mừng đến với SiderGin</title>
              </head>
              <body style='font-family: Arial, sans-serif; padding: 25px; background-color: #f5f7fa; color: #333;'>
                  <div style='max-width: 600px; margin: auto; background-color: #ffffff; padding: 30px; border-radius: 10px; box-shadow: 0 3px 15px rgba(0, 0, 0, 0.1);'>
                      <div style='text-align: center; margin-bottom: 25px; border-bottom: 2px solid #f0f0f0; padding-bottom: 20px;'>
                          <h1 style='color: #0066cc; font-size: 24px; margin: 0;'>SiderGin Support</h1>
                          <p style='color: #666; margin: 5px 0 0;'>Chào mừng bạn đến với SiderGin!</p>
                      </div>
                      <h2 style='color: #0066cc; margin-top: 0;'>Xin chào {newEmployee.HoTen},</h2>
                      <p style='line-height: 1.6; margin-bottom: 20px;'>Cảm ơn bạn đã đăng ký tài khoản nhân viên tại <strong>SiderGin</strong>. Tài khoản của bạn đã được tạo thành công và sẵn sàng sử dụng.</p>
                      <p style='line-height: 1.6;'>Vui lòng đăng nhập bằng thông tin sau:</p>
                      <div style='background-color: #f8f9fa; border-left: 4px solid #0066cc; padding: 15px 20px; margin: 25px 0; border-radius: 4px;'>
                          <p style='margin: 0 0 5px; font-size: 14px; color: #666;'>Tên tài khoản: <strong>{newEmployee.MaNv}</strong></p>
                          <p style='margin: 0 0 5px; font-size: 14px; color: #666;'>Email: <strong>{newEmployee.Email}</strong></p>
                      </div>
                      <p style='line-height: 1.6;'>Nếu bạn cần hỗ trợ, vui lòng liên hệ với chúng tôi qua:</p>
                      <div style='display: flex; margin: 15px 0 25px;'>
                          <div style='margin-right: 20px;'>
                              <p style='margin: 0; color: #666;'>
                                  <span style='font-size: 16px;'>📞</span> Hotline
                              </p>
                              <p style='margin: 5px 0 0; font-weight: bold;'>0123 456 789</p>
                          </div>
                          <div>
                              <p style='margin: 0; color: #666;'>
                                  <span style='font-size: 16px;'>✉️</span> Email hỗ trợ
                              </p>
                              <p style='margin: 5px 0 0; font-weight: bold;'>support@sidergin.com</p>
                          </div>
                      </div>
                      <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eaeaea;'>
                          <p style='margin: 0;'>Trân trọng,<br><strong>Đội ngũ hỗ trợ SiderGin</strong></p>
                      </div>
                  </div>
              </body>
              </html>";

            await SendEmail(newEmployee.Email, "Chào mừng bạn đến với SiderGin", message);

            TempData["ThongBao"] = "Đăng ký thành công! Vui lòng kiểm tra email để xác nhận.";
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Redirect("/");
        }

        [HttpGet("/Forbidden")]
        public IActionResult Forbidden()
        {
            return View();
        }

        private async Task<bool> IsRealEmail(string email)
        {
            string apiKey = "03424848e67c47c19fe0a512b4b8d768";
            string requestUrl = $"https://emailvalidation.abstractapi.com/v1/?api_key={apiKey}&email={email}";

            try
            {
                var response = await _httpClient.GetAsync(requestUrl);
                if (!response.IsSuccessStatusCode)
                    return false;

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<EmailVerificationResult>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result.Deliverability == "DELIVERABLE";
            }
            catch
            {
                return false;
            }
        }

        private string GenerateRandomKey(int length = 16)
        {
            var randomBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }

        private async Task SendEmail(string toEmail, string subject, string message)
        {
            var client = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("truongminhduc4002@gmail.com", "hocekpuhklqvkniu"),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("truongminhduc4002@gmail.com", "Sidergin Support"),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
                Priority = MailPriority.Normal,
                HeadersEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8,
                BodyEncoding = System.Text.Encoding.UTF8
            };

            mailMessage.To.Add(toEmail);
            mailMessage.ReplyToList.Add(new MailAddress("truongminhduc4002@gmail.com", "Sidergin Support"));

            mailMessage.Headers.Add("X-Priority", "3");
            mailMessage.Headers.Add("X-MSMail-Priority", "Normal");
            mailMessage.Headers.Add("Importance", "Normal");
            mailMessage.Headers.Add("X-Mailer", "Sidergin App");

            try
            {
                await client.SendMailAsync(mailMessage);
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"SMTP Exception: {ex.Message}");
                throw;
            }
        }
    }
}