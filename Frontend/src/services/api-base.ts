import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse } from "axios";

const instance: Readonly<AxiosInstance> = axios.create({
    baseURL: process.env.VUE_APP_API_URL || "http://localhost:5000/api",
    timeout: 5000
  });

instance.defaults.headers.get.Accepts = "application/json";
instance.defaults.headers.common["Access-Control-Allow-Origin"] = "*";

export default instance;