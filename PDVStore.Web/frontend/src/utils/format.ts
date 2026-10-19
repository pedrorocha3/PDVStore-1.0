export const brl = (valor: number | null | undefined) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(valor ?? 0);

export const data = (iso: string | null | undefined) => {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString('pt-BR');
};

export const dataHora = (iso: string | null | undefined) => {
  if (!iso) return '—';
  return new Date(iso).toLocaleString('pt-BR');
};

export const numero = (valor: number | null | undefined) =>
  new Intl.NumberFormat('pt-BR').format(valor ?? 0);

/** Converte "dd/mm/aaaa" ou ISO para o formato aceito pela API (yyyy-mm-ddT...). */
export const paraIsoInicio = (valor: string): string | undefined => {
  if (!valor) return undefined;
  if (valor.includes('T')) return valor;
  return `${valor}T00:00:00Z`;
};

export const paraIsoFim = (valor: string): string | undefined => {
  if (!valor) return undefined;
  if (valor.includes('T')) return valor;
  return `${valor}T23:59:59Z`;
};

export const hoje = () => new Date().toISOString().slice(0, 10);

export const inicioDoMes = () => {
  const agora = new Date();
  return new Date(Date.UTC(agora.getUTCFullYear(), agora.getUTCMonth(), 1)).toISOString().slice(0, 10);
};

export const percentualEstoque = (estoque: number, minimo: number) => {
  if (minimo <= 0) return estoque > 0 ? 100 : 0;
  return Math.min(100, Math.round((estoque / Math.max(minimo, 1)) * 100));
};
