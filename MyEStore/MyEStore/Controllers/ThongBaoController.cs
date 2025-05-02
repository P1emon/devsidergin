using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyEStore.Entities;
using MyEStore.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;
using DevSidergin.Entities;

namespace DevSidergin.Controllers
{
    [Authorize]
    public class ThongBaoController : Controller
    {
        private readonly MyeStoreContext _ctx;

        public ThongBaoController(MyeStoreContext ctx)
        {
            _ctx = ctx;
        }

        // GET: /ThongBao
        public async Task<IActionResult> Index()
        {
            var thongBaos = await _ctx.ThongBaos
                .OrderByDescending(t => t.NgayTao)
                .ToListAsync();
            return View(thongBaos);
        }

        // GET: /ThongBao/Create
        public async Task<IActionResult> Create()
        {
            var sliders = await _ctx.Sliders
                .Where(s => s.HieuLuc)
                .ToListAsync();
            ViewData["Sliders"] = sliders;
            return View();
        }

        // POST: /ThongBao/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ThongBao thongBao)
        {
            if (ModelState.IsValid)
            {
                // Get current user's ID from claims
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                thongBao.MaMv = userId;
                thongBao.NgayTao = DateTime.Now;

                _ctx.ThongBaos.Add(thongBao);
                await _ctx.SaveChangesAsync();

                // Send emails to customers who have email notifications enabled
                await SendEmailNotifications(thongBao);

                TempData["ThongBao"] = "Tạo thông báo thành công!";
                return RedirectToAction(nameof(Index));
            }

            var sliders = await _ctx.Sliders
                .Where(s => s.HieuLuc)
                .ToListAsync();
            ViewData["Sliders"] = sliders;
            return View(thongBao);
        }

        private async Task SendEmailNotifications(ThongBao thongBao)
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
                        message.From = new MailAddress("your-email@example.com"); // Replace with your email
                        message.To.Add(customer.Email);
                        message.Subject = thongBao.TieuDe;
                        message.Body = $@"
                            <h2>{thongBao.TieuDe}</h2>
                            <p>{thongBao.NoiDung}</p>
                            <p>Ngày tạo: {thongBao.NgayTao:dd/MM/yyyy HH:mm}</p>
                            <p>Trân trọng,<br>MyEStore Team</p>
                        ";
                        message.IsBodyHtml = true;

                        using (var client = new SmtpClient("smtp.gmail.com", 587))
                        {
                            client.EnableSsl = true;
                            client.Credentials = new NetworkCredential("your-email@example.com", "your-password"); // Replace with your credentials
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

        // GET: /ThongBao/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var thongBao = await _ctx.ThongBaos.FindAsync(id);
            if (thongBao == null)
            {
                return NotFound();
            }
            return View(thongBao);
        }

        // POST: /ThongBao/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ThongBao thongBao)
        {
            if (id != thongBao.MaTb)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _ctx.Update(thongBao);
                    await _ctx.SaveChangesAsync();
                    TempData["ThongBao"] = "Cập nhật thông báo thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ThongBaoExists(thongBao.MaTb))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(thongBao);
        }

        // GET: /ThongBao/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var thongBao = await _ctx.ThongBaos.FindAsync(id);
            if (thongBao == null)
            {
                return NotFound();
            }
            return View(thongBao);
        }

        // POST: /ThongBao/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var thongBao = await _ctx.ThongBaos.FindAsync(id);
            if (thongBao != null)
            {
                _ctx.ThongBaos.Remove(thongBao);
                await _ctx.SaveChangesAsync();
                TempData["ThongBao"] = "Xóa thông báo thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ThongBaoExists(int id)
        {
            return _ctx.ThongBaos.Any(e => e.MaTb == id);
        }
    }
}
