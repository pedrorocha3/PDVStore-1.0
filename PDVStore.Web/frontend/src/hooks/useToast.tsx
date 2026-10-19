import { createContext, useCallback, useContext, useState } from 'react';
import type { ReactNode } from 'react';

type TipoAviso = 'sucesso' | 'erro' | 'info';

interface Aviso {
  id: number;
  tipo: TipoAviso;
  mensagem: string;
}

interface ToastContexto {
  sucesso: (mensagem: string) => void;
  erro: (mensagem: string) => void;
  info: (mensagem: string) => void;
}

const ToastContext = createContext<ToastContexto | null>(null);
let sequencia = 0;

export function ToastProvider({ children }: { children: ReactNode }) {
  const [avisos, setAvisos] = useState<Aviso[]>([]);

  const remover = useCallback((id: number) => {
    setAvisos((atuais) => atuais.filter((a) => a.id !== id));
  }, []);

  const adicionar = useCallback((tipo: TipoAviso, mensagem: string) => {
    const id = ++sequencia;
    setAvisos((atuais) => [...atuais, { id, tipo, mensagem }]);
    window.setTimeout(() => remover(id), 5000);
  }, [remover]);

  return (
    <ToastContext.Provider
      value={{
        sucesso: (m) => adicionar('sucesso', m),
        erro: (m) => adicionar('erro', m),
        info: (m) => adicionar('info', m),
      }}
    >
      {children}
      <div className="toasts" aria-live="polite">
        {avisos.map((aviso) => (
          <div key={aviso.id} className={`toast toast-${aviso.tipo}`} onClick={() => remover(aviso.id)}>
            <span>{aviso.tipo === 'sucesso' ? '✓' : aviso.tipo === 'erro' ? '!' : 'i'}</span>
            {aviso.mensagem}
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast() {
  const contexto = useContext(ToastContext);
  if (!contexto) throw new Error('useToast deve ser usado dentro de ToastProvider.');
  return contexto;
}
