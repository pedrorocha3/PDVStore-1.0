import { api, queryString } from './api';
import type { PagedResult, TipoPermissao, Usuario } from '../types/api';

export interface UsuarioPayloadApi {
  nome: string;
  senha?: string;
  permissao: TipoPermissao;
  ativo?: boolean;
  fotoPath?: string | null;
}

export const usuariosService = {
  listar: (ativo?: boolean, page = 1, pageSize = 50) =>
    api.get<PagedResult<Usuario>>(`/api/usuarios${queryString({ ativo, page, pageSize })}`),
  criar: (payload: UsuarioPayloadApi) => api.post<Usuario>('/api/usuarios', payload),
  atualizar: (id: number, payload: UsuarioPayloadApi) =>
    api.put<Usuario>(`/api/usuarios/${id}`, payload),
  desativar: (id: number) => api.delete(`/api/usuarios/${id}`),
  alterarSenha: (id: number, senhaAtual: string, novaSenha: string) =>
    api.post<void>(`/api/usuarios/${id}/alterar-senha`, { senhaAtual, novaSenha }),
};
