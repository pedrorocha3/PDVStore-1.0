const CAMINHOS: Record<string, string> = {
  dashboard: 'M3 13h8V3H3v10Zm10 8h8V11h-8v10ZM3 21h8v-6H3v6Zm10-12h8V3h-8v6Z',
  pdv: 'M4 7h16l-1.5 11h-13L4 7Zm4-3h8v3H8V4Zm0 9h8m-4 0v4',
  vendas: 'M6 3h12v18l-3-2-3 2-3-2-3 2V3Zm3 5h6m-6 4h6m-6 4h4',
  produtos: 'M21 8l-9-5-9 5 9 5 9-5Zm-18 0v8l9 5 9-5V8m-9 5v8',
  estoque: 'M3 4h18v4H3V4Zm0 6h18v10H3V10Zm4 4h5',
  clientes: 'M16 11a4 4 0 1 0-8 0 4 4 0 0 0 8 0Zm-12 9a8 8 0 0 1 16 0M19 8a3 3 0 1 0 0-6',
  fornecedores: 'M3 21V7l6-4 6 4v14M9 21v-4h6v4m3-8h3v8h-3M3 11h6',
  compras: 'M3 4h2l2.4 11h11.2L21 7H6m2 12a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3Zm10 0a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3Z',
  caixa: 'M3 7h18v12H3V7Zm0 4h18M7 15h4',
  usuarios: 'M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8Zm-7 9a7 7 0 0 1 14 0M19 5v6m-3-3h6',
  sair: 'M10 17l5-5-5-5m5 5H3m10-9h8v18h-8',
  menu: 'M4 6h16M4 12h16M4 18h16',
};

export function Icone({ nome, tamanho = 20 }: { nome: keyof typeof CAMINHOS | string; tamanho?: number }) {
  return (
    <svg
      width={tamanho}
      height={tamanho}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d={CAMINHOS[nome] ?? CAMINHOS.dashboard} />
    </svg>
  );
}
