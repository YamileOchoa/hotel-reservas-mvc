using Hotel.Web.Models;
using Hotel.Web.Repositorios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hotel.Web.Controllers;

public class HabitacionesController : Controller
{
    private readonly HabitacionRepositorio _repo;
    public HabitacionesController(HabitacionRepositorio repo) => _repo = repo;

    private async Task CargarTipos(int? seleccionado = null) =>
        ViewBag.Tipos = new SelectList(await _repo.ListarTiposAsync(),
            "IdTipoHabitacion", "Nombre", seleccionado);

    public async Task<IActionResult> Index(int? idTipo)
    {
        await CargarTipos(idTipo);
        return View(await _repo.ListarActivasAsync(idTipo));
    }

    public async Task<IActionResult> Details(int id)
    {
        var h = await _repo.ObtenerAsync(id);
        return h == null ? NotFound() : View(h);
    }

    public async Task<IActionResult> Create()
    {
        await CargarTipos();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Numero,Piso,IdTipoHabitacion")] Habitacion h)
    {
        ModelState.Remove(nameof(Habitacion.IdTipoHabitacionNavigation));
        h.Numero = h.Numero?.Trim() ?? "";
        if (h.Numero.Length > 10)
            ModelState.AddModelError(nameof(h.Numero), "Máximo 10 caracteres.");
        else if (h.Numero != "" && await _repo.ExisteNumeroAsync(h.Numero))
            ModelState.AddModelError(nameof(h.Numero), "Ya existe una habitación con ese número.");

        if (ModelState.IsValid)
        {
            await _repo.CrearAsync(h);
            TempData["Mensaje"] = "Habitación creada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        await CargarTipos(h.IdTipoHabitacion);
        return View(h);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var h = await _repo.ObtenerAsync(id);
        if (h == null) return NotFound();
        await CargarTipos(h.IdTipoHabitacion);
        return View(h);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("IdHabitacion,Numero,Piso,IdTipoHabitacion")] Habitacion h)
    {
        if (id != h.IdHabitacion) return NotFound();
        ModelState.Remove(nameof(Habitacion.IdTipoHabitacionNavigation));
        h.Numero = h.Numero?.Trim() ?? "";
        if (h.Numero.Length > 10)
            ModelState.AddModelError(nameof(h.Numero), "Máximo 10 caracteres.");
        else if (h.Numero != "" && await _repo.ExisteNumeroAsync(h.Numero, h.IdHabitacion))
            ModelState.AddModelError(nameof(h.Numero), "Ya existe una habitación con ese número.");

        if (ModelState.IsValid)
        {
            if (!await _repo.ActualizarAsync(h)) return NotFound();
            TempData["Mensaje"] = "Habitación actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        await CargarTipos(h.IdTipoHabitacion);
        return View(h);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var h = await _repo.ObtenerAsync(id);
        return h == null ? NotFound() : View(h);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        TempData["Mensaje"] = await _repo.DesactivarAsync(id)
            ? "Habitación dada de baja." : "No se encontró la habitación.";
        return RedirectToAction(nameof(Index));
    }
}