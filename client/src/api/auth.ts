import { ApiError, apiRequest } from "./client";

export interface AuthResponse {
  isSuccess: boolean;
  email: string;
  username: string;
}

export async function login(
  email: string,
  password: string,
): Promise<AuthResponse> {
  return apiRequest<AuthResponse>("/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });
}

export async function register(
  email: string,
  password: string,
  username: string,
): Promise<AuthResponse> {
  return apiRequest<AuthResponse>("/auth/register", {
    method: "POST",
    body: JSON.stringify({ email, password, username }),
  });
}

export async function logout() {
  await apiRequest<void>("/auth/logout", {
    method: "POST",
  });

  return true;
}

export async function refresh(): Promise<AuthResponse | null> {
  try {
    return await apiRequest<AuthResponse>("/auth/refresh", {
      method: "POST",
    });
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      return null;
    }

    throw error;
  }
}
