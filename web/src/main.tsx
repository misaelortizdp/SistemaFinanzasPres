import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import App from "./App";
import { ProveedorAutenticacion } from "./autenticacion/ContextoAutenticacion";
import { Toaster } from "./components/ui/toast";
import { esErrorRecuperable } from "./lib/errorUtils";
import "./index.css";

const clienteConsultas = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: (failureCount, error) => {
        // No reintentar si el error no es recuperable
        if (!esErrorRecuperable(error)) return false;
        // Máximo 3 reintentos
        return failureCount < 3;
      },
      retryDelay: (attemptIndex) => {
        // Backoff exponencial: 1s, 2s, 4s
        return Math.min(1000 * 2 ** attemptIndex, 30000);
      },
      staleTime: 30000, // Considerar datos frescos por 30 segundos
    },
    mutations: {
      retry: (failureCount, error) => {
        // Solo reintentar mutaciones si el error es de red
        if (!esErrorRecuperable(error)) return false;
        return failureCount < 2; // Máximo 2 reintentos para mutaciones
      },
      retryDelay: 1000,
    },
  },
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
