import type {
  AllocatedBlockDto,
  BatchResultDto,
  ErrorResponse,
  RangeResponse,
  UtilizationResponse,
} from "./types";

const BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5179";

export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let res: Response;
  try {
    res = await fetch(`${BASE_URL}${path}`, {
      ...init,
      headers: { "Content-Type": "application/json", ...init?.headers },
    });
  } catch {
    throw new ApiError(
      "Web API'ye ulaşılamadı. Sunucunun çalıştığından emin olun.",
      0
    );
  }

  if (!res.ok) {
    let message = `İstek başarısız oldu (${res.status})`;
    try {
      const body = (await res.json()) as ErrorResponse;
      if (body?.message) message = body.message;
    } catch {
      // gövde JSON değilse varsayılan mesaj kullanılır
    }
    throw new ApiError(message, res.status);
  }

  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

export async function getSession(): Promise<RangeResponse | null> {
  try {
    return await request<RangeResponse>("/api/session");
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) return null;
    throw err;
  }
}

export function initSession(
  minFreqGHz: number,
  maxFreqGHz: number
): Promise<RangeResponse> {
  return request<RangeResponse>("/api/session", {
    method: "POST",
    body: JSON.stringify({ minFreqGHz, maxFreqGHz }),
  });
}

export function allocate(bandwidthMHz: number): Promise<AllocatedBlockDto> {
  return request<AllocatedBlockDto>("/api/allocations", {
    method: "POST",
    body: JSON.stringify({ bandwidthMHz }),
  });
}

export function allocateBatch(
  bandwidthsMHz: number[]
): Promise<BatchResultDto> {
  return request<BatchResultDto>("/api/allocations/batch", {
    method: "POST",
    body: JSON.stringify({ bandwidthsMHz }),
  });
}

export function getAllocations(): Promise<AllocatedBlockDto[]> {
  return request<AllocatedBlockDto[]>("/api/allocations");
}

export function deallocate(id: string): Promise<void> {
  return request<void>(`/api/allocations/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

export function getUtilization(): Promise<UtilizationResponse> {
  return request<UtilizationResponse>("/api/utilization");
}
