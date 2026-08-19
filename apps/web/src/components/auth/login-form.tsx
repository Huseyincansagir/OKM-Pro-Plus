"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { login, userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";

const loginSchema = z.object({
  userName: z.string().trim().min(1, "Kullanıcı adı zorunludur."),
  password: z.string().min(1, "Parola zorunludur."),
});

type LoginValues = z.infer<typeof loginSchema>;

export function LoginForm() {
  const router = useRouter();
  const status = useSessionStore((state) => state.status);
  const [serverError, setServerError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginValues>({
    defaultValues: { userName: "", password: "" },
  });

  useEffect(() => {
    if (status === "authenticated") {
      router.replace("/dashboard");
    }
  }, [router, status]);

  async function onSubmit(values: LoginValues) {
    setServerError(null);
    const parsed = loginSchema.safeParse(values);
    if (!parsed.success) {
      setServerError(parsed.error.issues[0]?.message ?? "Geçersiz form.");
      return;
    }

    try {
      await login(parsed.data.userName, parsed.data.password);
      router.replace("/dashboard");
    } catch (error) {
      setServerError(userFacingMessage(error));
    }
  }

  return (
    <form className="space-y-4" onSubmit={handleSubmit(onSubmit)} noValidate>
      {serverError ? (
        <Alert tone="danger" title="Giriş yapılamadı">
          {serverError}
        </Alert>
      ) : null}
      <Input
        label="Kullanıcı adı"
        autoComplete="username"
        required
        error={errors.userName?.message}
        {...register("userName", { required: "Kullanıcı adı zorunludur." })}
      />
      <Input
        label="Parola"
        type="password"
        autoComplete="current-password"
        required
        error={errors.password?.message}
        {...register("password", { required: "Parola zorunludur." })}
      />
      <Button type="submit" className="w-full" loading={isSubmitting}>
        Giriş yap
      </Button>
    </form>
  );
}
