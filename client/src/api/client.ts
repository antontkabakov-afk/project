const API_URL = normalizeApiBaseUrl(import.meta.env.VITE_API_BASE_URL || "/api");

export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

export async function apiRequest<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  return sendRequest<T>(path, init, true);
}

export function getApiErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.message) {
      return error.message;
    }

    if (error.status === 401) {
      return "Your session has expired. Please log in again.";
    }
  }

  if (error instanceof Error && error.message) {
    return error.message;
  }

  return "Something went wrong.";
}

async function sendRequest<T>(
  path: string,
  init: RequestInit,
  canRetryAfterRefresh: boolean,
): Promise<T> {
  const headers = new Headers(init.headers);

  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(buildApiUrl(path), {
    ...init,
    credentials: "include",
    headers,
  });

  if (
    response.status === 401 &&
    canRetryAfterRefresh &&
    shouldAttemptRefresh(path) &&
    (await tryRefresh())
  ) {
    return sendRequest<T>(path, init, false);
  }

  if (!response.ok) {
    const message = await readErrorMessage(response);
    throw new ApiError(message, response.status);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const contentLength = response.headers.get("Content-Length");

  if (contentLength === "0") {
    return undefined as T;
  }

  const responseText = await response.text();

  if (!responseText.trim()) {
    return undefined as T;
  }

  return JSON.parse(responseText) as T;
}

function buildApiUrl(path: string): string {
  return `${API_URL}${normalizePath(path)}`;
}

function normalizeApiBaseUrl(value: string): string {
  const normalizedValue = value.trim();

  if (!normalizedValue) {
    return "/api";
  }

  return normalizedValue.endsWith("/")
    ? normalizedValue.slice(0, -1)
    : normalizedValue;
}

function normalizePath(path: string): string {
  return path.startsWith("/") ? path : `/${path}`;
}

function shouldAttemptRefresh(path: string): boolean {
  return !normalizePath(path).startsWith("/auth/");
}

async function tryRefresh(): Promise<boolean> {
  try {
    const response = await fetch(buildApiUrl("/auth/refresh"), {
      credentials: "include",
      method: "POST",
    });

    return response.ok;
  } catch {
    return false;
  }
}

async function readErrorMessage(response: Response): Promise<string> {
  const contentType = response.headers.get("Content-Type") || "";

  if (contentType.includes("application/json")) {
    const payload = await response.json().catch(() => null);

    if (payload && typeof payload === "object") {
      const detail =
        "detail" in payload && typeof payload.detail === "string"
          ? payload.detail
          : null;
      const title =
        "title" in payload && typeof payload.title === "string"
          ? payload.title
          : null;
      const message =
        "message" in payload && typeof payload.message === "string"
          ? payload.message
          : null;

      return detail || message || title || `Request failed with status ${response.status}.`;
    }
  }

  const text = await response.text();
  return text || `Request failed with status ${response.status}.`;
}
