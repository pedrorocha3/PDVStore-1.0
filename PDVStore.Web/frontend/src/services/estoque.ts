import { api, queryString } from './api';
import type { MovimentacaoEstoque, PagedResult, Produto } from '../types/api';

export interface MovimentacoesFiltro {
  inicio?: string;
  fim?: string;
  produtoId?: number;
  tipo?: string;
  page?: number;
  pageSize?: number;
}

export const estoqueService = {
  entrada: (produtoId: number, quantidade: number, motivo?: string) =>
    api.post<MovimentacaoEstoque>('/api/estoque/entradas', { produtoId, quantidade, motivo }),
  saida: (produtoId: number, quantidade: number, motivo?: string) =>
    api.post<MovimentacaoEstoque>('/api/estoque/saidas', { produtoId, quantidade, motivo }),
  movimentacoes: (filtro: MovimentacoesFiltro = {}) =>
    api.get<PagedResult<MovimentacaoEstoque>>(`/api/estoque/movimentacoes${queryString({ ...filtro })}`),
  alertas: () => api.get<Produto[]>('/api/estoque/alertas'),
};
