import { Link } from 'react-router-dom';
import { paginaInicial, useAuth } from '../hooks/useAuth';

export function NaoEncontrado() {
  const { usuario } = useAuth();
  const destino = usuario ? paginaInicial(usuario.permissao) : '/login';

  return (
    <div className="tela-simples">
      <div className="estado-vazio-grande">
        <span className="codigo-404">404</span>
        <h1>Página não encontrada</h1>
        <p>O endereço informado não existe nesta aplicação.</p>
        <Link className="botao botao-primario" to={destino}>Voltar ao início</Link>
      </div>
    </div>
  );
}
