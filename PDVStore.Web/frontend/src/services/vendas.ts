import { api, queryString } from './api';
import type { Caixa, PagedResult, Venda, VendaPayload } from '../types/api';

export interface VendasFiltro {
  inicio?: string;
  fim?: string;
  caixaId?: number;
  status?: string;
  page?: number;
  pageSize?: number;
}

export const vendasService = {
  registrar: (payload: VendaPayload) => api.post<Venda>('/api/vendas', payload),
  listar: (filtro: VendasFiltro = {}) =>
    api.get<PagedResult<Venda>>(`/api/vendas${queryString({ ...filtro })}`),
  obter: (id: number) => api.get<Venda>(`/api/vendas/${id}`),
  cancelar: (id: number, motivo?: string) =>
    api.post<Venda>(`/api/vendas/${id}/cancelar`, { motivo: motivo ?? '' }),
  resumo: (inicio?: string, fim?: string) =>
    api.get<{ quantidade: number; total: number; porFormaPagamento: { formaPagamento: string; quantidade: number; total: number }[] }>(
      `/api/vendas/resumo${queryString({ inicio, fim })}`,
    ),
};

export type { Caixa };
