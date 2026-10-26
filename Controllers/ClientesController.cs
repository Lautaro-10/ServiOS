using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using ServiOS.Models;

namespace ServiOS.Controllers;

public class ClientesController : Controller
{
    private readonly string _connectionString;

    public ClientesController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    // GET: Clientes
    public IActionResult Index()
    {
        var clientes = new List<Cliente>();

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "SELECT Id, Dni, Nombre, Apellido, Telefono, Email, FechaAlta FROM Clientes ORDER BY Apellido, Nombre";

            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                clientes.Add(new Cliente
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Dni = reader["Dni"].ToString() ?? "",
                    Nombre = reader["Nombre"].ToString() ?? "",
                    Apellido = reader["Apellido"].ToString() ?? "",
                    Telefono = reader["Telefono"] != DBNull.Value ? reader["Telefono"].ToString() : null,
                    Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : null,
                    FechaAlta = Convert.ToDateTime(reader["FechaAlta"])
                });
            }
        }

        return View(clientes);
    }


    public IActionResult Details(int id)
    {
        Cliente? cliente = ObtenerPorId(id);
        if (cliente == null)
        {
            return NotFound();
        }

        return View(cliente);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Cliente cliente)
    {
        if (!ModelState.IsValid)
        {
            return View(cliente);
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            string checkSql = "SELECT COUNT(1) FROM Clientes WHERE Dni = @dni";
            using (var checkCmd = new MySqlCommand(checkSql, connection))
            {
                checkCmd.Parameters.AddWithValue("@dni", cliente.Dni);
                long count = Convert.ToInt64(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    ModelState.AddModelError(nameof(Cliente.Dni), "Ya existe un cliente con ese DNI.");
                    return View(cliente);
                }
            }

            string sql = @"INSERT INTO Clientes (Dni, Nombre, Apellido, Telefono, Email) 
                           VALUES (@dni, @nombre, @apellido, @telefono, @email)";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@dni", cliente.Dni);
            command.Parameters.AddWithValue("@nombre", cliente.Nombre);
            command.Parameters.AddWithValue("@apellido", cliente.Apellido);
            command.Parameters.AddWithValue("@telefono", (object?)cliente.Telefono ?? DBNull.Value);
            command.Parameters.AddWithValue("@email", (object?)cliente.Email ?? DBNull.Value);

            command.ExecuteNonQuery();
        }

        return RedirectToAction(nameof(Index));
    }


    public IActionResult Edit(int id)
    {
        Cliente? cliente = ObtenerPorId(id);
        if (cliente == null)
        {
            return NotFound();
        }

        return View(cliente);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Cliente cliente)
    {
        if (id != cliente.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(cliente);
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            string checkSql = "SELECT COUNT(1) FROM Clientes WHERE Dni = @dni AND Id != @id";
            using (var checkCmd = new MySqlCommand(checkSql, connection))
            {
                checkCmd.Parameters.AddWithValue("@dni", cliente.Dni);
                checkCmd.Parameters.AddWithValue("@id", id);
                long count = Convert.ToInt64(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    ModelState.AddModelError(nameof(Cliente.Dni), "Ya existe otro cliente con ese DNI.");
                    return View(cliente);
                }
            }

            string sql = @"UPDATE Clientes 
                           SET Dni = @dni, Nombre = @nombre, Apellido = @apellido, Telefono = @telefono, Email = @email 
                           WHERE Id = @id";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@dni", cliente.Dni);
            command.Parameters.AddWithValue("@nombre", cliente.Nombre);
            command.Parameters.AddWithValue("@apellido", cliente.Apellido);
            command.Parameters.AddWithValue("@telefono", (object?)cliente.Telefono ?? DBNull.Value);
            command.Parameters.AddWithValue("@email", (object?)cliente.Email ?? DBNull.Value);

            int rows = command.ExecuteNonQuery();
            if (rows == 0)
            {
                return NotFound();
            }
        }

        return RedirectToAction(nameof(Index));
    }


    public IActionResult Delete(int id)
    {
        Cliente? cliente = ObtenerPorId(id);
        if (cliente == null)
        {
            return NotFound();
        }

        return View(cliente);
    }


    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "DELETE FROM Clientes WHERE Id = @id";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        return RedirectToAction(nameof(Index));
    }

    private Cliente? ObtenerPorId(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        string sql = "SELECT Id, Dni, Nombre, Apellido, Telefono, Email, FechaAlta FROM Clientes WHERE Id = @id";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        using var reader = command.ExecuteReader();

        if (reader.Read())
        {
            return new Cliente
            {
                Id = Convert.ToInt32(reader["Id"]),
                Dni = reader["Dni"].ToString() ?? "",
                Nombre = reader["Nombre"].ToString() ?? "",
                Apellido = reader["Apellido"].ToString() ?? "",
                Telefono = reader["Telefono"] != DBNull.Value ? reader["Telefono"].ToString() : null,
                Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : null,
                FechaAlta = Convert.ToDateTime(reader["FechaAlta"])
            };
        }

        return null;
    }
}