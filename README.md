# Hotel.Web

Aplicación web de reservas de hotel desarrollada con **ASP.NET Core MVC** y **Entity Framework Core (Database First)**. Permite administrar habitaciones, consultar disponibilidad por rango de fechas y gestionar reservas de huéspedes.

---

## Tabla de contenido

1. [Tecnologías](#tecnologías)
2. [Requisitos previos](#requisitos-previos)
3. [Base de datos](#base-de-datos)
4. [Comando de scaffold utilizado](#comando-de-scaffold-utilizado)
5. [Configuración](#configuración)
6. [Estructura del proyecto](#estructura-del-proyecto)
7. [Funcionalidades](#funcionalidades)
8. [Reglas de negocio](#reglas-de-negocio)
9. [Decisiones de arquitectura](#decisiones-de-arquitectura)
10. [Cambio de esquema: columna Observaciones](#cambio-de-esquema-columna-observaciones)
11. [Diseño visual](#diseño-visual)
12. [Cómo ejecutar](#cómo-ejecutar)

---

## Tecnologías

| Componente              | Detalle                                                                           |
| ----------------------- | --------------------------------------------------------------------------------- |
| Framework               | ASP.NET Core MVC (.NET 10)                                                        |
| Acceso a datos          | Entity Framework Core, enfoque Database First                                     |
| Base de datos           | SQL Server Express                                                                |
| Gestor de base de datos | SQL Server Management Studio (SSMS) 2022                                          |
| Paquetes                | `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design` |
| Interfaz                | Bootstrap 5, CSS propio, Razor Views                                              |
| Editor                  | Visual Studio Code                                                                |

---

## Requisitos previos

- .NET SDK 10
- SQL Server Express (instancia `DESKTOP-664EG2H\SQLEXPRESS`)
- SQL Server Management Studio (SSMS) 2022
- Visual Studio Code con la extensión C# Dev Kit
- Herramienta `dotnet-ef`:

```bash
dotnet tool install --global dotnet-ef
```

---

## Base de datos

El esquema lo define el script `HotelDB.sql`, que crea la base `HotelDB` con sus datos de prueba. No se usan migraciones ni procedimientos almacenados.

El script se ejecuta desde SQL Server Management Studio 2022 (abrir `HotelDB.sql` y presionar Ejecutar) o, de forma alternativa, desde la terminal:

```bash
sqlcmd -S DESKTOP-664EG2H\SQLEXPRESS -E -C -i HotelDB.sql
```

| Tabla            | Descripción                                                             |
| ---------------- | ----------------------------------------------------------------------- |
| `TipoHabitacion` | Tipos de habitación, capacidad y precio por noche                       |
| `Habitacion`     | Habitaciones con número, piso y tipo. Columna `Activo`                  |
| `Huesped`        | Datos de los huéspedes. Columna `Activo`                                |
| `Reserva`        | Une un huésped con una habitación. Fechas de ingreso y salida, y estado |

Los estados de una reserva son `Pendiente`, `Confirmada`, `Cancelada` y `Finalizada`.

La eliminación de habitaciones y huéspedes es **lógica** (`Activo = 0`). Nunca se ejecuta un `DELETE` físico.

---

## Comando de scaffold utilizado

Ejecutado desde la carpeta del proyecto `Hotel.Web`:

```bash
dotnet ef dbcontext scaffold "Server=DESKTOP-664EG2H\SQLEXPRESS;Database=HotelDB;Trusted_Connection=True;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context HotelContext --no-onconfiguring
```

Regeneración del modelo tras agregar la columna `Observaciones`:

```bash
dotnet ef dbcontext scaffold "Server=DESKTOP-664EG2H\SQLEXPRESS;Database=HotelDB;Trusted_Connection=True;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context HotelContext --no-onconfiguring --force
```

| Parámetro                                 | Función                                                                               |
| ----------------------------------------- | ------------------------------------------------------------------------------------- |
| `dbcontext scaffold`                      | Genera las entidades y el contexto a partir de la base existente                      |
| `Microsoft.EntityFrameworkCore.SqlServer` | Proveedor de base de datos                                                            |
| `--output-dir Models`                     | Carpeta de salida de las clases generadas                                             |
| `--context HotelContext`                  | Nombre del contexto                                                                   |
| `--no-onconfiguring`                      | Genera el contexto sin `OnConfiguring`, ya que la conexión vive en `appsettings.json` |
| `--force`                                 | Sobrescribe los archivos existentes al regenerar                                      |

Los archivos de la carpeta `Models` no se modifican manualmente.

---

## Configuración

**appsettings.json**

```json
"ConnectionStrings": {
  "HotelDB": "Server=DESKTOP-664EG2H\\SQLEXPRESS;Database=HotelDB;Trusted_Connection=True;TrustServerCertificate=True"
}
```

**Program.cs**

```csharp
builder.Services.AddDbContext<HotelContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("HotelDB")));
builder.Services.AddScoped<HabitacionRepositorio>();
builder.Services.AddScoped<ReservaRepositorio>();
```

La ruta por defecto apunta a la página de inicio:

```csharp
pattern: "{controller=Home}/{action=Index}/{id?}"
```

---

## Estructura del proyecto

```
Hotel.Web
├── Controllers
│   ├── HomeController.cs
│   ├── HabitacionesController.cs
│   └── ReservasController.cs
├── Models                  Generado con scaffold, sin modificar
├── Repositorios
│   ├── HabitacionRepositorio.cs
│   └── ReservaRepositorio.cs
├── ViewModels
│   └── ViewModels.cs
├── Views
│   ├── Home
│   ├── Habitaciones
│   ├── Reservas
│   └── Shared
│       └── _Layout.cshtml
├── wwwroot/css/site.css
├── appsettings.json
└── Program.cs
```

---

## Funcionalidades

### Habitaciones

- Listado de habitaciones activas con su tipo, ordenadas por número.
- Filtro por tipo de habitación.
- Detalle, creación y edición con lista desplegable de tipos.
- Validación de número duplicado, mostrada en el formulario con `ModelState.AddModelError`.
- Eliminación lógica con pantalla de confirmación.

### Disponibilidad

- Filtro por GET con fecha de ingreso y salida.
- Muestra las habitaciones activas libres, con número de noches y total estimado.

### Reservas

- Creación con listas desplegables de huéspedes y habitaciones activas.
- Listado con huésped, habitación, fechas, estado y total.
- Cancelación de reservas en estado `Pendiente` o `Confirmada`.

---

## Reglas de negocio

**Cruce de reservas.** Una reserva se cruza con un rango solicitado cuando cumple las tres condiciones:

1. Su estado no es `Cancelada`.
2. Su `FechaIngreso` es menor que la salida solicitada.
3. Su `FechaSalida` es mayor que el ingreso solicitado.

La disponibilidad se resuelve en el repositorio con una sola consulta LINQ (`!Any`).

**Validaciones al crear una reserva**

- La salida debe ser posterior al ingreso.
- El ingreso no puede ser anterior a hoy.
- La habitación no puede estar reservada en ese rango.

Toda reserva nueva se guarda con `Estado = "Pendiente"`.

---

## Decisiones de arquitectura

- **Database First.** La base de datos define el modelo y el código se adapta a ella.
- **Capa de repositorios.** Todas las consultas LINQ y los `SaveChangesAsync` viven en `HabitacionRepositorio` y `ReservaRepositorio`. Los controladores no usan `HotelContext` directamente.
- **Inyección de dependencias.** Contexto y repositorios se registran en `Program.cs` y se inyectan por constructor.
- **Asincronía.** Todos los métodos de repositorios y acciones son `async` (`ToListAsync`, `AnyAsync`, `SaveChangesAsync`).
- **Lecturas sin seguimiento.** Las consultas de lectura usan `AsNoTracking()`.
- **Sin SQL manual.** Todo se resuelve con LINQ.
- **Post/Redirect/Get.** Cada acción de escritura valida en el POST con `ModelState.IsValid`, guarda y redirige. El resultado se informa con `TempData`.
- **ViewModels.** Los datos calculados (noches, total, disponibilidad) viven en `ViewModels` para no tocar `Models`.

---

## Cambio de esquema: columna Observaciones

Al finalizar se ejecutó en SQL Server:

```sql
ALTER TABLE Reserva ADD Observaciones NVARCHAR(200) NULL;
```

Después se regeneró el modelo con `--force`, se agregó `Observaciones` al `Bind` de la acción `Create` en `ReservasController` y se incorporó el campo al formulario `Views/Reservas/Create.cshtml`.

---

## Diseño visual

Interfaz sobria, pensada para una aplicación de reservas hoteleras.

| Elemento   | Valor                      |
| ---------- | -------------------------- |
| Color base | Azul noche `#16202C`       |
| Acento     | Dorado champagne `#B08D57` |
| Fondo      | Marfil `#F6F3EC`           |
| Títulos    | Cormorant Garamond (serif) |
| Texto      | Inter                      |

Incluye página de inicio con sección principal, menú de navegación con indicador de página activa, tablas y formularios renovados, y etiquetas de color para el estado de cada reserva. Los estilos están en `wwwroot/css/site.css`.

---

## Cómo ejecutar

```bash
# 1. Crear la base de datos
sqlcmd -S DESKTOP-664EG2H\SQLEXPRESS -E -C -i HotelDB.sql

# 2. Restaurar paquetes y ejecutar
cd Hotel.Web
dotnet restore
dotnet run
```

Abrir en el navegador la URL que muestre la consola. Si SQL Server rechaza el certificado, verificar que la cadena de conexión incluya `TrustServerCertificate=True`.
