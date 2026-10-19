import type { ButtonHTMLAttributes, InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from 'react';

type Variante = 'primario' | 'secundario' | 'perigo' | 'fantasma';

interface BotaoProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variante?: Variante;
  carregando?: boolean;
}

export function Botao({ variante = 'primario', carregando, children, className = '', disabled, ...props }: BotaoProps) {
  return (
    <button
      {...props}
      disabled={disabled || carregando}
      className={`botao botao-${variante} ${className}`}
    >
      {carregando && <span className="spinner spinner-claro" />}
      {children}
    </button>
  );
}

interface CampoProps {
  label: string;
  erro?: string;
  dica?: string;
  children: ReactNode;
  obrigatorio?: boolean;
}

export function Campo({ label, erro, dica, children, obrigatorio }: CampoProps) {
  return (
    <label className={`campo ${erro ? 'campo-erro' : ''}`}>
      <span className="campo-label">
        {label} {obrigatorio && <em>*</em>}
      </span>
      {children}
      {dica && !erro && <small>{dica}</small>}
      {erro && <small className="texto-erro">{erro}</small>}
    </label>
  );
}

export const Entrada = (props: InputHTMLAttributes<HTMLInputElement>) => (
  <input {...props} className={`entrada ${props.className ?? ''}`} />
);

export const Selecao = (props: SelectHTMLAttributes<HTMLSelectElement>) => (
  <select {...props} className={`entrada ${props.className ?? ''}`} />
);

export const AreaTexto = (props: TextareaHTMLAttributes<HTMLTextAreaElement>) => (
  <textarea {...props} className={`entrada ${props.className ?? ''}`} />
);

export function Modal({ titulo, children, onFechar, largura }: {
  titulo: string;
  children: ReactNode;
  onFechar: () => void;
  largura?: string;
}) {
  return (
    <div className="modal-fundo" role="dialog" aria-modal="true" onMouseDown={(e) => e.target === e.currentTarget && onFechar()}>
      <div className="modal" style={{ maxWidth: largura ?? '640px' }}>
        <header className="modal-topo">
          <h2>{titulo}</h2>
          <button className="modal-fechar" onClick={onFechar} aria-label="Fechar">×</button>
        </header>
        <div className="modal-corpo">{children}</div>
      </div>
    </div>
  );
}

export function Alerta({ tipo = 'info', children }: { tipo?: 'info' | 'sucesso' | 'erro' | 'aviso'; children: ReactNode }) {
  return <div className={`alerta alerta-${tipo}`}>{children}</div>;
}

export function Spinner({ texto }: { texto?: string }) {
  return (
    <div className="carregando">
      <span className="spinner" />
      {texto && <span>{texto}</span>}
    </div>
  );
}

export function Vazio({ titulo, descricao }: { titulo: string; descricao?: string }) {
  return (
    <div className="vazio">
      <strong>{titulo}</strong>
      {descricao && <span>{descricao}</span>}
    </div>
  );
}

export function Etiqueta({ children, tom = 'neutro' }: { children: ReactNode; tom?: 'neutro' | 'verde' | 'vermelho' | 'amarelo' | 'azul' }) {
  return <span className={`etiqueta etiqueta-${tom}`}>{children}</span>;
}

export function Paginacao({ page, totalPages, totalItems, onPage }: {
  page: number;
  totalPages: number;
  totalItems: number;
  onPage: (pagina: number) => void;
}) {
  if (totalPages <= 1) {
    return <div className="paginacao"><span>{totalItems} registro(s)</span></div>;
  }
  return (
    <div className="paginacao">
      <span>{totalItems} registro(s)</span>
      <div>
        <Botao variante="secundario" disabled={page <= 1} onClick={() => onPage(page - 1)}>Anterior</Botao>
        <span className="paginacao-atual">{page} / {totalPages}</span>
        <Botao variante="secundario" disabled={page >= totalPages} onClick={() => onPage(page + 1)}>Próxima</Botao>
      </div>
    </div>
  );
}

export function CabecalhoPagina({ titulo, descricao, acoes }: { titulo: string; descricao?: string; acoes?: ReactNode }) {
  return (
    <header className="cabecalho-pagina">
      <div>
        <h1>{titulo}</h1>
        {descricao && <p>{descricao}</p>}
      </div>
      {acoes && <div className="cabecalho-acoes">{acoes}</div>}
    </header>
  );
}
