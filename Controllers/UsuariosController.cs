using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using MySqlConnector;
using ServiOS.Helpers;
using ServiOS.Models;

namespace ServiOS.Controllers;

public class UsuariosController : Controller
{
    private readonly string _connectionString;
    private readonly IWebHostEnvironment _environment;

    public UsuariosController(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        _environment = environment;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Debe completar correo y contraseña.");
            return View();
        }

        Usuario? usuario = null;

        using (var connection = new MySqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            string sql = "SELECT Id, Nombre, Apellido, Email, PasswordHash, Rol, Avatar, Activo FROM Usuarios WHERE Email = @email AND Activo = 1";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@email", email);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                usuario = new Usuario
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Nombre = reader["Nombre"].ToString() ?? "",
                    Apellido = reader["Apellido"].ToString() ?? "",
                    Email = reader["Email"].ToString() ?? "",
                    PasswordHash = reader["PasswordHash"].ToString() ?? "",
                    Rol = reader["Rol"].ToString() ?? "Tecnico",
                    Avatar = reader["Avatar"] != DBNull.Value ? reader["Avatar"].ToString() : null,
                    Activo = Convert.ToBoolean(reader["Activo"])
                };
            }
        }

        if (usuario == null || !HashHelper.Verificar(password, usuario.PasswordHash))
        {
            ModelState.AddModelError("", "Credenciales incorrectas o usuario inactivo.");
            return View();
        }


        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, $"{usuario.Nombre} {usuario.Apellido}"),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol),
            new("Avatar", usuario.Avatar ?? "/img/default-avatar.png")
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Usuarios");
    }

    [Authorize]
    [HttpGet]
    public IActionResult Perfil()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            return Forbid();
        }

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = "SELECT Id, Nombre, Apellido, Email, Rol, Avatar FROM Usuarios WHERE Id = @id AND Activo = 1";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", usuarioId);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return NotFound();
        }

        var usuario = new Usuario
        {
            Id = Convert.ToInt32(reader["Id"]),
            Nombre = reader["Nombre"].ToString() ?? string.Empty,
            Apellido = reader["Apellido"].ToString() ?? string.Empty,
            Email = reader["Email"].ToString() ?? string.Empty,
            Rol = reader["Rol"].ToString() ?? string.Empty,
            Avatar = reader["Avatar"] != DBNull.Value ? reader["Avatar"].ToString() : null
        };

        return View(usuario);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Perfil(IFormFile? avatar, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            return Forbid();
        }

        if (avatar == null)
        {
            TempData["Error"] = "Seleccioná una imagen para actualizar el avatar.";
            return RedirectToAction(nameof(Perfil));
        }

        var avatarAnterior = ObtenerAvatar(usuarioId);
        if (avatarAnterior == null)
        {
            return NotFound();
        }

        string nuevoAvatar;
        try
        {
            nuevoAvatar = await ImageUploadHelper.SaveAsync(
                avatar,
                WebRootPath,
                "avatares",
                cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Perfil));
        }

        try
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            const string sql = "UPDATE Usuarios SET Avatar = @avatar WHERE Id = @id AND Activo = 1";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@avatar", nuevoAvatar);
            command.Parameters.AddWithValue("@id", usuarioId);
            if (command.ExecuteNonQuery() == 0)
            {
                ImageUploadHelper.DeleteManagedFile(WebRootPath, nuevoAvatar, "avatares");
                return NotFound();
            }
        }
        catch (MySqlException)
        {
            ImageUploadHelper.DeleteManagedFile(WebRootPath, nuevoAvatar, "avatares");
            throw;
        }

        var claims = User.Claims.Where(claim => claim.Type != "Avatar").ToList();
        claims.Add(new Claim("Avatar", nuevoAvatar));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

        ImageUploadHelper.DeleteManagedFile(WebRootPath, avatarAnterior, "avatares");
        TempData["Mensaje"] = "El avatar se actualizó correctamente.";
        return RedirectToAction(nameof(Perfil));
    }

    private string? ObtenerAvatar(int usuarioId)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = "SELECT Avatar FROM Usuarios WHERE Id = @id AND Activo = 1";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", usuarioId);
        var avatar = command.ExecuteScalar();
        return avatar == null ? null : avatar == DBNull.Value ? string.Empty : Convert.ToString(avatar);
    }

    private string WebRootPath =>
        _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
}