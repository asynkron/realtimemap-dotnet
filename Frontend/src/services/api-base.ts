import axios, { AxiosInstance } from "axios";
import config from "@/config";

const apiInstance: Readonly<AxiosInstance> = axios.create({
    baseURL: config.backendUrl + "/api",
    timeout: 5000
  });

apiInstance.defaults.headers.get.Accepts = "application/json";
apiInstance.defaults.headers.common["Access-Control-Allow-Origin"] = "*";

export default apiInstance;
