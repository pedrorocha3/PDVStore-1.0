import { useCallback, useEffect, useState } from 'react';
import { Alerta, Botao, CabecalhoPagina, Etiqueta, Spinner, Vazio } from '../components/ui';
import { UsuarioForm } from '../components/usuarios/UsuarioForm';
import { usuariosService } from '../services/usuarios';
import { ApiError } from '../services/api';
import { useToast } from '../hooks/useToast';
import { useAuth } from '../hooks/useAuth';
import type { Usuario } from '../types/api';
import { data } from '../utils/format';

export function Usuarios() {
  const { usuario: logado } = useAuth();
  const { sucesso, erro: avisarErro } = useToast();
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');
  const [editando, setEditando] = useState<Usuario | null>(null);
  const [formAberto, setFormAberto] = useState(false);

  const carregar = useCallback(async () => {
    setCarregando(true);
    setErro('');
    try {
      const r = await usuariosService.listar(undefined, 1, 100);
      setUsuarios(r.items);
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível carregar os usuários.');
    } finally {
      setCarregando(false);
    }
  }, []);

  useEffect(() => { void carregar(); }, [carregar]);

  async function desativar(usuario: Usuario) {
    if (usuario.id === logado?.id) {
      avisarErro('Você não pode desativar o próprio usuário.');
      return;
    }
    if (!window.confirm(`Desativar o usuário "${usuario.nome}"?`)) return;
    try {
      await usuariosService.desativar(usuario.id);
      sucesso('Usuário desativado.');
      await carregar();
    } catch (e) {
      avisarErro(e instanceof ApiError ? e.message : 'Não foi possível desativar o usuário.');
    }
  }

  return (
    <>
      <CabecalhoPagina
        titulo="Usuários"
        descricao="Perfis de acesso: Administrador, Operador (Caixa) e Estoquista."
        acoes={<Botao onClick={() => { setEditando(null); setFormAberto(true); }}>Novo usuário</Botao>}
      />

      <section className="painel">
        {erro && <Alerta tipo="erro">{erro}</Alerta>}
        {carregando ? <Spinner texto="Carregando usuários..." /> : usuarios.length === 0 ? (
          <Vazio titulo="Nenhum usuário cadastrado" />
        ) : (
          <div className="tabela-scroll">
            <table className="tabela">
              <thead>
                <tr><th>Usuário</th><th>Perfil</th><th>Criado em</th><th>Status</th><th className="direita">Ações</th></tr>
              </thead>
              <tbody>
                {usuarios.map((usuario) => (
                  <tr key={usuario.id}>
                    <td>
                      <strong>{usuario.nome}</strong>
                      {usuario.id === logado?.id && <small className="bloco texto-suave">você</small>}
                    </td>
                    <td><Etiqueta tom={usuario.permissao === 'Administrador' ? 'azul' : 'neutro'}>{usuario.permissaoDescricao}</Etiqueta></td>
                    <td>{data(usuario.createdAt)}</td>
                    <td><Etiqueta tom={usuario.ativo ? 'verde' : 'vermelho'}>{usuario.ativo ? 'Ativo' : 'Inativo'}</Etiqueta></td>
                    <td className="direita acoes-tabela">
                      <Botao variante="fantasma" onClick={() => { setEditando(usuario); setFormAberto(true); }}>Editar</Botao>
                      {usuario.ativo && usuario.id !== logado?.id && (
                        <Botao variante="fantasma" className="texto-perigo" onClick={() => void desativar(usuario)}>Desativar</Botao>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {formAberto && (
        <UsuarioForm usuario={editando} onFechar={() => setFormAberto(false)}
          onSalvo={() => { setFormAberto(false); void carregar(); }} />
      )}
    </>
  );
}
