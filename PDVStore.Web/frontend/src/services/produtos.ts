import { api, queryString } from './api';
import type { PagedResult, Produto, ProdutoPayload } from '../types/api';

export interface ProdutosFiltro {
  busca?: string;
  categoria?: string;
  ativo?: boolean;
  page?: number;
  pageSize?: number;
  ordenarPor?: string;
  descendente?: boolean;
}

export const produtosService = {
  listar: (filtro: ProdutosFiltro = {}) =>
    api.get<PagedResult<Produto>>(`/api/produtos${queryString({ ...filtro })}`),
  obter: (id: number) => api.get<Produto>(`/api/produtos/${id}`),
  porCodigo: (codigo: string) =>
    api.get<Produto>(`/api/produtos/codigo-barras/${encodeURIComponent(codigo)}`),
  criar: (payload: ProdutoPayload) => api.post<Produto>('/api/produtos', payload),
  atualizar: (id: number, payload: ProdutoPayload) => api.put<Produto>(`/api/produtos/${id}`, payload),
  desativar: (id: number) => api.delete(`/api/produtos/${id}`),
};
