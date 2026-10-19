import { Navigate, Route, Routes } from 'react-router-dom';
import { Layout } from '../layouts/Layout';
import { RequireAuth, RequireRole } from './guards';
import { paginaInicial, useAuth } from '../hooks/useAuth';
import { Login } from '../pages/Login';
import { Dashboard } from '../pages/Dashboard';
import { Produtos } from '../pages/Produtos';
import { Estoque } from '../pages/Estoque';
import { Pdv } from '../pages/Pdv';
import { Vendas } from '../pages/Vendas';
import { Caixa } from '../pages/Caixa';
import { Clientes } from '../pages/Clientes';
import { Fornecedores } from '../pages/Fornecedores';
import { Compras } from '../pages/Compras';
import { Usuarios } from '../pages/Usuarios';
import { NaoEncontrado } from '../pages/NaoEncontrado';

function Inicio() {
  const { usuario } = useAuth();
  return <Navigate to={paginaInicial(usuario?.permissao)} replace />;
}

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />

      <Route element={<RequireAuth />}>
        <Route element={<Layout />}>
          <Route index element={<Inicio />} />

          <Route element={<RequireRole papeis={['Administrador']} />}>
            <Route path="dashboard" element={<Dashboard />} />
            <Route path="vendas" element={<Vendas />} />
            <Route path="estoque" element={<Estoque />} />
            <Route path="clientes" element={<Clientes />} />
            <Route path="fornecedores" element={<Fornecedores />} />
            <Route path="compras" element={<Compras />} />
            <Route path="caixa" element={<Caixa />} />
            <Route path="usuarios" element={<Usuarios />} />
          </Route>

          <Route element={<RequireRole papeis={['Administrador', 'Operador']} />}>
            <Route path="pdv" element={<Pdv />} />
          </Route>

          <Route element={<RequireRole papeis={['Administrador', 'Estoquista']} />}>
            <Route path="produtos" element={<Produtos />} />
          </Route>
        </Route>
      </Route>

      <Route path="*" element={<NaoEncontrado />} />
    </Routes>
  );
}
