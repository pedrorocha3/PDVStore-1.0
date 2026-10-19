import { useCallback, useEffect, useState } from 'react';
import { Alerta, Botao, CabecalhoPagina, Entrada, Etiqueta, Spinner, Vazio } from '../components/ui';
import { relatoriosService } from '../services/relatorios';
import { ApiError } from '../services/api';
import type { Dashboard as DashboardDados } from '../types/api';
import { brl, data, hoje, inicioDoMes, numero } from '../utils/format';

export function Dashboard() {
  const [inicio, setInicio] = useState(inicioDoMes());
  const [fim, setFim] = useState(hoje());
  const [dados, setDados] = useState<DashboardDados | null>(null);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');

  const carregar = useCallback(async () => {
    setCarregando(true);
    setErro('');
    try {
      setDados(await relatoriosService.dashboard(
        inicio ? `${inicio}T00:00:00Z` : undefined,
        fim ? `${fim}T23:59:59Z` : undefined,
      ));
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível carregar o dashboard.');
    } finally {
      setCarregando(false);
    }
  }, [inicio, fim]);

  useEffect(() => {
    void carregar();
  }, [carregar]);

  const ticketMedio = dados && dados.quantidadeVendas > 0 ? dados.totalPeriodo / dados.quantidadeVendas : 0;
  const maiorVendaDia = Math.max(1, ...(dados?.vendasPorDia.map((d) => d.total) ?? [1]));

  return (
    <>
      <CabecalhoPagina
        titulo="Dashboard"
        descricao="Visão do período com faturamento, vendas, pagamentos e alertas de estoque."
        acoes={
          <div className="filtro-periodo">
            <Entrada type="date" value={inicio} onChange={(e) => setInicio(e.target.value)} aria-label="Data inicial" />
            <span>até</span>
            <Entrada type="date" value={fim} onChange={(e) => setFim(e.target.value)} aria-label="Data final" />
            <Botao variante="secundario" onClick={() => void carregar()} disabled={carregando}>Atualizar</Botao>
          </div>
        }
      />

      {erro && <Alerta tipo="erro">{erro}</Alerta>}
      {carregando && !dados ? <Spinner texto="Carregando indicadores..." /> : null}

      {dados && (
        <>
          <div className="cards-resumo">
            <article className="card-indicador destaque">
              <span>Faturamento</span>
              <strong>{brl(dados.totalPeriodo)}</strong>
              <small>Vendas concluídas no período</small>
            </article>
            <article className="card-indicador">
              <span>Vendas realizadas</span>
              <strong>{numero(dados.quantidadeVendas)}</strong>
              <small>Ticket médio de {brl(ticketMedio)}</small>
            </article>
            <article className="card-indicador">
              <span>Alertas de estoque</span>
              <strong>{numero(dados.alertasEstoque.length)}</strong>
              <small>Produtos no estoque mínimo</small>
            </article>
            <article className="card-indicador">
              <span>Itens no ranking</span>
              <strong>{numero(dados.itensMaisVendidos.length)}</strong>
              <small>Produtos mais vendidos</small>
            </article>
          </div>

          <div className="grade-2">
            <section className="painel">
              <header className="painel-topo">
                <h2>Faturamento por dia</h2>
                <span className="texto-suave">{data(dados.inicio)} — {data(dados.fim)}</span>
              </header>
              {dados.vendasPorDia.length === 0 ? (
                <Vazio titulo="Nenhuma venda concluída" descricao="As vendas do período aparecerão aqui." />
              ) : (
                <div className="grafico-barras">
                  {dados.vendasPorDia.map((dia) => (
                    <div className="barra-linha" key={dia.dia}>
                      <span className="barra-rotulo">{new Date(dia.dia).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' })}</span>
                      <div className="barra-trilho">
                        <div className="barra-preenchida" style={{ width: `${Math.max(4, (dia.total / maiorVendaDia) * 100)}%` }} />
                      </div>
                      <span className="barra-valor">{brl(dia.total)}</span>
                    </div>
                  ))}
                </div>
              )}
            </section>

            <section className="painel">
              <header className="painel-topo">
                <h2>Formas de pagamento</h2>
              </header>
              {dados.pagamentosPorForma.length === 0 ? (
                <Vazio titulo="Sem pagamentos no período" />
              ) : (
                <table className="tabela tabela-compacta">
                  <thead>
                    <tr><th>Forma</th><th className="direita">Qtd.</th><th className="direita">Total</th></tr>
                  </thead>
                  <tbody>
                    {dados.pagamentosPorForma.map((forma) => (
                      <tr key={forma.formaPagamento}>
                        <td>{forma.formaPagamento}</td>
                        <td className="direita">{numero(forma.quantidade)}</td>
                        <td className="direita">{brl(forma.total)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </section>
          </div>


          <div className="grade-2">
            <section className="painel">
              <header className="painel-topo">
                <h2>Produtos mais vendidos</h2>
              </header>
              {dados.itensMaisVendidos.length === 0 ? (
                <Vazio titulo="Nenhum item vendido" descricao="O ranking é calculado apenas com vendas concluídas." />
              ) : (
                <table className="tabela tabela-compacta">
                  <thead>
                    <tr>
                      <th>Produto</th>
                      <th className="direita">Vendidos</th>
                      <th className="direita">Valor</th>
                      <th>Estoque</th>
                    </tr>
                  </thead>
                  <tbody>
                    {dados.itensMaisVendidos.map((item) => (
                      <tr key={item.nomeProduto}>
                        <td>
                          <strong>{item.nomeProduto}</strong>
                          <small className="bloco texto-suave">{item.codigoBarras ?? 'sem código'}</small>
                        </td>
                        <td className="direita">{numero(item.totalVendido)}</td>
                        <td className="direita">{brl(item.valorTotalVendido)}</td>
                        <td>
                          {item.statusMinimo === 'Baixo'
                            ? <Etiqueta tom="vermelho">{item.estoqueAtual} un.</Etiqueta>
                            : <Etiqueta tom="verde">{item.estoqueAtual} un.</Etiqueta>}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </section>

            <section className="painel">
              <header className="painel-topo">
                <h2>Reposição necessária</h2>
                <Etiqueta tom={dados.alertasEstoque.length ? 'amarelo' : 'verde'}>
                  {dados.alertasEstoque.length} alerta(s)
                </Etiqueta>
              </header>
              {dados.alertasEstoque.length === 0 ? (
                <Vazio titulo="Estoque saudável" descricao="Nenhum produto ativo está no estoque mínimo." />
              ) : (
                <ul className="lista-alertas">
                  {dados.alertasEstoque.map((produto) => (
                    <li key={produto.id}>
                      <div>
                        <strong>{produto.nome}</strong>
                        <small>{produto.categoria ?? 'Sem categoria'}</small>
                      </div>
                      <Etiqueta tom="vermelho">{produto.estoque} / mín. {produto.estoqueMinimo}</Etiqueta>
                    </li>
                  ))}
                </ul>
              )}
            </section>
          </div>
        </>
      )}
    </>
  );
}
