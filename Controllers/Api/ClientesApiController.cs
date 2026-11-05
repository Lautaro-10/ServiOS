using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using ServiOS.Models;

namespace ServiOS.Controllers.Api;

[ApiController]
[Route("api/clientes")]
[Authorize(Policy = "SoloAdmin")]
public class ClientesApiController : ControllerBase
{
    private const int PageSize = 10;
    private readonly string _connectionString;

    public ClientesApiController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No está configurada la conexión DefaultConnection.");
    }

    [HttpGet]
    public IActionResult GetAll([FromQuery] int pagina = 1, [FromQuery] string? busqueda = null)
    {
        pagina = Math.Max(1, pagina);
        busqueda = string.IsNullOrWhiteSpace(busqueda) ? null : busqueda.Trim();

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        const string countSql = @"SELECT COUNT(1) FROM Clientes
                                  WHERE @busqueda IS NULL
                                     OR Dni LIKE @patron
                                     OR Nombre LIKE @patron
                                     OR Apellido LIKE @patron
                                     OR Telefono LIKE @patron
                                     OR Email LIKE @patron";
        int totalRegistros;
        using (var countCommand = new MySqlCommand(countSql, connection))
        {
            countCommand.Parameters.AddWithValue("@busqueda", (object?)busqueda ?? DBNull.Value);
            countCommand.Parameters.AddWithValue("@patron", $"%{busqueda}%");
            totalRegistros = Convert.ToInt32(countCommand.ExecuteScalar());
        }

        var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)PageSize);
        pagina = totalPaginas == 0 ? 1 : Math.Min(pagina, totalPaginas);

        var clientes = new List<Cliente>();
        const string dataSql = @"SELECT Id, Dni, Nombre, Apellido, Telefono, Email, FechaAlta
                                 FROM Clientes
                                 WHERE @busqueda IS NULL
                                    OR Dni LIKE @patron
                                    OR Nombre LIKE @patron
                                    OR Apellido LIKE @patron
                                    OR Telefono LIKE @patron
                                    OR Email LIKE @patron
                                 ORDER BY Apellido, Nombre
                                 LIMIT @limite OFFSET @offset";
        using (var dataCommand = new MySqlCommand(dataSql, connection))
        {
            dataCommand.Parameters.AddWithValue("@busqueda", (object?)busqueda ?? DBNull.Value);
            dataCommand.Parameters.AddWithValue("@patron", $"%{busqueda}%");
            dataCommand.Parameters.AddWithValue("@limite", PageSize);
            dataCommand.Parameters.AddWithValue("@offset", (pagina - 1) * PageSize);

            using var reader = dataCommand.ExecuteReader();
            while (reader.Read())
            {
                clientes.Add(MapearCliente(reader));
            }
        }

        return Ok(new
        {
            elementos = clientes,
            paginaActual = pagina,
            totalPaginas,
            totalRegistros,
            tamanioPagina = PageSize
        });
    }

    [HttpGet("{id:int}")]
    public IActionResult Get(int id)
    {
        var cliente = ObtenerPorId(id);
        return cliente == null ? NotFound(new { message = "No se encontró el cliente." }) : Ok(cliente);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create([FromBody] ClienteInput input)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        if (ExisteDni(connection, input.Dni, null))
        {
            return Conflict(new { message = "Ya existe un cliente con ese DNI." });
        }

        const string sql = @"INSERT INTO Clientes (Dni, Nombre, Apellido, Telefono, Email)
                             VALUES (@dni, @nombre, @apellido, @telefono, @email)";
        using var command = CrearCommand(connection, sql, input);
        try
        {
            command.ExecuteNonQuery();
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            return Conflict(new { message = "Ya existe un cliente con ese DNI." });
        }

        var cliente = ObtenerPorId((int)command.LastInsertedId);
        return CreatedAtAction(nameof(Get), new { id = cliente!.Id }, cliente);
    }

    [HttpPut("{id:int}")]
    [ValidateAntiForgeryToken]
    public IActionResult Update(int id, [FromBody] ClienteInput input)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        if (ExisteDni(connection, input.Dni, id))
        {
            return Conflict(new { message = "Ya existe otro cliente con ese DNI." });
        }

        const string sql = @"UPDATE Clientes
                             SET Dni = @dni, Nombre = @nombre, Apellido = @apellido,
                                 Telefono = @telefono, Email = @email
                             WHERE Id = @id";
        using var command = CrearCommand(connection, sql, input);
        command.Parameters.AddWithValue("@id", id);

        try
        {
            command.ExecuteNonQuery();
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            return Conflict(new { message = "Ya existe otro cliente con ese DNI." });
        }

        var cliente = ObtenerPorId(id);
        return cliente == null
            ? NotFound(new { message = "No se encontró el cliente." })
            : Ok(cliente);
    }

    [HttpDelete("{id:int}")]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        using var command = new MySqlCommand("DELETE FROM Clientes WHERE Id = @id", connection);
        command.Parameters.AddWithValue("@id", id);

        try
        {
            if (command.ExecuteNonQuery() == 0)
            {
                return NotFound(new { message = "No se encontró el cliente." });
            }
        }
        catch (MySqlException exception) when (exception.Number == 1451)
        {
            return Conflict(new { message = "No se puede eliminar el cliente porque tiene equipos asociados." });
        }

        return NoContent();
    }

    private Cliente? ObtenerPorId(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        using var command = new MySqlCommand(
            "SELECT Id, Dni, Nombre, Apellido, Telefono, Email, FechaAlta FROM Clientes WHERE Id = @id",
            connection);
        command.Parameters.AddWithValue("@id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapearCliente(reader) : null;
    }

    private static bool ExisteDni(MySqlConnection connection, string dni, int? excluirId)
    {
        const string sql = "SELECT COUNT(1) FROM Clientes WHERE Dni = @dni AND (@excluirId IS NULL OR Id <> @excluirId)";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@dni", dni);
        command.Parameters.AddWithValue("@excluirId", (object?)excluirId ?? DBNull.Value);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static MySqlCommand CrearCommand(MySqlConnection connection, string sql, ClienteInput input)
    {
        var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@dni", input.Dni);
        command.Parameters.AddWithValue("@nombre", input.Nombre);
        command.Parameters.AddWithValue("@apellido", input.Apellido);
        command.Parameters.AddWithValue("@telefono", (object?)input.Telefono ?? DBNull.Value);
        command.Parameters.AddWithValue("@email", (object?)input.Email ?? DBNull.Value);
        return command;
    }

    private static Cliente MapearCliente(MySqlDataReader reader) => new()
    {
        Id = Convert.ToInt32(reader["Id"]),
        Dni = reader["Dni"].ToString() ?? string.Empty,
        Nombre = reader["Nombre"].ToString() ?? string.Empty,
        Apellido = reader["Apellido"].ToString() ?? string.Empty,
        Telefono = reader["Telefono"] != DBNull.Value ? reader["Telefono"].ToString() : null,
        Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : null,
        FechaAlta = Convert.ToDateTime(reader["FechaAlta"])
    };

    public sealed class ClienteInput
    {
        [Required(ErrorMessage = "El DNI es obligatorio.")]
        [StringLength(15, ErrorMessage = "El DNI no puede superar los 15 caracteres.")]
        public string Dni { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(50)]
        public string Apellido { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Formato de teléfono inválido.")]
        [StringLength(50)]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "Formato de email inválido.")]
        [StringLength(100)]
        public string? Email { get; set; }
    }
}
