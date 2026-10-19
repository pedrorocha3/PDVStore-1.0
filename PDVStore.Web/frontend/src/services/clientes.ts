import { api, queryString } from './api';
import type { Cliente, ClientePayload, PagedResult } from '../types/api';

export interface ClientesFiltro {
  busca?: string;
  ativo?: boolean;
  page?: number;
  pageSize?: number;
}

export const clientesService = {
  listar: (filtro: ClientesFiltro = {}) =>
    api.get<PagedResult<Cliente>>(`/api/clientes${queryString({ ...filtro })}`),
  obter: (id: number) => api.get<Cliente>(`/api/clientes/${id}`),
  criar: (payload: ClientePayload) => api.post<Cliente>('/api/clientes', payload),
  atualizar: (id: number, payload: ClientePayload) => api.put<Cliente>(`/api/clientes/${id}`, payload),
  status: (id: number, ativo: boolean) => api.patch<Cliente>(`/api/clientes/${id}/status`, { ativo }),
  receberFiado: (id: number, valor: number) =>
    api.post<Cliente>(`/api/clientes/${id}/receber-fiado`, { valor }),
};
