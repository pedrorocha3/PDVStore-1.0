import { api } from './api';
import type { LoginResponse, Usuario } from '../types/api';

export const authService = {
  login: (nome: string, senha: string) =>
    api.post<LoginResponse>('/api/auth/login', { nome, senha }),
  me: () => api.get<Usuario>('/api/auth/me'),
};
