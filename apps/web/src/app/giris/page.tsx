import { LoginForm } from "@/components/auth/login-form";

export const metadata = {
  title: "Giriş — Factory ERP",
};

export default function LoginPage() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-surface-50 px-4 py-10">
      <section className="w-full max-w-md rounded-2xl border border-surface-200 bg-white p-8 shadow-subtle">
        <p className="mb-2 text-sm font-semibold tracking-[0.16em] text-teal-600 uppercase">
          Factory ERP
        </p>
        <h1 className="text-2xl font-bold text-navy-950">Giriş yap</h1>
        <p className="mt-2 mb-6 text-sm text-slate-600">
          Şirket içi operasyon ekranlarına erişmek için hesabınızla oturum açın.
        </p>
        <LoginForm />
      </section>
    </main>
  );
}
