import { useState } from 'react';
import type { FormEvent } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { useToast } from '../hooks/useToast';
import { Icone } from '../components/Icone';
import { Alerta, Botao, Campo, Entrada, Modal } from '../components/ui';
import { usuariosService } from '../services/usuarios';
import { ApiError } from '../services/api';
import type { TipoPermissao } from '../types/api';

interface ItemMenu {
  para: string;
  rotulo: string;
  icone: string;
  papeis: TipoPermissao[];
}

const ITENS: ItemMenu[] = [
  { para: '/dashboard', rotulo: 'Dashboard', icone: 'dashboard', papeis: ['Administrador'] },
  { para: '/pdv', rotulo: 'Vender (PDV)', icone: 'pdv', papeis: ['Administrador', 'Operador'] },
  { para: '/vendas', rotulo: 'Vendas', icone: 'vendas', papeis: ['Administrador'] },
  { para: '/produtos', rotulo: 'Produtos', icone: 'produtos', papeis: ['Administrador', 'Estoquista'] },
  { para: '/estoque', rotulo: 'Estoque', icone: 'estoque', papeis: ['Administrador'] },
  { para: '/clientes', rotulo: 'Clientes', icone: 'clientes', papeis: ['Administrador'] },
  { para: '/fornecedores', rotulo: 'Fornecedores', icone: 'fornecedores', papeis: ['Administrador'] },
  { para: '/compras', rotulo: 'Compras', icone: 'compras', papeis: ['Administrador'] },
  { para: '/caixa', rotulo: 'Abrir/Fechar Caixa', icone: 'caixa', papeis: ['Administrador'] },
  { para: '/usuarios', rotulo: 'Usuários', icone: 'usuarios', papeis: ['Administrador'] },
];

export function Layout() {
  const { usuario, sair } = useAuth();
  const { sucesso } = useToast();
  const [menuAberto, setMenuAberto] = useState(false);
  const [senhaAberta, setSenhaAberta] = useState(false);
  const [senhaAtual, setSenhaAtual] = useState('');
  const [novaSenha, setNovaSenha] = useState('');
  const [confirmacao, setConfirmacao] = useState('');
  const [erroSenha, setErroSenha] = useState('');
  const [salvandoSenha, setSalvandoSenha] = useState(false);
  const local = useLocation();

  async function alterarSenha(evento: FormEvent) {
    evento.preventDefault();
    setErroSenha('');
    if (!usuario) return;
    if (novaSenha.length < 6) { setErroSenha('A nova senha deve ter pelo menos 6 caracteres.'); return; }
    if (novaSenha !== confirmacao) { setErroSenha('A confirmação não confere com a nova senha.'); return; }

    setSalvandoSenha(true);
    try {
      await usuariosService.alterarSenha(usuario.id, senhaAtual, novaSenha);
      sucesso('Senha alterada com sucesso.');
      setSenhaAberta(false);
      setSenhaAtual('');
      setNovaSenha('');
      setConfirmacao('');
    } catch (e) {
      setErroSenha(e instanceof ApiError ? e.message : 'Não foi possível alterar a senha.');
    } finally {
      setSalvandoSenha(false);
    }
  }

  const tituloAtual = ITENS.find((item) => local.pathname.startsWith(item.para))?.rotulo ?? 'PDVStore';
  const itensVisiveis = ITENS.filter((item) => usuario && item.papeis.includes(usuario.permissao));

  return (
    <div className="app">
      <aside className={`sidebar ${menuAberto ? 'sidebar-aberta' : ''}`}>
        <div className="marca">
          <span className="marca-icone">PDV</span>
          <div>
            <strong>PDVStore</strong>
            <small>Web</small>
          </div>
        </div>

        <nav className="menu" onClick={() => setMenuAberto(false)}>
          {itensVisiveis.map((item) => (
            <NavLink
              key={item.para}
              to={item.para}
              className={({ isActive }) => `menu-item ${isActive ? 'menu-item-ativo' : ''}`}
            >
              <Icone nome={item.icone} />
              <span>{item.rotulo}</span>
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-rodape">
          <div className="usuario-info">
            <span className="avatar">{usuario?.nome.charAt(0).toUpperCase()}</span>
            <div>
              <strong>{usuario?.nome}</strong>
              <small>{usuario?.permissaoDescricao}</small>
            </div>
          </div>
          <button className="menu-item sair" onClick={() => setSenhaAberta(true)}>
            <Icone nome="usuarios" />
            <span>Alterar senha</span>
          </button>
          <button className="menu-item sair" onClick={sair}>
            <Icone nome="sair" />
            <span>Sair</span>
          </button>
        </div>
      </aside>

      {menuAberto && <div className="sidebar-fundo" onClick={() => setMenuAberto(false)} />}

      <div className="conteudo">
        <header className="topo">
          <button className="menu-mobile" onClick={() => setMenuAberto(true)} aria-label="Abrir menu">
            <Icone nome="menu" />
          </button>
          <span className="topo-titulo">{tituloAtual}</span>
          <span className="topo-data">{new Date().toLocaleDateString('pt-BR', { weekday: 'long', day: '2-digit', month: 'long' })}</span>
        </header>
        <main className="pagina">
          <Outlet />
        </main>
      </div>

      {senhaAberta && (
        <Modal titulo="Alterar minha senha" onFechar={() => setSenhaAberta(false)} largura="480px">
          <form className="formulario" onSubmit={alterarSenha}>
            {erroSenha && <Alerta tipo="erro">{erroSenha}</Alerta>}
            <Campo label="Senha atual" obrigatorio>
              <Entrada type="password" value={senhaAtual} onChange={(e) => setSenhaAtual(e.target.value)} autoFocus />
            </Campo>
            <Campo label="Nova senha" obrigatorio dica="Mínimo de 6 caracteres.">
              <Entrada type="password" value={novaSenha} onChange={(e) => setNovaSenha(e.target.value)} />
            </Campo>
            <Campo label="Confirmar nova senha" obrigatorio>
              <Entrada type="password" value={confirmacao} onChange={(e) => setConfirmacao(e.target.value)} />
            </Campo>
            <div className="modal-acoes">
              <Botao variante="secundario" type="button" onClick={() => setSenhaAberta(false)}>Cancelar</Botao>
              <Botao type="submit" carregando={salvandoSenha}>Alterar senha</Botao>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
