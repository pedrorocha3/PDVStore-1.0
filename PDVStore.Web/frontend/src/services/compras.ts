import { api, queryString } from './api';
import type { Compra, CompraPayload, PagedResult } from '../types/api';

export interface ComprasFiltro {
  inicio?: string;
  fim?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

export const comprasService = {
  registrar: (payload: CompraPayload) => api.post<Compra>('/api/compras', payload),
  listar: (filtro: ComprasFiltro = {}) =>
    api.get<PagedResult<Compra>>(`/api/compras${queryString({ ...filtro })}`),
  obter: (id: number) => api.get<Compra>(`/api/compras/${id}`),
  cancelar: (id: number) => api.post<Compra>(`/api/compras/${id}/cancelar`),
};
