import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Factory ERP",
  description: "Factory ERP web uygulaması scaffold'ı",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="tr">
      <body>{children}</body>
    </html>
  );
}
