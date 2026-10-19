import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { authService } from '../services/auth';
import { session, setUnauthorizedHandler } from '../services/api';
import type { Usuario } from '../types/api';

interface AuthContexto {
  usuario: Usuario | null;
  carregando: boolean;
  entrar: (nome: string, senha: string) => Promise<Usuario>;
  sair: () => void;
  ehAdmin: boolean;
}

const AuthContext = createContext<AuthContexto | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [usuario, setUsuario] = useState<Usuario | null>(() => session.usuario<Usuario>());
  const [carregando, setCarregando] = useState(() => Boolean(session.token()));

  useEffect(() => {
    setUnauthorizedHandler(() => setUsuario(null));

    if (!session.token()) {
      setCarregando(false);
      return () => setUnauthorizedHandler(null);
    }

    let ativo = true;
    authService
      .me()
      .then((dados) => {
        if (ativo) setUsuario(dados);
      })
      .catch(() => {
        session.limpar();
        if (ativo) setUsuario(null);
      })
      .finally(() => {
        if (ativo) setCarregando(false);
      });

    return () => {
      ativo = false;
      setUnauthorizedHandler(null);
    };
  }, []);

  const valor = useMemo<AuthContexto>(
    () => ({
      usuario,
      carregando,
      ehAdmin: usuario?.permissao === 'Administrador',
      async entrar(nome, senha) {
        const resposta = await authService.login(nome, senha);
        session.salvar(resposta.token, resposta.usuario);
        setUsuario(resposta.usuario);
        return resposta.usuario;
      },
      sair() {
        session.limpar();
        setUsuario(null);
      },
    }),
    [usuario, carregando],
  );

  return <AuthContext.Provider value={valor}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const contexto = useContext(AuthContext);
  if (!contexto) throw new Error('useAuth deve ser usado dentro de AuthProvider.');
  return contexto;
}

export function paginaInicial(permissao: Usuario['permissao'] | undefined): string {
  if (permissao === 'Operador') return '/pdv';
  if (permissao === 'Estoquista') return '/produtos';
  return '/dashboard';
}
