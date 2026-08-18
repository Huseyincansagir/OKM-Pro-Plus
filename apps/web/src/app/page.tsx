export default function HomePage() {
  return (
    <main className="min-h-screen bg-surface-50 px-6 py-16 text-navy-950 sm:px-10">
      <section className="mx-auto flex min-h-[480px] max-w-4xl flex-col justify-center rounded-2xl border border-surface-200 bg-white p-8 shadow-subtle sm:p-12">
        <p className="mb-4 text-sm font-semibold uppercase tracking-[0.18em] text-teal-600">
          Factory ERP
        </p>
        <h1 className="max-w-2xl text-3xl font-bold tracking-tight sm:text-5xl">
          Web uygulaması altyapısı hazır.
        </h1>
        <p className="mt-5 max-w-2xl text-base leading-7 text-slate-600 sm:text-lg">
          Bu sayfa WEB SLICE 001 kapsamında oluşturulan Next.js App Router
          scaffold’ının çalıştığını gösterir. İşletme ekranları sonraki slice’larda
          eklenecektir.
        </p>
        <div className="mt-8 inline-flex w-fit items-center gap-2 rounded-full bg-success-100 px-4 py-2 text-sm font-medium text-success-500">
          <span aria-hidden="true" className="h-2 w-2 rounded-full bg-success-500" />
          Scaffold çalışıyor
        </div>
      </section>
    </main>
  );
}
