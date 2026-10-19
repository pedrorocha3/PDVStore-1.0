const API_BASE = (import.meta.env.VITE_API_URL as string | undefined)?.replace(/\/$/, '') ?? '';

const TOKEN_KEY = 'pdvstore.web.token';
const USER_KEY = 'pdvstore.web.usuario';

export class ApiError extends Error {
  constructor(message: string, public readonly status: number, public readonly details?: string[]) {
    super(message);
    this.name = 'ApiError';
  }
}

let unauthorizedHandler: (() => void) | null = null;

export function setUnauthorizedHandler(handler: (() => void) | null) {
  unauthorizedHandler = handler;
}

export const session = {
  token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  },
  usuario<T>(): T | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as T;
    } catch {
      return null;
    }
  },
  salvar(token: string, usuario: unknown) {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(USER_KEY, JSON.stringify(usuario));
  },
  limpar() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  },
};

function mensagemDoErro(status: number, body: unknown): { mensagem: string; detalhes?: string[] } {
  if (body && typeof body === 'object') {
    const problema = body as Record<string, unknown>;
    const detalhes: string[] = [];
    if (problema.errors && typeof problema.errors === 'object') {
      for (const valor of Object.values(problema.errors as Record<string, unknown>)) {
        if (Array.isArray(valor)) detalhes.push(...valor.map(String));
      }
    }
    const mensagem =
      (typeof problema.detail === 'string' && problema.detail) ||
      (typeof problema.mensagem === 'string' && problema.mensagem) ||
      (typeof problema.title === 'string' && problema.title) ||
      'Não foi possível concluir a operação.';
    return { mensagem, detalhes: detalhes.length ? detalhes : undefined };
  }
  return { mensagem: status === 0 ? 'API indisponível. Verifique se o back-end está em execução.' : 'Erro inesperado.' };
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers);
  const token = session.token();

  if (token) headers.set('Authorization', `Bearer ${token}`);
  if (options.body !== undefined && !(options.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json');
  }

  let response: Response;
  try {
    response = await fetch(`${API_BASE}${path}`, { ...options, headers });
  } catch {
    throw new ApiError('API indisponível. Verifique se o back-end está em execução.', 0);
  }

  if (response.status === 401 && !path.includes('/api/auth/login')) {
    session.limpar();
    unauthorizedHandler?.();
  }

  if (response.status === 204) return undefined as T;

  const texto = await response.text();
  let body: unknown = null;
  if (texto) {
    try {
      body = JSON.parse(texto);
    } catch {
      body = texto;
    }
  }

  if (!response.ok) {
    const { mensagem, detalhes } = mensagemDoErro(response.status, body);
    throw new ApiError(mensagem, response.status, detalhes);
  }

  return body as T;
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'POST', body: body === undefined ? undefined : JSON.stringify(body) }),
  put: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'PUT', body: JSON.stringify(body) }),
  patch: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'PATCH', body: JSON.stringify(body) }),
  delete: <T = void>(path: string) => request<T>(path, { method: 'DELETE' }),
};

export function queryString(params: Record<string, string | number | boolean | null | undefined>): string {
  const search = new URLSearchParams();
  for (const [chave, valor] of Object.entries(params)) {
    if (valor === undefined || valor === null || valor === '') continue;
    search.set(chave, String(valor));
  }
  const texto = search.toString();
  return texto ? `?${texto}` : '';
}
