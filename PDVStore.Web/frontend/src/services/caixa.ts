import { api, queryString } from './api';
import type { Caixa, PagedResult } from '../types/api';

export const caixaService = {
  aberto: () => api.get<Caixa | undefined>('/api/caixa/aberto'),
  abrir: (valorInicial: number) => api.post<Caixa>('/api/caixa/abrir', { valorInicial }),
  sangria: (id: number, valor: number) => api.post<Caixa>(`/api/caixa/${id}/sangria`, { valor }),
  fechar: (id: number) => api.post<Caixa>(`/api/caixa/${id}/fechar`),
  listar: (page = 1, pageSize = 20) =>
    api.get<PagedResult<Caixa>>(`/api/caixa${queryString({ page, pageSize })}`),
};
