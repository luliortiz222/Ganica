using GanicaApi.Models;

namespace GanicaApi.Data;

public static class DbInitializer
{
    public static void Seed(ApplicationDbContext context)
    {
        // Borramos todo lo viejo para asegurar que queden solo los 3 correctos
        if (context.ServiciosRecoleccion.Any())
        {
            context.ServiciosRecoleccion.RemoveRange(context.ServiciosRecoleccion);
            context.SaveChanges();
        }

        var servicios = new ServicioRecoleccion[]
        {
            new ServicioRecoleccion
            {
                Nombre = "Residuos Domiciliarios",
                Descripcion = "Sacar los residuos en bolsas cerradas y en el cesto de tu domicilio.",
                Dias = "Lunes a Viernes",
                Horario = "18:00 a 22:00 hs",
                RequiereSolicitud = false
            },
            new ServicioRecoleccion
            {
                Nombre = "Recolección Diferenciada (Reciclables)",
                Descripcion = "Se retira plástico, cartón, vidrio y metal secos y limpios.",
                Dias = "Martes y Jueves",
                Horario = "08:00 a 12:00 hs",
                RequiereSolicitud = false
            },
            new ServicioRecoleccion
            {
                Nombre = "Residuos Voluminosos y Poda",
                Descripcion = "Previa solicitud por la app. Retiro de restos de poda, escombros en bolsas y electrodomésticos en desuso.",
                Dias = "Previa solicitud",
                Horario = "A coordinar",
                RequiereSolicitud = true
            }
        };

        context.ServiciosRecoleccion.AddRange(servicios);
        context.SaveChanges();
    }
}