import { useState } from 'react';
import type { FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { Alerta, Botao, Campo, Entrada } from '../components/ui';
import { paginaInicial, useAuth } from '../hooks/useAuth';
import { ApiError } from '../services/api';

export function Login() {
  const { usuario, entrar } = useAuth();
  const navigate = useNavigate();
  const local = useLocation();
  const [nome, setNome] = useState('');
  const [senha, setSenha] = useState('');
  const [erro, setErro] = useState('');
  const [enviando, setEnviando] = useState(false);

  if (usuario) return <Navigate to={paginaInicial(usuario.permissao)} replace />;

  async function onSubmit(evento: FormEvent) {
    evento.preventDefault();
    setErro('');

    if (!nome.trim() || !senha) {
      setErro('Informe usuário e senha.');
      return;
    }

    setEnviando(true);
    try {
      const autenticado = await entrar(nome.trim(), senha);
      const destino = (local.state as { de?: string } | null)?.de;
      navigate(destino && destino !== '/login' ? destino : paginaInicial(autenticado.permissao), { replace: true });
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível entrar.');
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="login-tela">
      <section className="login-lateral">
        <div className="login-marca">
          <span className="marca-icone grande">PDV</span>
          <div>
            <strong>PDVStore</strong>
            <small>Gestão de ponto de venda</small>
          </div>
        </div>

        <div className="login-chamada">
          <h1>Seu PDV completo, agora na web.</h1>
          <p>
            Vendas, estoque, caixa, compras, clientes e relatórios em uma interface
            integrada ao mesmo banco de dados do PDVStore desktop.
          </p>
          <ul>
            <li>Venda com controle de estoque e fiado</li>
            <li>Abertura e fechamento de caixa</li>
            <li>Alertas de reposição e dashboard</li>
          </ul>
        </div>

        <small className="login-rodape">Acesso restrito a usuários cadastrados no PDVStore.</small>
      </section>

      <section className="login-area">
        <form className="login-card" onSubmit={onSubmit}>
          <div>
            <h2>Entrar</h2>
            <p className="texto-suave">Use as mesmas credenciais da aplicação desktop.</p>
          </div>

          {erro && <Alerta tipo="erro">{erro}</Alerta>}

          <Campo label="Usuário" obrigatorio>
            <Entrada
              value={nome}
              onChange={(e) => setNome(e.target.value)}
              autoComplete="username"
              autoFocus
              placeholder="Nome do usuário"
            />
          </Campo>

          <Campo label="Senha" obrigatorio>
            <Entrada
              type="password"
              value={senha}
              onChange={(e) => setSenha(e.target.value)}
              autoComplete="current-password"
              placeholder="Sua senha"
            />
          </Campo>

          <Botao type="submit" carregando={enviando} className="botao-largo">
            Entrar no sistema
          </Botao>

          <p className="login-aviso">
            O acesso é controlado por perfil: Administrador, Operador (Caixa) ou Estoquista.
          </p>
        </form>
      </section>
    </div>
  );
}
