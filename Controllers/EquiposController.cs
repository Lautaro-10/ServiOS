using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySqlConnector;
using ServiOS.Models;

namespace ServiOS.Controllers;

public class EquiposController : Controller
{
    private readonly string _connectionString;

    public EquiposController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    private void CargarClientesEnViewBag()
    {
        var clientes = new List<SelectListItem>();

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "SELECT Id, CONCAT(Apellido, ', ', Nombre, ' (DNI: ', Dni, ')') AS InfoCliente FROM Clientes ORDER BY Apellido, Nombre";

            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                clientes.Add(new SelectListItem(
                    reader["InfoCliente"].ToString(),
                    reader["Id"].ToString()
                ));
            }
        }

        ViewBag.Clientes = clientes;
    }

    // GET: Equipos
    public IActionResult Index()
    {
        var equipos = new List<Equipo>();

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = @"SELECT e.Id, e.ClienteId, e.Marca, e.Modelo, e.Imei, 
                                  e.ClaveDesbloqueo, e.ObservacionesEsteticas, e.Activo,
                                  c.Nombre AS CliNombre, c.Apellido AS CliApellido, c.Dni AS CliDni
                           FROM Equipos e
                           INNER JOIN Clientes c ON e.ClienteId = c.Id
                           ORDER BY e.Marca, e.Modelo";

            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                equipos.Add(new Equipo
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    ClienteId = Convert.ToInt32(reader["ClienteId"]),
                    Marca = reader["Marca"].ToString() ?? "",
                    Modelo = reader["Modelo"].ToString() ?? "",
                    Imei = reader["Imei"].ToString() ?? "",
                    ClaveDesbloqueo = reader["ClaveDesbloqueo"] != DBNull.Value ? reader["ClaveDesbloqueo"].ToString() : null,
                    ObservacionesEsteticas = reader["ObservacionesEsteticas"] != DBNull.Value ? reader["ObservacionesEsteticas"].ToString() : null,
                    Activo = Convert.ToBoolean(reader["Activo"]),
                    Cliente = new Cliente
                    {
                        Id = Convert.ToInt32(reader["ClienteId"]),
                        Nombre = reader["CliNombre"].ToString() ?? "",
                        Apellido = reader["CliApellido"].ToString() ?? "",
                        Dni = reader["CliDni"].ToString() ?? ""
                    }
                });
            }
        }

        return View(equipos);
    }

    // GET: Equipos/Details/5
    public IActionResult Details(int id)
    {
        Equipo? equipo = null;

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = @"SELECT e.Id, e.ClienteId, e.Marca, e.Modelo, e.Imei, 
                                  e.ClaveDesbloqueo, e.ObservacionesEsteticas, e.Activo,
                                  c.Nombre AS CliNombre, c.Apellido AS CliApellido, c.Dni AS CliDni, c.Telefono AS CliTel, c.Email AS CliEmail
                           FROM Equipos e
                           INNER JOIN Clientes c ON e.ClienteId = c.Id
                           WHERE e.Id = @id";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                equipo = new Equipo
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    ClienteId = Convert.ToInt32(reader["ClienteId"]),
                    Marca = reader["Marca"].ToString() ?? "",
                    Modelo = reader["Modelo"].ToString() ?? "",
                    Imei = reader["Imei"].ToString() ?? "",
                    ClaveDesbloqueo = reader["ClaveDesbloqueo"] != DBNull.Value ? reader["ClaveDesbloqueo"].ToString() : null,
                    ObservacionesEsteticas = reader["ObservacionesEsteticas"] != DBNull.Value ? reader["ObservacionesEsteticas"].ToString() : null,
                    Activo = Convert.ToBoolean(reader["Activo"]),
                    Cliente = new Cliente
                    {
                        Id = Convert.ToInt32(reader["ClienteId"]),
                        Nombre = reader["CliNombre"].ToString() ?? "",
                        Apellido = reader["CliApellido"].ToString() ?? "",
                        Dni = reader["CliDni"].ToString() ?? "",
                        Telefono = reader["CliTel"] != DBNull.Value ? reader["CliTel"].ToString() : null,
                        Email = reader["CliEmail"] != DBNull.Value ? reader["CliEmail"].ToString() : null
                    }
                };
            }
        }

        if (equipo == null)
        {
            return NotFound();
        }

        return View(equipo);
    }

    // GET: Equipos/Create
    public IActionResult Create()
    {
        CargarClientesEnViewBag();
        return View();
    }

    // POST: Equipos/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Equipo equipo)
    {
        if (!ModelState.IsValid)
        {
            CargarClientesEnViewBag();
            return View(equipo);
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            // Validar unicidad del IMEI
            string checkSql = "SELECT COUNT(1) FROM Equipos WHERE Imei = @imei";
            using (var checkCmd = new MySqlCommand(checkSql, connection))
            {
                checkCmd.Parameters.AddWithValue("@imei", equipo.Imei);
                long count = Convert.ToInt64(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    ModelState.AddModelError(nameof(Equipo.Imei), "Ya existe un equipo registrado con ese número de IMEI.");
                    CargarClientesEnViewBag();
                    return View(equipo);
                }
            }

            string sql = @"INSERT INTO Equipos 
                           (ClienteId, Marca, Modelo, Imei, ClaveDesbloqueo, ObservacionesEsteticas, Activo) 
                           VALUES (@clienteId, @marca, @modelo, @imei, @clave, @obs, @activo)";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@clienteId", equipo.ClienteId);
            command.Parameters.AddWithValue("@marca", equipo.Marca);
            command.Parameters.AddWithValue("@modelo", equipo.Modelo);
            command.Parameters.AddWithValue("@imei", equipo.Imei);
            command.Parameters.AddWithValue("@clave", (object?)equipo.ClaveDesbloqueo ?? DBNull.Value);
            command.Parameters.AddWithValue("@obs", (object?)equipo.ObservacionesEsteticas ?? DBNull.Value);
            command.Parameters.AddWithValue("@activo", equipo.Activo ? 1 : 0);

            command.ExecuteNonQuery();
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Equipos/Edit/5
    public IActionResult Edit(int id)
    {
        Equipo? equipo = ObtenerPorId(id);
        if (equipo == null)
        {
            return NotFound();
        }

        CargarClientesEnViewBag();
        return View(equipo);
    }

    // POST: Equipos/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Equipo equipo)
    {
        if (id != equipo.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            CargarClientesEnViewBag();
            return View(equipo);
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            // Validar unicidad de IMEI excluyendo al propio equipo
            string checkSql = "SELECT COUNT(1) FROM Equipos WHERE Imei = @imei AND Id != @id";
            using (var checkCmd = new MySqlCommand(checkSql, connection))
            {
                checkCmd.Parameters.AddWithValue("@imei", equipo.Imei);
                checkCmd.Parameters.AddWithValue("@id", id);
                long count = Convert.ToInt64(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    ModelState.AddModelError(nameof(Equipo.Imei), "Ya existe otro equipo registrado con ese número de IMEI.");
                    CargarClientesEnViewBag();
                    return View(equipo);
                }
            }

            string sql = @"UPDATE Equipos 
                           SET ClienteId = @clienteId, Marca = @marca, Modelo = @modelo, 
                               Imei = @imei, ClaveDesbloqueo = @clave, ObservacionesEsteticas = @obs, 
                               Activo = @activo 
                           WHERE Id = @id";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@clienteId", equipo.ClienteId);
            command.Parameters.AddWithValue("@marca", equipo.Marca);
            command.Parameters.AddWithValue("@modelo", equipo.Modelo);
            command.Parameters.AddWithValue("@imei", equipo.Imei);
            command.Parameters.AddWithValue("@clave", (object?)equipo.ClaveDesbloqueo ?? DBNull.Value);
            command.Parameters.AddWithValue("@obs", (object?)equipo.ObservacionesEsteticas ?? DBNull.Value);
            command.Parameters.AddWithValue("@activo", equipo.Activo ? 1 : 0);

            int rows = command.ExecuteNonQuery();
            if (rows == 0)
            {
                return NotFound();
            }
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Equipos/Delete/5
    public IActionResult Delete(int id)
    {
        return Details(id);
    }

    // POST: Equipos/Delete/5
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "DELETE FROM Equipos WHERE Id = @id";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        return RedirectToAction(nameof(Index));
    }

    private Equipo? ObtenerPorId(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        string sql = "SELECT * FROM Equipos WHERE Id = @id";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        using var reader = command.ExecuteReader();

        if (reader.Read())
        {
            return new Equipo
            {
                Id = Convert.ToInt32(reader["Id"]),
                ClienteId = Convert.ToInt32(reader["ClienteId"]),
                Marca = reader["Marca"].ToString() ?? "",
                Modelo = reader["Modelo"].ToString() ?? "",
                Imei = reader["Imei"].ToString() ?? "",
                ClaveDesbloqueo = reader["ClaveDesbloqueo"] != DBNull.Value ? reader["ClaveDesbloqueo"].ToString() : null,
                ObservacionesEsteticas = reader["ObservacionesEsteticas"] != DBNull.Value ? reader["ObservacionesEsteticas"].ToString() : null,
                Activo = Convert.ToBoolean(reader["Activo"])
            };
        }

        return null;
    }
}