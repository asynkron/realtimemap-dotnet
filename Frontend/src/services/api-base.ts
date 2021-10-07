import axios, { AxiosInstance } from "axios";

const apiInstance: Readonly<AxiosInstance> = axios.create({
    baseURL: process.env.VUE_APP_API_URL || "http://localhost:5000/api",
    timeout: 5000
  });

apiInstance.defaults.headers.get.Accepts = "application/json";
apiInstance.defaults.headers.common["Access-Control-Allow-Origin"] = "*";

export default apiInstance;
