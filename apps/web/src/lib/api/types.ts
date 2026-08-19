export type SessionUser = {
  id: string;
  userName: string;
  displayName: string;
  roles: string[];
  permissions: string[];
};

export type ClientSession = {
  user: SessionUser;
  accessTokenExpiresAt?: string;
};

export type BackendAuthTokens = {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: SessionUser & { rowVersion?: number };
};

export type MeResponse = {
  user: SessionUser;
  company?: { code: string; name: string };
  permissionVersion?: string;
};

export type ApiErrorKind =
  | "bad_request"
  | "unauthenticated"
  | "permission_denied"
  | "not_found"
  | "conflict"
  | "business_rule"
  | "unexpected"
  | "network";

export type NormalizedApiError = {
  kind: ApiErrorKind;
  status: number;
  code?: string;
  title?: string;
  detail?: string;
  requestId?: string;
  correlationId?: string;
  retryable?: boolean;
  errors?: unknown;
  actions?: unknown;
};

export class ApiError extends Error {
  readonly kind: ApiErrorKind;
  readonly status: number;
  readonly code?: string;
  readonly title?: string;
  readonly detail?: string;
  readonly requestId?: string;
  readonly correlationId?: string;
  readonly retryable?: boolean;
  readonly errors?: unknown;
  readonly actions?: unknown;

  constructor(normalized: NormalizedApiError) {
    super(normalized.detail ?? normalized.title ?? "İstek başarısız oldu");
    this.name = "ApiError";
    this.kind = normalized.kind;
    this.status = normalized.status;
    this.code = normalized.code;
    this.title = normalized.title;
    this.detail = normalized.detail;
    this.requestId = normalized.requestId;
    this.correlationId = normalized.correlationId;
    this.retryable = normalized.retryable;
    this.errors = normalized.errors;
    this.actions = normalized.actions;
  }
}

export type ApiRequestOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  path: string;
  body?: unknown;
  auth?: boolean;
  idempotent?: boolean;
  idempotencyKey?: string;
  ifMatch?: string;
  skipRefresh?: boolean;
};
