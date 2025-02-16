using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using Identity.Server.Keycloak.DTOs;
using Identity.Server.Keycloak.Services;

namespace Identity.Server.Keycloak.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KeycloakController : ControllerBase
{
    private readonly KeycloakService _keycloakService;

    public KeycloakController(KeycloakService keycloakService)
    {
        _keycloakService = keycloakService;
    }

    /// 🔹 **ADMIN TOKEN OLISH (master realm)**
    [HttpGet("admin-token")]
public async Task<IActionResult> GetAdminToken()
{
    try
    {
        var token = await _keycloakService.GetAdminTokenAsync();
        return Ok(new { access_token = token });
    }
    catch (Exception ex)
    {
        return BadRequest(new { error = ex.Message });
    }
}

    /// 🔹 **FOYDALANUVCHI YARATISH**
    [HttpPost("create-user")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto model)
    {
        var result = await _keycloakService.CreateUserAsync(model.Username, model.Password, model.Roles);
        if (!result)
            return BadRequest(new { message = "Foydalanuvchi yaratib bo‘lmadi" });

        return Ok(new { message = "Foydalanuvchi muvaffaqiyatli yaratildi" });
    }

    /// 🔹 **FOYDALANUVCHI LOGIN QILISH (platform client, ilmhub realm)**
    [HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginDto model)
{
    try
    {
        var token = await _keycloakService.LoginAsync(model.Username, model.Password);
        if (token == null)
            return Unauthorized(new { message = "Noto‘g‘ri username yoki parol" });

        return Ok(new { access_token = token });
    }
    catch (Exception ex)
    {
        return BadRequest(new { error = ex.Message });
    }
}


[HttpPut("update-user/{userId}")]
public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserDto model)
{
    try
    {
        var result = await _keycloakService.UpdateUserAsync(userId, model.NewUsername, model.NewPassword);
        if (!result)
            return BadRequest(new { message = "Foydalanuvchini yangilashda xatolik" });

        return Ok(new { message = "Foydalanuvchi muvaffaqiyatli yangilandi" });
    }
    catch (Exception ex)
    {
        return BadRequest(new { error = ex.Message });
    }
}

    /// 🔹 **BARCHA FOYDALANUVCHILARNI OLISH (ilmhub realm)**
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _keycloakService.GetUsersAsync();
        return Ok(users);
    }

    /// 🔹 **USERNAME BO‘YICHA FOYDALANUVCHI QIDIRISH**
    [HttpGet("user/{username}")]
    public async Task<IActionResult> GetUserByUsername(string username)
    {
        var user = await _keycloakService.GetUserByUsernameAsync(username);
        if (user == null)
            return NotFound(new { message = "Foydalanuvchi topilmadi" });

        return Ok(user);
    }

    /// 🔹 **FOYDALANUVCHI O‘CHIRISH**
    [HttpDelete("delete-user/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var result = await _keycloakService.DeleteUserAsync(userId);
        if (!result)
            return BadRequest(new { message = "Foydalanuvchini o‘chirishda xatolik" });

        return Ok(new { message = "Foydalanuvchi muvaffaqiyatli o‘chirildi" });
    }

    /// 🔹 **BARCHA ROLELARNI OLISH**
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _keycloakService.GetRolesAsync();
        return Ok(roles);
    }
}
