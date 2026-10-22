namespace Hotel.Web.ViewModels;

public class HabitacionDisponibleVM
{
    public int IdHabitacion { get; set; }
    public string Numero { get; set; } = "";
    public int Piso { get; set; }
    public string Tipo { get; set; } = "";
    public int Capacidad { get; set; }
    public decimal PrecioNoche { get; set; }
    public int Noches { get; set; }
    public decimal Total { get; set; }
}

public class DisponiblesVM
{
    public DateOnly? Ingreso { get; set; }
    public DateOnly? Salida { get; set; }
    public List<HabitacionDisponibleVM> Habitaciones { get; set; } = new();
}

public class ReservaListaVM
{
    public int IdReserva { get; set; }
    public string Huesped { get; set; } = "";
    public string Habitacion { get; set; } = "";
    public DateOnly FechaIngreso { get; set; }
    public DateOnly FechaSalida { get; set; }
    public string Estado { get; set; } = "";
    public decimal PrecioNoche { get; set; }
    public int Noches => FechaSalida.DayNumber - FechaIngreso.DayNumber;
    public decimal Total => Noches * PrecioNoche;
}