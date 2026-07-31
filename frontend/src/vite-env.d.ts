/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_URL: string;
  readonly VITE_VAPID_PUBLIC_KEY?: string;
  readonly VITE_COLLAR_WHATSAPP_NUMBER?: string;
  readonly VITE_SINPE_PHONE?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
