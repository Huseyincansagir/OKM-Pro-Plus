import type { Metadata } from "next";
import { ToastProvider } from "@/components/ui/toast";
import "./globals.css";

export const metadata: Metadata = {
  title: "Factory ERP",
  description: "Factory ERP web uygulaması",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="tr">
      <body>
        <ToastProvider>{children}</ToastProvider>
      </body>
    </html>
  );
}
