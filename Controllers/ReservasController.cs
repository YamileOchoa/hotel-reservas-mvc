using Hotel.Web.Models;
using Hotel.Web.Repositorios;
using Hotel.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Web.Controllers;

public class ReservasController : Controller
{
    private readonly ReservaRepositorio _repo;
    public ReservasController(ReservaRepositorio repo) => _repo = repo;

    public async Task<IActionResult> Index() => View(await _repo.ListarAsync());

    public async Task<IActionResult> Disponibles(DateOnly? ingreso, DateOnly? salida)
    {
        var vm = new DisponiblesVM { Ingreso = ingreso, Salida = salida };
        if (ingreso.HasValue && salida.HasValue)
        {
            if (salida <= ingreso)
                ModelState.AddModelError("", "La salida debe ser posterior al ingreso.");
            else
                vm.Habitaciones = await _repo.ListarDisponiblesAsync(ingreso.Value, salida.Value);
        }
        return View(vm);
    }

    private async Task CargarListas()
    {
        ViewBag.Huespedes = await _repo.ListarHuespedesActivosAsync();
        ViewBag.Habitaciones = await _repo.ListarHabitacionesActivasAsync();
    }

    public async Task<IActionResult> Create()
    {
        await CargarListas();
        return View(new Reserva
        {
            FechaIngreso = DateOnly.FromDateTime(DateTime.Today),
            FechaSalida = DateOnly.FromDateTime(DateTime.Today).AddDays(1)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("IdHuesped,IdHabitacion,FechaIngreso,FechaSalida,Observaciones")] Reserva r)
    {
        ModelState.Remove(nameof(Reserva.IdHuespedNavigation));
        ModelState.Remove(nameof(Reserva.IdHabitacionNavigation));
        ModelState.Remove(nameof(Reserva.Estado));

        if (r.FechaSalida <= r.FechaIngreso)
            ModelState.AddModelError(nameof(r.FechaSalida), "La salida debe ser posterior al ingreso.");
        if (r.FechaIngreso < DateOnly.FromDateTime(DateTime.Today))
            ModelState.AddModelError(nameof(r.FechaIngreso), "El ingreso no puede ser anterior a hoy.");
        if (ModelState.IsValid &&
            await _repo.HabitacionOcupadaAsync(r.IdHabitacion, r.FechaIngreso, r.FechaSalida))
            ModelState.AddModelError(nameof(r.IdHabitacion), "La habitación ya está reservada en ese rango de fechas.");

        if (ModelState.IsValid)
        {
            await _repo.CrearAsync(r);
            TempData["Mensaje"] = "Reserva registrada (Pendiente).";
            return RedirectToAction(nameof(Index));
        }
        await CargarListas();
        return View(r);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        TempData["Mensaje"] = await _repo.CancelarAsync(id)
            ? "Reserva cancelada." : "Solo se pueden cancelar reservas Pendientes o Confirmadas.";
        return RedirectToAction(nameof(Index));
    }
}