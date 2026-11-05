using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using ServiOS.Models;

namespace ServiOS.Controllers;

[Authorize(Policy = "SoloAdmin")]
public class TiposServicioController : Controller
{
    private readonly string _connectionString;

    public TiposServicioController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    // GET: TiposServicio
    public IActionResult Index()
    {
        var tipos = new List<TipoServicio>();

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "SELECT Id, Descripcion, CostoBaseEstimado FROM TiposServicio ORDER BY Descripcion";

            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tipos.Add(new TipoServicio
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Descripcion = reader["Descripcion"].ToString() ?? "",
                    CostoBaseEstimado = Convert.ToDecimal(reader["CostoBaseEstimado"])
                });
            }
        }

        return View(tipos);
    }

    // GET: TiposServicio/Details/5
    public IActionResult Details(int id)
    {
        TipoServicio? tipo = ObtenerPorId(id);
        if (tipo == null)
        {
            return NotFound();
        }

        return View(tipo);
    }

    // GET: TiposServicio/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: TiposServicio/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(TipoServicio tipo)
    {
        if (!ModelState.IsValid)
        {
            return View(tipo);
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "INSERT INTO TiposServicio (Descripcion, CostoBaseEstimado) VALUES (@descripcion, @costo)";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@descripcion", tipo.Descripcion);
            command.Parameters.AddWithValue("@costo", tipo.CostoBaseEstimado);

            command.ExecuteNonQuery();
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: TiposServicio/Edit/5
    public IActionResult Edit(int id)
    {
        TipoServicio? tipo = ObtenerPorId(id);
        if (tipo == null)
        {
            return NotFound();
        }

        return View(tipo);
    }

    // POST: TiposServicio/Edit/4
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, TipoServicio tipo)
    {
        if (id != tipo.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(tipo);
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "UPDATE TiposServicio SET Descripcion = @descripcion, CostoBaseEstimado = @costo WHERE Id = @id";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@descripcion", tipo.Descripcion);
            command.Parameters.AddWithValue("@costo", tipo.CostoBaseEstimado);

            int rows = command.ExecuteNonQuery();
            if (rows == 0)
            {
                return NotFound();
            }
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: TiposServicio/Delete/5
    public IActionResult Delete(int id)
    {
        TipoServicio? tipo = ObtenerPorId(id);
        if (tipo == null)
        {
            return NotFound();
        }

        return View(tipo);
    }

    // POST: TiposServicio/Delete/3
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            const string sql = "DELETE FROM TiposServicio WHERE Id = @id";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }
        catch (MySqlException exception) when (exception.Number == 1451)
        {
            var tipo = ObtenerPorId(id);
            if (tipo == null)
            {
                return NotFound();
            }

            ViewBag.Error = "No se puede eliminar el servicio porque está asociado a órdenes.";
            return View("Delete", tipo);
        }

        return RedirectToAction(nameof(Index));
    }

    private TipoServicio? ObtenerPorId(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        string sql = "SELECT Id, Descripcion, CostoBaseEstimado FROM TiposServicio WHERE Id = @id";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        using var reader = command.ExecuteReader();

        if (reader.Read())
        {
            return new TipoServicio
            {
                Id = Convert.ToInt32(reader["Id"]),
                Descripcion = reader["Descripcion"].ToString() ?? "",
                CostoBaseEstimado = Convert.ToDecimal(reader["CostoBaseEstimado"])
            };
        }

        return null;
    }
}