import { useCallback, useEffect, useState } from 'react';
import { Alerta, Botao, CabecalhoPagina, Campo, Entrada, Etiqueta, Selecao, Spinner, Vazio } from '../components/ui';
import { comprasService } from '../services/compras';
import { fornecedoresService } from '../services/fornecedores';
import { produtosService } from '../services/produtos';
import { ApiError } from '../services/api';
import { useToast } from '../hooks/useToast';
import type { Compra, Fornecedor, Produto } from '../types/api';
import { brl, dataHora, numero } from '../utils/format';

interface ItemNovo {
  produtoId: number;
  nome: string;
  quantidade: number;
  precoCusto: number;
}

export function Compras() {
  const { sucesso, erro: avisarErro } = useToast();
  const [fornecedores, setFornecedores] = useState<Fornecedor[]>([]);
  const [produtos, setProdutos] = useState<Produto[]>([]);
  const [compras, setCompras] = useState<Compra[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');
  const [fornecedorId, setFornecedorId] = useState('');
  const [numeroNota, setNumeroNota] = useState('');
  const [produtoId, setProdutoId] = useState('');
  const [quantidade, setQuantidade] = useState(1);
  const [precoCusto, setPrecoCusto] = useState(0);
  const [itens, setItens] = useState<ItemNovo[]>([]);
  const [salvando, setSalvando] = useState(false);

  const carregar = useCallback(async () => {
    setCarregando(true);
    setErro('');
    try {
      const [listaCompras, listaFornecedores, listaProdutos] = await Promise.all([
        comprasService.listar({ page: 1, pageSize: 20 }),
        fornecedoresService.listar({ ativo: true, pageSize: 100 }),
        produtosService.listar({ ativo: true, pageSize: 100, ordenarPor: 'nome' }),
      ]);
      setCompras(listaCompras.items);
      setFornecedores(listaFornecedores.items);
      setProdutos(listaProdutos.items);
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível carregar as compras.');
    } finally {
      setCarregando(false);
    }
  }, []);

  useEffect(() => { void carregar(); }, [carregar]);

  function selecionarProduto(id: string) {
    setProdutoId(id);
    const produto = produtos.find((p) => p.id === Number(id));
    if (produto) setPrecoCusto(produto.precoCusto);
  }

  function adicionarItem() {
    const produto = produtos.find((p) => p.id === Number(produtoId));
    if (!produto) { avisarErro('Selecione um produto.'); return; }
    if (quantidade <= 0) { avisarErro('Quantidade deve ser maior que zero.'); return; }
    if (precoCusto < 0) { avisarErro('Preço de custo não pode ser negativo.'); return; }

    setItens((atuais) => {
      const existente = atuais.find((i) => i.produtoId === produto.id);
      if (existente) {
        return atuais.map((i) => i.produtoId === produto.id
          ? { ...i, quantidade: i.quantidade + quantidade, precoCusto }
          : i);
      }
      return [...atuais, { produtoId: produto.id, nome: produto.nome, quantidade, precoCusto }];
    });
    setProdutoId('');
    setQuantidade(1);
    setPrecoCusto(0);
  }

  const total = itens.reduce((soma, item) => soma + item.quantidade * item.precoCusto, 0);

  async function salvar() {
    if (!fornecedorId) { avisarErro('Selecione o fornecedor.'); return; }
    if (itens.length === 0) { avisarErro('Adicione pelo menos um item.'); return; }

    setSalvando(true);
    try {
      await comprasService.registrar({
        fornecedorId: Number(fornecedorId),
        numeroNota: numeroNota.trim() || undefined,
        itens: itens.map((i) => ({
          produtoId: i.produtoId,
          quantidade: i.quantidade,
          precoCusto: i.precoCusto,
        })),
      });
      sucesso('Compra registrada e estoque atualizado.');
      setItens([]);
      setNumeroNota('');
      setFornecedorId('');
      await carregar();
    } catch (e) {
      avisarErro(e instanceof ApiError ? e.message : 'Não foi possível registrar a compra.');
    } finally {
      setSalvando(false);
    }
  }

  async function cancelar(compra: Compra) {
    if (!window.confirm(`Cancelar a compra #${compra.id}? O estoque será estornado.`)) return;
    try {
      await comprasService.cancelar(compra.id);
      sucesso(`Compra #${compra.id} cancelada.`);
      await carregar();
    } catch (e) {
      avisarErro(e instanceof ApiError ? e.message : 'Não foi possível cancelar a compra.');
    }
  }

  return (
    <>
      <CabecalhoPagina
        titulo="Compras"
        descricao="Entrada de mercadorias. Ao registrar, o estoque e o preço de custo são atualizados."
      />

      <section className="painel">
        <header className="painel-topo"><h2>Nova compra</h2></header>
        <div className="grade-3-form">
          <Campo label="Fornecedor" obrigatorio>
            <Selecao value={fornecedorId} onChange={(e) => setFornecedorId(e.target.value)}>
              <option value="">Selecione...</option>
              {fornecedores.map((f) => <option key={f.id} value={f.id}>{f.nome}</option>)}
            </Selecao>
          </Campo>
          <Campo label="Número da nota">
            <Entrada value={numeroNota} onChange={(e) => setNumeroNota(e.target.value)} maxLength={60} />
          </Campo>
          <div className="campo">
            <span className="campo-label">Total da compra</span>
            <div className="valor-calculado destaque">{brl(total)}</div>
          </div>
        </div>

        <div className="barra-filtros itens-compra-linha">
          <Selecao value={produtoId} onChange={(e) => selecionarProduto(e.target.value)} aria-label="Produto">
            <option value="">Selecione o produto...</option>
            {produtos.map((p) => <option key={p.id} value={p.id}>{p.nome} — custo atual {brl(p.precoCusto)}</option>)}
          </Selecao>
          <Entrada type="number" min={1} value={quantidade} onChange={(e) => setQuantidade(Number(e.target.value))}
            aria-label="Quantidade" className="entrada-curta" />
          <Entrada type="number" min={0} step="0.01" value={precoCusto} onChange={(e) => setPrecoCusto(Number(e.target.value))}
            aria-label="Preço de custo" className="entrada-curta" />
          <Botao variante="secundario" onClick={adicionarItem}>Adicionar item</Botao>
        </div>

        {itens.length === 0 ? (
          <Vazio titulo="Nenhum item adicionado" descricao="Selecione produto, quantidade e custo para montar a compra." />
        ) : (
          <div className="tabela-scroll">
            <table className="tabela tabela-compacta">
              <thead>
                <tr><th>Produto</th><th className="direita">Qtd.</th><th className="direita">Custo unit.</th><th className="direita">Subtotal</th><th /></tr>
              </thead>
              <tbody>
                {itens.map((item) => (
                  <tr key={item.produtoId}>
                    <td>{item.nome}</td>
                    <td className="direita">{numero(item.quantidade)}</td>
                    <td className="direita">{brl(item.precoCusto)}</td>
                    <td className="direita">{brl(item.quantidade * item.precoCusto)}</td>
                    <td className="direita">
                      <Botao variante="fantasma" className="texto-perigo"
                        onClick={() => setItens((atuais) => atuais.filter((i) => i.produtoId !== item.produtoId))}>
                        Remover
                      </Botao>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="modal-acoes">
          <Botao variante="secundario" onClick={() => setItens([])} disabled={itens.length === 0}>Limpar itens</Botao>
          <Botao onClick={() => void salvar()} carregando={salvando} disabled={itens.length === 0 || !fornecedorId}>
            Registrar compra · {brl(total)}
          </Botao>
        </div>
      </section>

      <section className="painel">
        <header className="painel-topo"><h2>Compras registradas</h2></header>
        {erro && <Alerta tipo="erro">{erro}</Alerta>}
        {carregando ? <Spinner texto="Carregando compras..." /> : compras.length === 0 ? (
          <Vazio titulo="Nenhuma compra registrada" />
        ) : (
          <div className="tabela-scroll">
            <table className="tabela">
              <thead>
                <tr>
                  <th>#</th><th>Data</th><th>Fornecedor</th><th>Nota</th>
                  <th className="direita">Itens</th><th className="direita">Total</th>
                  <th>Status</th><th className="direita">Ações</th>
                </tr>
              </thead>
              <tbody>
                {compras.map((compra) => (
                  <tr key={compra.id}>
                    <td className="mono">{compra.id}</td>
                    <td>{dataHora(compra.dataCompra)}</td>
                    <td>{compra.fornecedorNome}</td>
                    <td>{compra.numeroNota ?? '—'}</td>
                    <td className="direita">{compra.itens.length}</td>
                    <td className="direita"><strong>{brl(compra.valorTotal)}</strong></td>
                    <td><Etiqueta tom={compra.status === 'Concluida' ? 'verde' : 'vermelho'}>{compra.status}</Etiqueta></td>
                    <td className="direita">
                      {compra.status === 'Concluida' && (
                        <Botao variante="fantasma" className="texto-perigo" onClick={() => void cancelar(compra)}>Cancelar</Botao>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </>
  );
}

