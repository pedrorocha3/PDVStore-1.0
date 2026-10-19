import { api, queryString } from './api';
import type { Fornecedor, FornecedorPayload, PagedResult } from '../types/api';

export interface FornecedoresFiltro {
  busca?: string;
  ativo?: boolean;
  page?: number;
  pageSize?: number;
}

export const fornecedoresService = {
  listar: (filtro: FornecedoresFiltro = {}) =>
    api.get<PagedResult<Fornecedor>>(`/api/fornecedores${queryString({ ...filtro })}`),
  criar: (payload: FornecedorPayload) => api.post<Fornecedor>('/api/fornecedores', payload),
  atualizar: (id: number, payload: FornecedorPayload) =>
    api.put<Fornecedor>(`/api/fornecedores/${id}`, payload),
  status: (id: number, ativo: boolean) =>
    api.patch<Fornecedor>(`/api/fornecedores/${id}/status`, { ativo }),
};
