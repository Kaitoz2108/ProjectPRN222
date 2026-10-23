using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeManagementSystem.Models.Entities;
using CafeManagementSystem.Models.ViewModels;

namespace CafeManagementSystem.Controllers;

public class AccountController : Controller
{
    private readonly CafeManagementDbContext _context;

    public AccountController(CafeManagementDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // 1. LOGIN (GET & POST)
    // ==========================================
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return RedirectByRole(role);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Phone == model.Phone.Trim() && u.PasswordHash == model.Password);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Số điện thoại hoặc mật khẩu không chính xác.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản lý.");
            return View(model);
        }

        // Tạo danh sách Claims xác thực
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.RoleName),
            new Claim(ClaimTypes.MobilePhone, user.Phone),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim("LoyaltyPoints", user.LoyaltyPoints.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(model.RememberMe ? 72 : 8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        // Lưu thông tin phiên làm việc vào Session
        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("FullName", user.FullName);
        HttpContext.Session.SetString("Role", user.Role.RoleName);
        HttpContext.Session.SetString("Phone", user.Phone);

        TempData["SuccessMessage"] = $"Đăng nhập thành công! Chào mừng {user.FullName} ({user.Role.RoleName}).";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectByRole(user.Role.RoleName);
    }

    // ==========================================
    // 2. REGISTER (GET & POST) - CUSTOMER ROLE
    // ==========================================
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return RedirectByRole(role);
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var phoneTrimmed = model.Phone.Trim();
        if (await _context.Users.AnyAsync(u => u.Phone == phoneTrimmed))
        {
            ModelState.AddModelError("Phone", "Số điện thoại này đã được đăng ký.");
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var emailTrimmed = model.Email.Trim();
            if (await _context.Users.AnyAsync(u => u.Email == emailTrimmed))
            {
                ModelState.AddModelError("Email", "Địa chỉ email này đã tồn tại trong hệ thống.");
                return View(model);
            }
        }

        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
        if (customerRole == null)
        {
            ModelState.AddModelError(string.Empty, "Lỗi hệ thống: Vai trò Customer không tồn tại.");
            return View(model);
        }

        var newUser = new User
        {
            RoleId = customerRole.RoleId,
            FullName = model.FullName.Trim(),
            Phone = phoneTrimmed,
            Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
            PasswordHash = model.Password,
            Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim(),
            LoyaltyPoints = 0,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Bạn có thể đăng nhập ngay bây giờ.";
        return RedirectToAction(nameof(Login));
    }

    // ==========================================
    // 3. PROFILE (GET & POST)
    // ==========================================
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return NotFound();
        }

        var model = new ProfileViewModel
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Phone = user.Phone,
            Email = user.Email,
            Address = user.Address,
            RoleName = user.Role.RoleName,
            LoyaltyPoints = user.LoyaltyPoints,
            CreatedAt = user.CreatedAt
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.RoleName = user.Role.RoleName;
            model.LoyaltyPoints = user.LoyaltyPoints;
            model.Phone = user.Phone;
            model.CreatedAt = user.CreatedAt;
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var emailTrimmed = model.Email.Trim();
            if (await _context.Users.AnyAsync(u => u.Email == emailTrimmed && u.UserId != userId))
            {
                ModelState.AddModelError("Email", "Địa chỉ email này đã được người khác sử dụng.");
                model.RoleName = user.Role.RoleName;
                model.LoyaltyPoints = user.LoyaltyPoints;
                model.Phone = user.Phone;
                model.CreatedAt = user.CreatedAt;
                return View(model);
            }
            user.Email = emailTrimmed;
        }
        else
        {
            user.Email = null;
        }

        user.FullName = model.FullName.Trim();
        user.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();

        await _context.SaveChangesAsync();

        HttpContext.Session.SetString("FullName", user.FullName);
        TempData["SuccessMessage"] = "Cập nhật hồ sơ cá nhân thành công!";

        return RedirectToAction(nameof(Profile));
    }

    // ==========================================
    // 4. LOGOUT (GET)
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "Bạn đã đăng xuất khỏi hệ thống.";
        return RedirectToAction(nameof(Login));
    }

    // ==========================================
    // 5. ACCESS DENIED (GET)
    // ==========================================
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // Helper: Điều hướng tự động theo 5 Role
    private IActionResult RedirectByRole(string? role)
    {
        return role switch
        {
            "Admin" => RedirectToAction("Dashboard", "Admin"),
            "Cashier" => RedirectToAction("Shift", "Cashier"),
            "Barista" => RedirectToAction("KDS", "Barista"),
            "DeliveryStaff" => RedirectToAction("Index", "Delivery"),
            "Customer" => RedirectToAction("Menu", "Customer"),
            _ => RedirectToAction("Index", "Home")
        };
    }
}
