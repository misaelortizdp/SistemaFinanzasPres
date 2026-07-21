import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import App from "./App";
import { ProveedorAutenticacion } from "./autenticacion/ContextoAutenticacion";
import { Toaster } from "./components/ui/toast";
import "./index.css";

const clienteConsultas = new QueryClient({
  defaultOptions: { queries: { refetchOnWindowFocus: false, retry: 1 } },
});

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <QueryClientProvider client={clienteConsultas}>
      <BrowserRouter>
        <ProveedorAutenticacion>
          <App />
          <Toaster />
        </ProveedorAutenticacion>
      </BrowserRouter>
    </QueryClientProvider>
  </React.StrictMode>
);
