using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySqlConnector;
using ServiOS.Models;
using System.Security.Claims;

namespace ServiOS.Controllers;

[Authorize(Policy = "SoloAdmin")]
public class PagosController : Controller
{
    private const int TamanioPagina = 10;
    private readonly string _connectionString;

    public PagosController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    public IActionResult Index(int pagina = 1)
    {
        pagina = Math.Max(1, pagina);
        var pagos = new List<Pago>();
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        int totalRegistros;
        using (var countCommand = new MySqlCommand("SELECT COUNT(1) FROM Pagos", connection))
        {
            totalRegistros = Convert.ToInt32(countCommand.ExecuteScalar());
        }

        var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)TamanioPagina);
        pagina = totalPaginas == 0 ? 1 : Math.Min(pagina, totalPaginas);

        const string sql = @"SELECT p.Id, p.OrdenId, p.UsuarioCobroId, p.FechaPago, p.Importe, p.Concepto, p.Anulado,
                                    o.EquipoId, e.Marca, e.Modelo, e.Imei,
                                    c.Nombre AS ClienteNombre, c.Apellido AS ClienteApellido, c.Dni,
                                    u.Nombre AS UsuarioNombre, u.Apellido AS UsuarioApellido
                             FROM Pagos p
                             INNER JOIN OrdenesReparacion o ON p.OrdenId = o.Id
                             INNER JOIN Equipos e ON o.EquipoId = e.Id
                             INNER JOIN Clientes c ON e.ClienteId = c.Id
                             INNER JOIN Usuarios u ON p.UsuarioCobroId = u.Id
                             ORDER BY p.FechaPago DESC, p.Id DESC
                             LIMIT @tamanioPagina OFFSET @offset";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tamanioPagina", TamanioPagina);
        command.Parameters.AddWithValue("@offset", (pagina - 1) * TamanioPagina);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            pagos.Add(MapearPago(reader));
        }

        return View(new PaginacionRespuesta<Pago>
        {
            Elementos = pagos,
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            TotalRegistros = totalRegistros,
            TamanioPagina = TamanioPagina
        });
    }

    public IActionResult Details(int id)
    {
        var pago = ObtenerPorId(id);
        return pago == null ? NotFound() : View(pago);
    }

    public IActionResult Create()
    {
        CargarOrdenes();
        return View(new Pago { FechaPago = DateTime.Now });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Pago pago)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            return Forbid();
        }

        pago.UsuarioCobroId = usuarioId;
        if (!ModelState.IsValid)
        {
            CargarOrdenes();
            return View(pago);
        }

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = @"INSERT INTO Pagos (OrdenId, UsuarioCobroId, FechaPago, Importe, Concepto)
                             VALUES (@ordenId, @usuarioId, @fecha, @importe, @concepto)";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ordenId", pago.OrdenId);
        command.Parameters.AddWithValue("@usuarioId", usuarioId);
        command.Parameters.AddWithValue("@fecha", pago.FechaPago);
        command.Parameters.AddWithValue("@importe", pago.Importe);
        command.Parameters.AddWithValue("@concepto", pago.Concepto);
        command.ExecuteNonQuery();

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var pago = ObtenerPorId(id);
        if (pago == null)
        {
            return NotFound();
        }

        if (pago.Anulado)
        {
            return BadRequest("No se puede editar un pago anulado.");
        }

        CargarOrdenes(pago.OrdenId);
        return View(pago);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Pago pago)
    {
        if (id != pago.Id)
        {
            return BadRequest();
        }

        var original = ObtenerPorId(id);
        if (original == null)
        {
            return NotFound();
        }

        if (original.Anulado)
        {
            return BadRequest("No se puede editar un pago anulado.");
        }

        pago.UsuarioCobroId = original.UsuarioCobroId;
        if (!ModelState.IsValid)
        {
            CargarOrdenes(pago.OrdenId);
            return View(pago);
        }

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = @"UPDATE Pagos
                             SET OrdenId = @ordenId, FechaPago = @fecha, Importe = @importe, Concepto = @concepto
                             WHERE Id = @id AND Anulado = 0";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@ordenId", pago.OrdenId);
        command.Parameters.AddWithValue("@fecha", pago.FechaPago);
        command.Parameters.AddWithValue("@importe", pago.Importe);
        command.Parameters.AddWithValue("@concepto", pago.Concepto);
        if (command.ExecuteNonQuery() == 0)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var pago = ObtenerPorId(id);
        if (pago == null)
        {
            return NotFound();
        }

        if (pago.Anulado)
        {
            return BadRequest("El pago ya está anulado.");
        }

        return View(pago);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = "UPDATE Pagos SET Anulado = 1 WHERE Id = @id AND Anulado = 0";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        if (command.ExecuteNonQuery() == 0)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    private void CargarOrdenes(int? seleccionada = null)
    {
        var ordenes = new List<SelectListItem>();
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = @"SELECT o.Id, e.Marca, e.Modelo, e.Imei, c.Apellido, c.Nombre
                             FROM OrdenesReparacion o
                             INNER JOIN Equipos e ON o.EquipoId = e.Id
                             INNER JOIN Clientes c ON e.ClienteId = c.Id
                             ORDER BY o.Id DESC";
        using var command = new MySqlCommand(sql, connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var id = Convert.ToInt32(reader["Id"]);
            var equipo = $"{reader["Marca"]} {reader["Modelo"]} - {reader["Imei"]}";
            var cliente = $"{reader["Apellido"]}, {reader["Nombre"]}";
            ordenes.Add(new SelectListItem(
                $"Orden #{id} - {equipo} - {cliente}",
                id.ToString(),
                seleccionada == id));
        }

        ViewBag.Ordenes = ordenes;
    }

    private Pago? ObtenerPorId(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = @"SELECT p.Id, p.OrdenId, p.UsuarioCobroId, p.FechaPago, p.Importe, p.Concepto, p.Anulado,
                                    o.EquipoId, e.Marca, e.Modelo, e.Imei,
                                    c.Nombre AS ClienteNombre, c.Apellido AS ClienteApellido, c.Dni,
                                    u.Nombre AS UsuarioNombre, u.Apellido AS UsuarioApellido
                             FROM Pagos p
                             INNER JOIN OrdenesReparacion o ON p.OrdenId = o.Id
                             INNER JOIN Equipos e ON o.EquipoId = e.Id
                             INNER JOIN Clientes c ON e.ClienteId = c.Id
                             INNER JOIN Usuarios u ON p.UsuarioCobroId = u.Id
                             WHERE p.Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapearPago(reader) : null;
    }

    private static Pago MapearPago(MySqlDataReader reader)
    {
        var ordenId = Convert.ToInt32(reader["OrdenId"]);
        return new Pago
        {
            Id = Convert.ToInt32(reader["Id"]),
            OrdenId = ordenId,
            UsuarioCobroId = Convert.ToInt32(reader["UsuarioCobroId"]),
            FechaPago = Convert.ToDateTime(reader["FechaPago"]),
            Importe = Convert.ToDecimal(reader["Importe"]),
            Concepto = reader["Concepto"].ToString() ?? string.Empty,
            Anulado = Convert.ToBoolean(reader["Anulado"]),
            Orden = new OrdenReparacion
            {
                Id = ordenId,
                Equipo = new Equipo
                {
                    Id = Convert.ToInt32(reader["EquipoId"]),
                    Marca = reader["Marca"].ToString() ?? string.Empty,
                    Modelo = reader["Modelo"].ToString() ?? string.Empty,
                    Imei = reader["Imei"].ToString() ?? string.Empty,
                    Cliente = new Cliente
                    {
                        Nombre = reader["ClienteNombre"].ToString() ?? string.Empty,
                        Apellido = reader["ClienteApellido"].ToString() ?? string.Empty,
                        Dni = reader["Dni"].ToString() ?? string.Empty
                    }
                }
            },
            UsuarioCobro = new Usuario
            {
                Nombre = reader["UsuarioNombre"].ToString() ?? string.Empty,
                Apellido = reader["UsuarioApellido"].ToString() ?? string.Empty
            }
        };
    }
}
