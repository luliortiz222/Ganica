import { sincronizar_cola } from "./offline_sync_service";

export function iniciar_offline_service(): void {

  console.log(
    `Estado inicial de conexión: ${
      navigator.onLine ? "ONLINE" : "OFFLINE"
    }`
  );

  window.addEventListener("online", async () => {

    console.log("📶 Internet disponible nuevamente.");

    try {

      await sincronizar_cola();

    } catch (error) {

      console.error(
        "Error durante la sincronización:",
        error
      );

    }
  });

  // También intentamos sincronizar al iniciar la aplicación.
  if (navigator.onLine) {

    console.log(
      "📶 Aplicación iniciada con conexión. Revisando operaciones pendientes..."
    );

    sincronizar_cola().catch((error) => {

      console.error(
        "Error al sincronizar al iniciar:",
        error
      );

    });
  }
}