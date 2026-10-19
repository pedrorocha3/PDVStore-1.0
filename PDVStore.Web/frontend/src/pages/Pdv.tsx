import { useCallback, useEffect, useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { Alerta, Botao, Campo, Entrada, Etiqueta, Modal, Selecao, Spinner, Vazio } from '../components/ui';
import { caixaService } from '../services/caixa';
import { clientesService } from '../services/clientes';
import { produtosService } from '../services/produtos';
import { vendasService } from '../services/vendas';
import { ApiError } from '../services/api';
import { useToast } from '../hooks/useToast';
import type { Caixa, Cliente, Produto, Venda } from '../types/api';
import { brl, numero } from '../utils/format';

interface ItemCarrinho { produto: Produto; quantidade: number; }
const FORMAS = ['Dinheiro', 'PIX', 'Cartão Crédito', 'Cartão Débito', 'Fiado'];

export function Pdv() {
  const { sucesso, erro: avisarErro } = useToast();
  const [caixa, setCaixa] = useState<Caixa | null>(null);
  const [carregandoCaixa, setCarregandoCaixa] = useState(true);
  const [clientes, setClientes] = useState<Cliente[]>([]);
  const [busca, setBusca] = useState('');
  const [produtos, setProdutos] = useState<Produto[]>([]);
  const [buscando, setBuscando] = useState(false);
  const [codigo, setCodigo] = useState('');
  const [carrinho, setCarrinho] = useState<ItemCarrinho[]>([]);
  const [desconto, setDesconto] = useState(0);
  const [forma, setForma] = useState('Dinheiro');
  const [clienteId, setClienteId] = useState('');
  const [finalizando, setFinalizando] = useState(false);
  const [erro, setErro] = useState('');
  const [venda, setVenda] = useState<Venda | null>(null);

  const carregarCaixa = useCallback(async () => {
    setCarregandoCaixa(true);
    try { setCaixa((await caixaService.aberto()) ?? null); }
    catch (e) { avisarErro(e instanceof ApiError ? e.message : 'Não foi possível consultar o caixa.'); }
    finally { setCarregandoCaixa(false); }
  }, [avisarErro]);

  const buscarProdutos = useCallback(async (termo = '') => {
    setBuscando(true);
    try {
      const resultado = await produtosService.listar({ busca: termo || undefined, ativo: true, pageSize: 30, ordenarPor: 'nome' });
      setProdutos(resultado.items);
    } catch (e) { avisarErro(e instanceof ApiError ? e.message : 'Não foi possível buscar produtos.'); }
    finally { setBuscando(false); }
  }, [avisarErro]);

  useEffect(() => {
    void carregarCaixa();
    void buscarProdutos();
    clientesService.listar({ ativo: true, pageSize: 200 }).then((r) => setClientes(r.items)).catch(() => setClientes([]));
  }, [carregarCaixa, buscarProdutos]);

  function adicionar(produto: Produto) {
    if (produto.estoque <= 0) { avisarErro(`"${produto.nome}" está sem estoque.`); return; }
    setCarrinho((atual) => {
      const existente = atual.find((i) => i.produto.id === produto.id);
      if (existente) {
        if (existente.quantidade >= produto.estoque) { avisarErro(`Estoque máximo de "${produto.nome}" atingido.`); return atual; }
        return atual.map((i) => i.produto.id === produto.id ? { ...i, quantidade: i.quantidade + 1 } : i);
      }
      return [...atual, { produto, quantidade: 1 }];
    });
  }

  async function adicionarPorCodigo(evento: FormEvent) {
    evento.preventDefault();
    const termo = codigo.trim();
    if (!termo) return;
    try { adicionar(await produtosService.porCodigo(termo)); setCodigo(''); }
    catch (e) { avisarErro(e instanceof ApiError ? `Código não encontrado: ${e.message}` : 'Produto não encontrado.'); }
  }

  function alterarQuantidade(produtoId: number, quantidade: number) {
    setCarrinho((atual) => atual.map((item) => {
      if (item.produto.id !== produtoId) return item;
      return { ...item, quantidade: Math.min(Math.max(1, item.produto.estoque), Math.max(1, quantidade)) };
    }));
  }

  function remover(produtoId: number) { setCarrinho((atual) => atual.filter((i) => i.produto.id !== produtoId)); }

  const subtotal = useMemo(() => carrinho.reduce((s, i) => s + i.produto.preco * i.quantidade, 0), [carrinho]);
  const total = Math.max(0, subtotal - desconto);
  const quantidadeItens = carrinho.reduce((s, i) => s + i.quantidade, 0);

  async function finalizar() {
    setErro('');
    if (carrinho.length === 0) { setErro('Adicione pelo menos um item à venda.'); return; }
    if (!caixa) { setErro('Abra o caixa antes de registrar vendas.'); return; }
    if (forma === 'Fiado' && !clienteId) { setErro('Selecione o cliente para a venda fiada.'); return; }
    if (desconto < 0 || desconto > subtotal) { setErro('Desconto inválido.'); return; }
    setFinalizando(true);
    try {
      const registrada = await vendasService.registrar({
        itens: carrinho.map((i) => ({ produtoId: i.produto.id, quantidade: i.quantidade })),
        desconto, formaPagamento: forma, clienteId: forma === 'Fiado' ? Number(clienteId) : null,
      });
      setVenda(registrada);
      sucesso(`Venda #${registrada.id} registrada.`);
      setCarrinho([]); setDesconto(0); setClienteId('');
      await Promise.all([buscarProdutos(), carregarCaixa()]);
    } catch (e) {
      setErro(e instanceof ApiError ? [e.message, ...(e.details ?? [])].join(' ') : 'Não foi possível finalizar a venda.');
    } finally { setFinalizando(false); }
  }

  return (
    <>
      <header className="cabecalho-pagina">
        <div><h1>Vender (PDV)</h1><p>Busque o produto, monte o carrinho e finalize a venda.</p></div>
        <div className="status-caixa">
          {carregandoCaixa ? <Spinner /> : caixa ? (
            <><Etiqueta tom="verde">Caixa aberto #{caixa.id}</Etiqueta><small>Inicial {brl(caixa.valorInicial)} · {caixa.usuarioNome}</small></>
          ) : <Etiqueta tom="vermelho">Caixa fechado</Etiqueta>}
        </div>
      </header>
      {!carregandoCaixa && !caixa && <Alerta tipo="aviso">Nenhum caixa aberto. Um Administrador precisa abrir o caixa antes de registrar vendas.</Alerta>}
      <div className="pdv-grade">
        <section className="painel pdv-produtos">
          <form className="barra-filtros" onSubmit={(e) => { e.preventDefault(); void buscarProdutos(busca); }}>
            <Entrada placeholder="Buscar produto por nome ou código" value={busca} onChange={(e) => setBusca(e.target.value)} aria-label="Buscar produto" />
            <Botao type="submit" variante="secundario" carregando={buscando}>Buscar</Botao>
          </form>
          <form className="barra-filtros" onSubmit={adicionarPorCodigo}>
            <Entrada placeholder="Bipe ou digite o código de barras" value={codigo} onChange={(e) => setCodigo(e.target.value)} aria-label="Código de barras" />
            <Botao type="submit" variante="secundario">Adicionar código</Botao>
          </form>
          {buscando ? <Spinner texto="Buscando produtos..." /> : produtos.length === 0 ? (
            <Vazio titulo="Nenhum produto encontrado" descricao="Verifique o termo de busca ou o cadastro." />
          ) : (
            <div className="lista-produtos-pdv">
              {produtos.map((produto) => (
                <button key={produto.id} className="produto-pdv" onClick={() => adicionar(produto)} disabled={produto.estoque <= 0}>
                  <div><strong>{produto.nome}</strong><small>{produto.codigoBarras ?? 'sem código'} · {produto.categoria ?? 'sem categoria'}</small></div>
                  <div className="produto-pdv-lado">
                    <strong>{brl(produto.preco)}</strong>
                    <Etiqueta tom={produto.estoque > produto.estoqueMinimo ? 'verde' : 'vermelho'}>{produto.estoque} un.</Etiqueta>
                  </div>
                </button>
              ))}
            </div>
          )}
        </section>
        <section className="painel pdv-carrinho">
          <header className="painel-topo">
            <h2>Carrinho ({numero(quantidadeItens)} item(ns))</h2>
            {carrinho.length > 0 && <Botao variante="fantasma" className="texto-perigo" onClick={() => setCarrinho([])}>Limpar</Botao>}
          </header>
          {carrinho.length === 0 ? (
            <Vazio titulo="Carrinho vazio" descricao="Clique em um produto para adicioná-lo." />
          ) : (
            <ul className="itens-carrinho">
              {carrinho.map((item) => (
                <li key={item.produto.id}>
                  <div className="item-carrinho-nome"><strong>{item.produto.nome}</strong><small>{brl(item.produto.preco)} cada</small></div>
                  <input className="qtd-carrinho" type="number" min={1} max={item.produto.estoque} value={item.quantidade}
                    onChange={(e) => alterarQuantidade(item.produto.id, Number(e.target.value))} aria-label={`Quantidade de ${item.produto.nome}`} />
                  <span className="item-carrinho-total">{brl(item.produto.preco * item.quantidade)}</span>
                  <button className="remover-item" onClick={() => remover(item.produto.id)} aria-label="Remover item">×</button>
                </li>
              ))}
            </ul>
          )}
          <div className="resumo-venda">
            <div className="linha-resumo"><span>Subtotal</span><strong>{brl(subtotal)}</strong></div>
            <div className="linha-resumo"><span>Desconto (R$)</span>
              <Entrada type="number" min={0} step="0.01" value={desconto} onChange={(e) => setDesconto(Math.max(0, Number(e.target.value)))} className="entrada-desconto" />
            </div>
            <div className="linha-resumo total"><span>Total</span><strong>{brl(total)}</strong></div>
          </div>
          <Campo label="Forma de pagamento" obrigatorio>
            <Selecao value={forma} onChange={(e) => setForma(e.target.value)}>
              {FORMAS.map((f) => <option key={f} value={f}>{f}</option>)}
            </Selecao>
          </Campo>
          {forma === 'Fiado' && (
            <Campo label="Cliente (fiado)" obrigatorio dica="O limite de crédito é validado pela API.">
              <Selecao value={clienteId} onChange={(e) => setClienteId(e.target.value)}>
                <option value="">Selecione o cliente...</option>
                {clientes.map((c) => <option key={c.id} value={c.id}>{c.nome} — deve {brl(c.saldoDevedor)}</option>)}
              </Selecao>
            </Campo>
          )}
          {erro && <Alerta tipo="erro">{erro}</Alerta>}
          <Botao className="botao-largo botao-finalizar" onClick={() => void finalizar()} carregando={finalizando} disabled={!caixa || carrinho.length === 0}>
            Finalizar venda · {brl(total)}
          </Botao>
        </section>
      </div>
      {venda && (
        <Modal titulo={`Comprovante da venda #${venda.id}`} onFechar={() => setVenda(null)} largura="520px">
          <div className="comprovante">
            <div className="comprovante-topo">
              <strong>PDVStore</strong>
              <small>{new Date(venda.dataVenda).toLocaleString('pt-BR')}</small>
              <small>Venda #{venda.id} · Caixa #{venda.caixaId}</small>
            </div>
            <ul>{venda.itens.map((item) => <li key={item.id}><span>{item.quantidade}x {item.produtoNome}</span><span>{brl(item.subtotal)}</span></li>)}</ul>
            <div className="comprovante-totais">
              <div><span>Subtotal</span><span>{brl(venda.valorTotal + venda.desconto)}</span></div>
              {venda.desconto > 0 && <div><span>Desconto</span><span>- {brl(venda.desconto)}</span></div>}
              <div className="total"><span>Total</span><span>{brl(venda.valorTotal)}</span></div>
            </div>
            <p><strong>Pagamento:</strong> {venda.formaPagamento}</p>
            {venda.pixTxId && <p className="mono"><strong>PIX TxId:</strong> {venda.pixTxId}</p>}
            {venda.clienteNome && <p><strong>Cliente:</strong> {venda.clienteNome}</p>}
            <p><strong>Operador:</strong> {venda.usuarioNome ?? '—'}</p>
          </div>
          <div className="modal-acoes">
            <Botao variante="secundario" onClick={() => window.print()}>Imprimir</Botao>
            <Botao onClick={() => setVenda(null)}>Nova venda</Botao>
          </div>
        </Modal>
      )}
    </>
  );
}
