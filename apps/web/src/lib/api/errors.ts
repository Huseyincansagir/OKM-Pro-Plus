import { ApiError, type ApiErrorKind, type NormalizedApiError } from "@/lib/api/types";

function kindFromStatus(status: number): ApiErrorKind {
  if (status === 400) return "bad_request";
  if (status === 401) return "unauthenticated";
  if (status === 403) return "permission_denied";
  if (status === 404) return "not_found";
  if (status === 409) return "conflict";
  if (status === 422) return "business_rule";
  if (status === 0) return "network";
  return "unexpected";
}

function readExtension(body: Record<string, unknown>, key: string): unknown {
  if (key in body) {
    return body[key];
  }
  const extensions = body.extensions;
  if (extensions && typeof extensions === "object") {
    return (extensions as Record<string, unknown>)[key];
  }
  return undefined;
}

export function normalizeApiError(
  status: number,
  body: unknown,
  fallbackDetail?: string,
): NormalizedApiError {
  const record = body && typeof body === "object" ? (body as Record<string, unknown>) : {};
  const code = readExtension(record, "code");
  const requestId = readExtension(record, "requestId");
  const correlationId = readExtension(record, "correlationId");
  const retryable = readExtension(record, "retryable");

  return {
    kind: kindFromStatus(status),
    status,
    code: typeof code === "string" ? code : undefined,
    title: typeof record.title === "string" ? record.title : undefined,
    detail:
      typeof record.detail === "string"
        ? record.detail
        : fallbackDetail ?? "İstek tamamlanamadı.",
    requestId: typeof requestId === "string" ? requestId : undefined,
    correlationId: typeof correlationId === "string" ? correlationId : undefined,
    retryable: typeof retryable === "boolean" ? retryable : undefined,
    errors: readExtension(record, "errors"),
    actions: readExtension(record, "actions"),
  };
}

export function toApiError(
  status: number,
  body: unknown,
  fallbackDetail?: string,
): ApiError {
  return new ApiError(normalizeApiError(status, body, fallbackDetail));
}

export function userFacingMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.kind === "unauthenticated") {
      return error.detail ?? "Kullanıcı adı veya parola geçersiz.";
    }
    if (error.kind === "permission_denied") {
      return "Bu işlem için yetkiniz yok.";
    }
    return error.detail ?? error.title ?? "İstek başarısız oldu.";
  }
  if (error instanceof Error) {
    return error.message;
  }
  return "İstek başarısız oldu.";
}
