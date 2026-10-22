using Hotel.Web.Models;
using Hotel.Web.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Web.Repositorios;

public class ReservaRepositorio
{
    private readonly HotelContext _context;
    public ReservaRepositorio(HotelContext context) => _context = context;

    // Una sola consulta con !Any: habitaciones activas sin reservas que se crucen
    public async Task<List<HabitacionDisponibleVM>> ListarDisponiblesAsync(DateOnly ingreso, DateOnly salida)
    {
        var lista = await _context.Habitacions.AsNoTracking()
            .Where(h => h.Activo &&
                !h.Reservas.Any(r => r.Estado != "Cancelada"
                                  && r.FechaIngreso < salida
                                  && r.FechaSalida > ingreso))
            .OrderBy(h => h.Numero)
            .Select(h => new HabitacionDisponibleVM
            {
                IdHabitacion = h.IdHabitacion,
                Numero = h.Numero,
                Piso = h.Piso,
                Tipo = h.IdTipoHabitacionNavigation.Nombre,
                Capacidad = h.IdTipoHabitacionNavigation.Capacidad,
                PrecioNoche = h.IdTipoHabitacionNavigation.PrecioNoche
            })
            .ToListAsync();

        int noches = salida.DayNumber - ingreso.DayNumber;
        foreach (var x in lista) { x.Noches = noches; x.Total = noches * x.PrecioNoche; }
        return lista;
    }

    public async Task<bool> HabitacionOcupadaAsync(int idHabitacion, DateOnly ingreso, DateOnly salida) =>
        await _context.Reservas.AnyAsync(r => r.IdHabitacion == idHabitacion
            && r.Estado != "Cancelada"
            && r.FechaIngreso < salida
            && r.FechaSalida > ingreso);

    public async Task<List<ReservaListaVM>> ListarAsync() =>
        await _context.Reservas.AsNoTracking()
            .OrderByDescending(r => r.FechaIngreso)
            .Select(r => new ReservaListaVM
            {
                IdReserva = r.IdReserva,
                Huesped = r.IdHuespedNavigation.Apellidos + ", " + r.IdHuespedNavigation.Nombres,
                Habitacion = r.IdHabitacionNavigation.Numero,
                FechaIngreso = r.FechaIngreso,
                FechaSalida = r.FechaSalida,
                Estado = r.Estado,
                PrecioNoche = r.IdHabitacionNavigation.IdTipoHabitacionNavigation.PrecioNoche
            })
            .ToListAsync();

    public async Task<List<SelectListItem>> ListarHuespedesActivosAsync()
    {
        var datos = await _context.Huespeds.AsNoTracking()
            .Where(h => h.Activo).OrderBy(h => h.Apellidos)
            .Select(h => new { h.IdHuesped, h.Dni, h.Nombres, h.Apellidos })
            .ToListAsync();
        return datos.Select(h => new SelectListItem(
            $"{h.Apellidos}, {h.Nombres} ({h.Dni})", h.IdHuesped.ToString())).ToList();
    }

    public async Task<List<SelectListItem>> ListarHabitacionesActivasAsync()
    {
        var datos = await _context.Habitacions.AsNoTracking()
            .Where(h => h.Activo).OrderBy(h => h.Numero)
            .Select(h => new { h.IdHabitacion, h.Numero, Tipo = h.IdTipoHabitacionNavigation.Nombre })
            .ToListAsync();
        return datos.Select(h => new SelectListItem(
            $"{h.Numero} - {h.Tipo}", h.IdHabitacion.ToString())).ToList();
    }

    public async Task CrearAsync(Reserva r)
    {
        r.Estado = "Pendiente";
        _context.Reservas.Add(r);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> CancelarAsync(int id)
    {
        var r = await _context.Reservas.FindAsync(id);
        if (r == null || (r.Estado != "Pendiente" && r.Estado != "Confirmada")) return false;
        r.Estado = "Cancelada";
        await _context.SaveChangesAsync();
        return true;
    }
}