using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyEStore.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace MyEStore.Controllers
{
    [Authorize]
    public class HoaDonController : Controller
    {
        private readonly MyeStoreContext _context;

        public MyeStoreContext Context => _context;

        public HoaDonController(MyeStoreContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search, string trangThai)
        {
            var hoaDons = Context.HoaDons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.MaTrangThaiNavigation)
                .AsQueryable();

            // Lọc theo tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                hoaDons = hoaDons.Where(h => h.MaHd.ToString().Contains(search) || h.MaKhNavigation.HoTen.Contains(search));
                ViewData["SearchQuery"] = search;
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(trangThai) && int.TryParse(trangThai, out int trangThaiId))
            {
                hoaDons = hoaDons.Where(h => h.MaTrangThai == trangThaiId);
                ViewData["SelectedTrangThai"] = trangThai;
            }

            // Lấy danh sách trạng thái cho bộ lọc
            ViewData["TrangThai"] = await Context.TrangThais.ToListAsync();

            return View(await hoaDons.ToListAsync());
        }

        // GET: HoaDon/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hoaDon = await Context.HoaDons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.MaTrangThaiNavigation)
                .Include(h => h.ChiTietHds)
                    .ThenInclude(ct => ct.MaHhNavigation)
                .FirstOrDefaultAsync(m => m.MaHd == id);

            if (hoaDon == null)
            {
                return NotFound();
            }

            return View(hoaDon);
        }

        // GET: HoaDon/UpdateStatus/5
        public async Task<IActionResult> UpdateStatus(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hoaDon = await Context.HoaDons
                .Include(h => h.MaTrangThaiNavigation)
                .FirstOrDefaultAsync(m => m.MaHd == id);

            if (hoaDon == null)
            {
                return NotFound();
            }

            ViewData["TrangThai"] = await Context.TrangThais.ToListAsync();
            return View(hoaDon);
        }

        // POST: HoaDon/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, int maTrangThai, string? lyDo, DateTime? ngayHuy)
        {
            var hoaDon = await Context.HoaDons.FindAsync(id);
            if (hoaDon == null)
            {
                return NotFound();
            }

            hoaDon.MaTrangThai = maTrangThai;
            if (maTrangThai == 4) // Giả sử 4 là trạng thái Hủy
            {
                hoaDon.NgayHuy = ngayHuy ?? DateTime.Now;
                hoaDon.LyDo = lyDo;
            }
            else
            {
                hoaDon.NgayHuy = null;
                hoaDon.LyDo = null;
            }

            try
            {
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!Context.HoaDons.Any(e => e.MaHd == id))
                {
                    return NotFound();
                }
                throw;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}