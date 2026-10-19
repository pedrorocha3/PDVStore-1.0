import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { paginaInicial, useAuth } from '../hooks/useAuth';
import { Spinner } from '../components/ui';
import type { TipoPermissao } from '../types/api';

export function RequireAuth() {
  const { usuario, carregando } = useAuth();
  const local = useLocation();

  if (carregando) {
    return (
      <div className="tela-carregando">
        <Spinner texto="Validando sessão..." />
      </div>
    );
  }

  if (!usuario) {
    return <Navigate to="/login" replace state={{ de: local.pathname }} />;
  }

  return <Outlet />;
}

export function RequireRole({ papeis }: { papeis: TipoPermissao[] }) {
  const { usuario } = useAuth();

  if (!usuario) return <Navigate to="/login" replace />;
  if (!papeis.includes(usuario.permissao)) {
    return <Navigate to={paginaInicial(usuario.permissao)} replace />;
  }

  return <Outlet />;
}
