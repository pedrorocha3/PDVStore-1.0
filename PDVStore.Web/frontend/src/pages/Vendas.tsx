import { useCallback, useEffect, useState } from 'react';
import { Alerta, Botao, CabecalhoPagina, Entrada, Etiqueta, Modal, Paginacao, Selecao, Spinner, Vazio } from '../components/ui';
import { vendasService } from '../services/vendas';
import { ApiError } from '../services/api';
import { useToast } from '../hooks/useToast';
import type { Venda } from '../types/api';
import { brl, dataHora, hoje, inicioDoMes, numero } from '../utils/format';

export function Vendas() {
  const { sucesso, erro: avisarErro } = useToast();
  const [inicio, setInicio] = useState(inicioDoMes());
  const [fim, setFim] = useState(hoje());
  const [status, setStatus] = useState('');
  const [pagina, setPagina] = useState(1);
  const [vendas, setVendas] = useState<Venda[]>([]);
  const [total, setTotal] = useState(0);
  const [totalPaginas, setTotalPaginas] = useState(0);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');
  const [detalhe, setDetalhe] = useState<Venda | null>(null);

  const carregar = useCallback(async () => {
    setCarregando(true);
    setErro('');
    try {
      const resultado = await vendasService.listar({
        inicio: inicio ? `${inicio}T00:00:00Z` : undefined,
        fim: fim ? `${fim}T23:59:59Z` : undefined,
        status: status || undefined,
        page: pagina,
        pageSize: 15,
      });
      setVendas(resultado.items);
      setTotal(resultado.totalItems);
      setTotalPaginas(resultado.totalPages);
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível carregar as vendas.');
    } finally {
      setCarregando(false);
    }
  }, [inicio, fim, status, pagina]);

  useEffect(() => {
    void carregar();
  }, [carregar]);

  async function cancelar(venda: Venda) {
    const motivo = window.prompt(`Motivo do cancelamento da venda #${venda.id} (opcional):`, 'Cancelamento solicitado');
    if (motivo === null) return;
    if (!window.confirm(`Cancelar a venda #${venda.id}? O estoque será devolvido e o fiado estornado.`)) return;

    try {
      await vendasService.cancelar(venda.id, motivo);
      sucesso(`Venda #${venda.id} cancelada.`);
      setDetalhe(null);
      await carregar();
    } catch (e) {
      avisarErro(e instanceof ApiError ? e.message : 'Não foi possível cancelar a venda.');
    }
  }

  return (
    <>
      <CabecalhoPagina
        titulo="Vendas"
        descricao="Histórico das vendas do período. O cancelamento estorna estoque e saldo devedor."
      />

      <section className="painel">
        <div className="barra-filtros">
          <Entrada type="date" value={inicio} onChange={(e) => { setInicio(e.target.value); setPagina(1); }} aria-label="Início" />
          <span>até</span>
          <Entrada type="date" value={fim} onChange={(e) => { setFim(e.target.value); setPagina(1); }} aria-label="Fim" />
          <Selecao value={status} onChange={(e) => { setStatus(e.target.value); setPagina(1); }}>
            <option value="">Todos os status</option>
            <option value="Concluida">Concluídas</option>
            <option value="Cancelada">Canceladas</option>
          </Selecao>
        </div>

        {erro && <Alerta tipo="erro">{erro}</Alerta>}
        {carregando ? <Spinner texto="Carregando vendas..." /> : vendas.length === 0 ? (
          <Vazio titulo="Nenhuma venda no período" descricao="Ajuste as datas ou registre uma venda no PDV." />
        ) : (
          <div className="tabela-scroll">
            <table className="tabela">
              <thead>
                <tr>
                  <th>#</th><th>Data</th><th>Cliente</th><th>Pagamento</th>
                  <th className="direita">Itens</th><th className="direita">Total</th>
                  <th>Status</th><th className="direita">Ações</th>
                </tr>
              </thead>
              <tbody>
                {vendas.map((venda) => (
                  <tr key={venda.id}>
                    <td className="mono">{venda.id}</td>
                    <td>{dataHora(venda.dataVenda)}</td>
                    <td>{venda.clienteNome ?? <span className="texto-suave">Consumidor</span>}</td>
                    <td>{venda.formaPagamento}</td>
                    <td className="direita">{numero(venda.itens.reduce((s, i) => s + i.quantidade, 0))}</td>
                    <td className="direita"><strong>{brl(venda.valorTotal)}</strong></td>
                    <td>
                      <Etiqueta tom={venda.status === 'Concluida' ? 'verde' : 'vermelho'}>
                        {venda.status ?? '—'}
                      </Etiqueta>
                    </td>
                    <td className="direita acoes-tabela">
                      <Botao variante="fantasma" onClick={() => setDetalhe(venda)}>Detalhes</Botao>
                      {venda.status === 'Concluida' && (
                        <Botao variante="fantasma" className="texto-perigo" onClick={() => void cancelar(venda)}>Cancelar</Botao>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <Paginacao page={pagina} totalPages={totalPaginas} totalItems={total} onPage={setPagina} />
      </section>


      {detalhe && (
        <Modal titulo={`Venda #${detalhe.id}`} onFechar={() => setDetalhe(null)} largura="620px">
          <div className="detalhe-venda">
            <div className="grade-2-form">
              <div><span className="rotulo">Data</span><strong>{dataHora(detalhe.dataVenda)}</strong></div>
              <div><span className="rotulo">Status</span><Etiqueta tom={detalhe.status === 'Concluida' ? 'verde' : 'vermelho'}>{detalhe.status}</Etiqueta></div>
              <div><span className="rotulo">Operador</span><strong>{detalhe.usuarioNome ?? '—'}</strong></div>
              <div><span className="rotulo">Cliente</span><strong>{detalhe.clienteNome ?? 'Consumidor'}</strong></div>
              <div><span className="rotulo">Pagamento</span><strong>{detalhe.formaPagamento}</strong></div>
              <div><span className="rotulo">Caixa</span><strong>#{detalhe.caixaId}</strong></div>
            </div>

            {detalhe.pixTxId && <p className="mono">PIX TxId: {detalhe.pixTxId}</p>}

            <table className="tabela tabela-compacta">
              <thead>
                <tr><th>Produto</th><th className="direita">Qtd.</th><th className="direita">Unit.</th><th className="direita">Subtotal</th></tr>
              </thead>
              <tbody>
                {detalhe.itens.map((item) => (
                  <tr key={item.id}>
                    <td>{item.produtoNome}</td>
                    <td className="direita">{item.quantidade}</td>
                    <td className="direita">{brl(item.precoUnitario)}</td>
                    <td className="direita">{brl(item.subtotal)}</td>
                  </tr>
                ))}
              </tbody>
            </table>

            <div className="comprovante-totais">
              <div><span>Desconto</span><span>{brl(detalhe.desconto)}</span></div>
              <div className="total"><span>Total</span><span>{brl(detalhe.valorTotal)}</span></div>
            </div>
          </div>
        </Modal>
      )}
    </>
  );
}
