import mapboxConfig from '@/mapboxConfig';

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

export default Object.freeze({
  backendUrl: process.env.VUE_APP_API_URL || "http://localhost:5000",
  mapboxToken: mapboxConfig.getAccessToken(),
  ...getRuntimeConfig()
}) as Configuration;
