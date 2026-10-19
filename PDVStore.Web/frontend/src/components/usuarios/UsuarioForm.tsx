import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alerta, Botao, Campo, Entrada, Modal, Selecao } from '../ui';
import { ApiError } from '../../services/api';
import { usuariosService } from '../../services/usuarios';
import { useToast } from '../../hooks/useToast';
import type { TipoPermissao, Usuario } from '../../types/api';

export function UsuarioForm({ usuario, onFechar, onSalvo }: {
  usuario: Usuario | null;
  onFechar: () => void;
  onSalvo: () => void;
}) {
  const { sucesso } = useToast();
  const [nome, setNome] = useState(usuario?.nome ?? '');
  const [senha, setSenha] = useState('');
  const [permissao, setPermissao] = useState<TipoPermissao>(usuario?.permissao ?? 'Operador');
  const [ativo, setAtivo] = useState(usuario?.ativo ?? true);
  const [erro, setErro] = useState('');
  const [salvando, setSalvando] = useState(false);

  async function salvar(evento: FormEvent) {
    evento.preventDefault();
    if (!nome.trim()) { setErro('Nome é obrigatório.'); return; }
    if (!usuario && senha.length < 6) { setErro('A senha inicial deve ter pelo menos 6 caracteres.'); return; }

    setSalvando(true);
    setErro('');
    try {
      if (usuario) {
        await usuariosService.atualizar(usuario.id, { nome: nome.trim(), permissao, ativo });
        sucesso('Usuário atualizado.');
      } else {
        await usuariosService.criar({ nome: nome.trim(), senha, permissao });
        sucesso('Usuário cadastrado.');
      }
      onSalvo();
    } catch (e) {
      setErro(e instanceof ApiError ? [e.message, ...(e.details ?? [])].join(' ') : 'Erro ao salvar usuário.');
    } finally {
      setSalvando(false);
    }
  }

  return (
    <Modal titulo={usuario ? `Editar usuário #${usuario.id}` : 'Novo usuário'} onFechar={onFechar}>
      <form className="formulario" onSubmit={salvar}>
        {erro && <Alerta tipo="erro">{erro}</Alerta>}

        <Campo label="Nome de acesso" obrigatorio>
          <Entrada value={nome} onChange={(e) => setNome(e.target.value)} maxLength={100} autoFocus />
        </Campo>

        {!usuario && (
          <Campo label="Senha inicial" obrigatorio dica="Mínimo de 6 caracteres. Será armazenada como hash BCrypt.">
            <Entrada type="password" value={senha} onChange={(e) => setSenha(e.target.value)} maxLength={100} />
          </Campo>
        )}

        <Campo label="Perfil de acesso" obrigatorio>
          <Selecao value={permissao} onChange={(e) => setPermissao(e.target.value as TipoPermissao)}>
            <option value="Operador">Operador (Caixa) — somente PDV</option>
            <option value="Estoquista">Estoquista — somente produtos</option>
            <option value="Administrador">Administrador — acesso total</option>
          </Selecao>
        </Campo>

        {usuario && (
          <label className="campo-check">
            <input type="checkbox" checked={ativo} onChange={(e) => setAtivo(e.target.checked)} />
            <span>Usuário ativo</span>
          </label>
        )}

        <div className="modal-acoes">
          <Botao variante="secundario" type="button" onClick={onFechar}>Cancelar</Botao>
          <Botao type="submit" carregando={salvando}>{usuario ? 'Salvar alterações' : 'Cadastrar usuário'}</Botao>
        </div>
      </form>
    </Modal>
  );
}
