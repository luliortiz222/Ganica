
import { Capacitor } from "@capacitor/core";
import { SpeechRecognition } from "@capgo/capacitor-speech-recognition";

interface ResultadoVoz {
  matches?: string[];
}

interface ReconocimientoNavegador {
  lang: string;
  interimResults: boolean;
  maxAlternatives: number;
  onresult: ((event: any) => void) | null;
  onerror: ((event: any) => void) | null;
  onend: (() => void) | null;
  start: () => void;
  stop: () => void;
}

interface ConstructorReconocimiento {
  new (): ReconocimientoNavegador;
}

function obtener_reconocimiento_navegador():
  | ConstructorReconocimiento
  | null {
  const ventana = window as any;

  return (
    ventana.SpeechRecognition ??
    ventana.webkitSpeechRecognition ??
    null
  );
}

function dictar_en_navegador(): Promise<string> {
  return new Promise((resolve, reject) => {
    const Constructor = obtener_reconocimiento_navegador();

    if (!Constructor) {
      reject(
        new Error(
          "El navegador no admite reconocimiento de voz. Podés escribir las observaciones manualmente."
        )
      );
      return;
    }

    const reconocimiento = new Constructor();

    reconocimiento.lang = "es-AR";
    reconocimiento.interimResults = false;
    reconocimiento.maxAlternatives = 1;

    let finalizado = false;

    reconocimiento.onresult = (event: any) => {
      const texto = event.results?.[0]?.[0]?.transcript?.trim();

      if (texto) {
        finalizado = true;
        resolve(texto);
      }
    };

    reconocimiento.onerror = (event: any) => {
      if (finalizado) return;

      finalizado = true;

      if (event.error === "not-allowed" || event.error === "service-not-allowed") {
        reject(
          new Error(
            "No se permitió el uso del micrófono. Revisá los permisos del navegador."
          )
        );
        return;
      }

      if (event.error === "no-speech") {
        reject(new Error("No se detectó voz. Intentá nuevamente."));
        return;
      }

      reject(
        new Error(
          "No se pudo reconocer la voz. Podés escribir el texto manualmente."
        )
      );
    };

    reconocimiento.onend = () => {
      if (!finalizado) {
        finalizado = true;
        reject(new Error("No se reconoció ningún texto. Intentá nuevamente."));
      }
    };

    try {
      reconocimiento.start();
    } catch {
      reject(new Error("No se pudo iniciar el micrófono."));
    }
  });
}

async function dictar_en_android(): Promise<string> {
  const permisos = await SpeechRecognition.requestPermissions();

  if (permisos.speechRecognition !== "granted") {
    throw new Error(
      "Necesitás conceder permiso para usar el micrófono y el reconocimiento de voz."
    );
  }

  const disponibilidad = await SpeechRecognition.available();

  if (!disponibilidad.available) {
    throw new Error(
      "El reconocimiento de voz no está disponible en este dispositivo."
    );
  }

  const resultado = (await SpeechRecognition.start({
    language: "es-AR",
    maxResults: 1,
    partialResults: false,
    popup: true
  })) as ResultadoVoz;

  const texto = resultado.matches?.[0]?.trim();

  if (!texto) {
    throw new Error("No se reconoció ningún texto. Intentá nuevamente.");
  }

  return texto;
}

export async function dictar_texto(): Promise<string> {
  if (Capacitor.isNativePlatform()) {
    return await dictar_en_android();
  }

  return await dictar_en_navegador();
}
