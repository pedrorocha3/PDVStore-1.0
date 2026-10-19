import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Alerta, Botao, CabecalhoPagina, Campo, Entrada, Etiqueta, Paginacao, Selecao, Spinner, Vazio } from '../components/ui';
import { estoqueService } from '../services/estoque';
import { produtosService } from '../services/produtos';
import { ApiError } from '../services/api';
import { useToast } from '../hooks/useToast';
import type { MovimentacaoEstoque, Produto } from '../types/api';
import { dataHora, numero } from '../utils/format';

export function Estoque() {
  const { sucesso, erro: avisarErro } = useToast();
  const [produtos, setProdutos] = useState<Produto[]>([]);
  const [alertas, setAlertas] = useState<Produto[]>([]);
  const [movimentos, setMovimentos] = useState<MovimentacaoEstoque[]>([]);
  const [pagina, setPagina] = useState(1);
  const [total, setTotal] = useState(0);
  const [totalPaginas, setTotalPaginas] = useState(0);
  const [tipoFiltro, setTipoFiltro] = useState('');
  const [inicio, setInicio] = useState('');
  const [fim, setFim] = useState('');
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');
  const [produtoId, setProdutoId] = useState('');
  const [quantidade, setQuantidade] = useState(1);
  const [motivo, setMotivo] = useState('');
  const [tipo, setTipo] = useState<'entrada' | 'saida'>('entrada');
  const [salvando, setSalvando] = useState(false);

  const carregarMovimentos = useCallback(async () => {
    setCarregando(true);
    setErro('');
    try {
      const resultado = await estoqueService.movimentacoes({
        tipo: tipoFiltro || undefined,
        inicio: inicio ? `${inicio}T00:00:00Z` : undefined,
        fim: fim ? `${fim}T23:59:59Z` : undefined,
        page: pagina,
        pageSize: 15,
      });
      setMovimentos(resultado.items);
      setTotal(resultado.totalItems);
      setTotalPaginas(resultado.totalPages);
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível carregar as movimentações.');
    } finally { setCarregando(false); }
  }, [tipoFiltro, inicio, fim, pagina]);

  const carregarProdutos = useCallback(async () => {
    try {
      const [lista, baixos] = await Promise.all([
        produtosService.listar({ ativo: true, pageSize: 100, ordenarPor: 'nome' }),
        estoqueService.alertas(),
      ]);
      setProdutos(lista.items);
      setAlertas(baixos);
    } catch (e) { avisarErro(e instanceof ApiError ? e.message : 'Não foi possível carregar produtos.'); }
  }, [avisarErro]);

  useEffect(() => { void carregarProdutos(); }, [carregarProdutos]);
  useEffect(() => { void carregarMovimentos(); }, [carregarMovimentos]);

  async function movimentar(evento: FormEvent) {
    evento.preventDefault();
    if (!produtoId) { avisarErro('Selecione um produto.'); return; }
    if (quantidade <= 0) { avisarErro('Quantidade deve ser maior que zero.'); return; }
    setSalvando(true);
    try {
      if (tipo === 'entrada') await estoqueService.entrada(Number(produtoId), quantidade, motivo || undefined);
      else await estoqueService.saida(Number(produtoId), quantidade, motivo || undefined);
      sucesso(tipo === 'entrada' ? 'Entrada de estoque registrada.' : 'Saída de estoque registrada.');
      setQuantidade(1); setMotivo(''); setPagina(1);
      await Promise.all([carregarMovimentos(), carregarProdutos()]);
    } catch (e) { avisarErro(e instanceof ApiError ? e.message : 'Não foi possível movimentar o estoque.'); }
    finally { setSalvando(false); }
  }

  return (
    <>
      <CabecalhoPagina titulo="Estoque" descricao="Ajustes manuais com histórico auditável. Vendas e compras geram movimentações automaticamente." />
      <div className="grade-2">
        <section className="painel">
          <header className="painel-topo"><h2>Nova movimentação</h2></header>
          <form className="formulario" onSubmit={movimentar}>
            <Campo label="Tipo" obrigatorio>
              <Selecao value={tipo} onChange={(e) => setTipo(e.target.value as 'entrada' | 'saida')}>
                <option value="entrada">Entrada (aumenta o saldo)</option>
                <option value="saida">Saída (reduz o saldo)</option>
              </Selecao>
            </Campo>
            <Campo label="Produto" obrigatorio>
              <Selecao value={produtoId} onChange={(e) => setProdutoId(e.target.value)}>
                <option value="">Selecione...</option>
                {produtos.map((p) => <option key={p.id} value={p.id}>{p.nome} — saldo {p.estoque}</option>)}
              </Selecao>
            </Campo>
            <div className="grade-2-form">
              <Campo label="Quantidade" obrigatorio>
                <Entrada type="number" min={1} value={quantidade} onChange={(e) => setQuantidade(Number(e.target.value))} />
              </Campo>
              <Campo label="Motivo">
                <Entrada value={motivo} onChange={(e) => setMotivo(e.target.value)} placeholder="Inventário, perda, devolução..." maxLength={300} />
              </Campo>
            </div>
            <Botao type="submit" carregando={salvando}>{tipo === 'entrada' ? 'Registrar entrada' : 'Registrar saída'}</Botao>
          </form>
        </section>
        <section className="painel">
          <header className="painel-topo"><h2>Reposição necessária</h2><Etiqueta tom={alertas.length ? 'amarelo' : 'verde'}>{alertas.length}</Etiqueta></header>
          {alertas.length === 0 ? <Vazio titulo="Estoque saudável" /> : (
            <ul className="lista-alertas">
              {alertas.map((p) => (
                <li key={p.id}><div><strong>{p.nome}</strong><small>{p.categoria ?? 'Sem categoria'}</small></div><Etiqueta tom="vermelho">{p.estoque} / mín. {p.estoqueMinimo}</Etiqueta></li>
              ))}
            </ul>
          )}
        </section>
      </div>
      <section className="painel">
        <header className="painel-topo">
          <h2>Histórico de movimentações</h2>
          <div className="filtro-periodo">
            <Entrada type="date" value={inicio} onChange={(e) => { setInicio(e.target.value); setPagina(1); }} aria-label="Início" />
            <span>até</span>
            <Entrada type="date" value={fim} onChange={(e) => { setFim(e.target.value); setPagina(1); }} aria-label="Fim" />
            <Selecao value={tipoFiltro} onChange={(e) => { setTipoFiltro(e.target.value); setPagina(1); }}>
              <option value="">Todos os tipos</option><option value="Entrada">Entradas</option><option value="Saída">Saídas</option>
            </Selecao>
          </div>
        </header>
        {erro && <Alerta tipo="erro">{erro}</Alerta>}
        {carregando ? <Spinner texto="Carregando movimentações..." /> : movimentos.length === 0 ? (
          <Vazio titulo="Sem movimentações" descricao="Nenhum registro no período selecionado." />
        ) : (
          <div className="tabela-scroll">
            <table className="tabela">
              <thead><tr><th>Data</th><th>Produto</th><th>Tipo</th><th className="direita">Qtd.</th><th>Motivo</th><th>Usuário</th></tr></thead>
              <tbody>
                {movimentos.map((m) => (
                  <tr key={m.id}>
                    <td>{dataHora(m.dataMovimentacao)}</td><td>{m.produtoNome}</td>
                    <td><Etiqueta tom={m.tipo === 'Entrada' ? 'verde' : 'amarelo'}>{m.tipo}</Etiqueta></td>
                    <td className="direita">{numero(m.quantidade)}</td><td>{m.motivo ?? '—'}</td><td className="texto-suave">{m.usuarioNome ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        <Paginacao page={pagina} totalPages={totalPaginas} totalItems={total} onPage={setPagina} />
      </section>
    </>
  );
}
