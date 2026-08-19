import { PublicShell } from "@/components/public/public-shell";
import { QuoteCheckout } from "@/components/public/quote-checkout";

export const metadata = {
  title: "Teklif sepeti — Factory ERP",
};

export default function QuoteBasketPage() {
  return (
    <PublicShell>
      <QuoteCheckout />
    </PublicShell>
  );
}
