const mapboxAccessToken = "<your access token>";

export interface Configuration {
  backendUrl: string;
  mapboxToken: string;
}

function getRuntimeConfig(): object {
  // eslint-disable-next-line
  const runtimeConfig = (window as any).realtimeMapConfig;

  if(typeof runtimeConfig === "object") {

    // remove not substituted config properties
    for (const key in runtimeConfig) {
      const val = runtimeConfig[key];

      if (typeof val === "string" && val.startsWith("VUE_APP_"))
        delete runtimeConfig[key];
    }

    return runtimeConfig;
  }

  return {};
}

const config = Object.freeze({
  backendUrl: process.env.VUE_APP_API_URL || "http://localhost:5000",
  mapboxToken: mapboxAccessToken,
  ...getRuntimeConfig()
});

if (config.mapboxToken as string === "<your access token>") {
  alert("Mapbox access token not set. Please set it in src/config.ts!")
  throw new Error("Mapbox access token not set.");
}

export default config as Configuration;
