export type TipoPermissao = 'Operador' | 'Administrador' | 'Estoquista';

export interface Usuario {
  id: number;
  nome: string;
  permissao: TipoPermissao;
  permissaoDescricao: string;
  ativo: boolean;
  createdAt: string;
  fotoPath?: string | null;
}

export interface LoginResponse {
  token: string;
  expiraEm: string;
  usuario: Usuario;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface Produto {
  id: number;
  nome: string;
  codigoBarras: string | null;
  preco: number;
  precoCusto: number;
  estoque: number;
  estoqueMinimo: number;
  categoria: string | null;
  descricao: string | null;
  ativo: boolean;
}

export interface ProdutoPayload {
  codigoBarras: string;
  nome: string;
  preco: number;
  precoCusto: number;
  estoque?: number;
  estoqueMinimo: number;
  categoria: string;
  descricao: string;
}

export interface Cliente {
  id: number;
  nome: string;
  cpfCnpj: string | null;
  telefone: string | null;
  email: string | null;
  endereco: string | null;
  cadastradoEm: string;
  ativo: boolean;
  limiteCredito: number;
  saldoDevedor: number;
}

export interface ClientePayload {
  nome: string;
  cpfCnpj?: string;
  telefone?: string;
  email?: string;
  endereco?: string;
  limiteCredito: number;
}

export interface Fornecedor {
  id: number;
  nome: string;
  cnpj: string | null;
  telefone: string | null;
  email: string | null;
  ativo: boolean;
}

export interface FornecedorPayload {
  nome: string;
  cnpj?: string;
  telefone?: string;
  email?: string;
}

export interface Caixa {
  id: number;
  usuarioCaixaId: number;
  usuarioNome: string | null;
  abertura: string;
  fechamento: string | null;
  valorInicial: number;
  valorFinal: number | null;
  sangria: number | null;
  status: string;
}

export interface MovimentacaoEstoque {
  id: number;
  produtoId: number;
  produtoNome: string | null;
  tipo: string;
  quantidade: number;
  precoUnitario: number | null;
  dataMovimentacao: string;
  usuarioId: number | null;
  usuarioNome: string | null;
  motivo: string | null;
  referenciaVendaId: number | null;
}

export interface ItemVenda {
  id: number;
  produtoId: number;
  produtoNome: string | null;
  quantidade: number;
  precoUnitario: number;
  subtotal: number;
}

export interface Venda {
  id: number;
  usuarioCaixaId: number;
  usuarioNome: string | null;
  dataVenda: string;
  valorTotal: number;
  desconto: number;
  formaPagamento: string;
  pixTxId: string | null;
  status: string | null;
  caixaId: number;
  clienteId: number | null;
  clienteNome: string | null;
  itens: ItemVenda[];
}

export interface VendaPayload {
  itens: { produtoId: number; quantidade: number }[];
  desconto: number;
  formaPagamento: string;
  clienteId?: number | null;
}

export interface ItemCompra {
  id: number;
  produtoId: number;
  produtoNome: string | null;
  quantidade: number;
  precoCusto: number;
  subtotal: number;
}

export interface Compra {
  id: number;
  fornecedorId: number;
  fornecedorNome: string | null;
  usuarioCaixaId: number;
  usuarioNome: string | null;
  dataCompra: string;
  numeroNota: string | null;
  valorTotal: number;
  status: string;
  itens: ItemCompra[];
}

export interface CompraPayload {
  fornecedorId: number;
  numeroNota?: string;
  itens: { produtoId: number; quantidade: number; precoCusto: number }[];
}

export interface ItemRelatorio {
  nomeProduto: string;
  codigoBarras: string | null;
  totalVendido: number;
  valorTotalVendido: number;
  estoqueAtual: number;
  estoqueMinimo: number;
  statusMinimo: string;
}

export interface ResumoFormaPagamento {
  formaPagamento: string;
  quantidade: number;
  total: number;
}

export interface Dashboard {
  inicio: string;
  fim: string;
  totalPeriodo: number;
  quantidadeVendas: number;
  itensMaisVendidos: ItemRelatorio[];
  alertasEstoque: Produto[];
  pagamentosPorForma: ResumoFormaPagamento[];
  vendasPorDia: { dia: string; total: number }[];
  movimentacoesPorDia: { dia: string; entradas: number; saidas: number }[];
}

export interface UsuarioPayload {
  nome: string;
  senha?: string;
  permissao: TipoPermissao;
  ativo?: boolean;
  fotoPath?: string | null;
}
