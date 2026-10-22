using Hotel.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Web.Repositorios;

public class HabitacionRepositorio
{
    private readonly HotelContext _context;
    public HabitacionRepositorio(HotelContext context) => _context = context;

    public async Task<List<Habitacion>> ListarActivasAsync(int? idTipo) =>
        await _context.Habitacions.AsNoTracking()
            .Include(h => h.IdTipoHabitacionNavigation)
            .Where(h => h.Activo && (idTipo == null || h.IdTipoHabitacion == idTipo))
            .OrderBy(h => h.Numero)
            .ToListAsync();

    public async Task<Habitacion?> ObtenerAsync(int id) =>
        await _context.Habitacions.AsNoTracking()
            .Include(h => h.IdTipoHabitacionNavigation)
            .FirstOrDefaultAsync(h => h.IdHabitacion == id && h.Activo);

    public async Task<List<TipoHabitacion>> ListarTiposAsync() =>
        await _context.TipoHabitacions.AsNoTracking()
            .Where(t => t.Activo).OrderBy(t => t.Nombre).ToListAsync();

    public async Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null) =>
        await _context.Habitacions.AnyAsync(h =>
            h.Numero == numero && (excluirId == null || h.IdHabitacion != excluirId));

    public async Task CrearAsync(Habitacion h)
    {
        h.Activo = true;
        _context.Habitacions.Add(h);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ActualizarAsync(Habitacion datos)
    {
        var h = await _context.Habitacions.FindAsync(datos.IdHabitacion);
        if (h == null || !h.Activo) return false;
        h.Numero = datos.Numero;
        h.Piso = datos.Piso;
        h.IdTipoHabitacion = datos.IdTipoHabitacion;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DesactivarAsync(int id)   // eliminación lógica
    {
        var h = await _context.Habitacions.FindAsync(id);
        if (h == null) return false;
        h.Activo = false;
        await _context.SaveChangesAsync();
        return true;
    }
}