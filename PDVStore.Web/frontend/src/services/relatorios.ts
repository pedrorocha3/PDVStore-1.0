import { api, queryString } from './api';
import type { Dashboard, ItemRelatorio, Produto } from '../types/api';

export const relatoriosService = {
  dashboard: (inicio?: string, fim?: string) =>
    api.get<Dashboard>(`/api/relatorios/dashboard${queryString({ inicio, fim })}`),
  itensMaisVendidos: (inicio?: string, fim?: string, topN = 10) =>
    api.get<ItemRelatorio[]>(`/api/relatorios/itens-mais-vendidos${queryString({ inicio, fim, topN })}`),
  estoqueMinimo: () => api.get<Produto[]>('/api/relatorios/estoque-minimo'),
};
