import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Alerta, Botao, CabecalhoPagina, Entrada, Etiqueta, Paginacao, Selecao, Spinner, Vazio } from '../components/ui';
import { ProdutoForm } from '../components/produtos/ProdutoForm';
import { produtosService } from '../services/produtos';
import { ApiError } from '../services/api';
import { useToast } from '../hooks/useToast';
import type { Produto } from '../types/api';
import { brl, numero } from '../utils/format';

export function Produtos() {
  const { sucesso, erro: avisarErro } = useToast();
  const [busca, setBusca] = useState('');
  const [buscaAplicada, setBuscaAplicada] = useState('');
  const [situacao, setSituacao] = useState<'true' | 'false' | 'todos'>('true');
  const [pagina, setPagina] = useState(1);
  const [produtos, setProdutos] = useState<Produto[]>([]);
  const [total, setTotal] = useState(0);
  const [totalPaginas, setTotalPaginas] = useState(0);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');
  const [modalAberto, setModalAberto] = useState(false);
  const [editando, setEditando] = useState<Produto | null>(null);

  const carregar = useCallback(async () => {
    setCarregando(true);
    setErro('');
    try {
      const resultado = await produtosService.listar({
        busca: buscaAplicada || undefined,
        ativo: situacao === 'todos' ? undefined : situacao === 'true',
        page: pagina,
        pageSize: 15,
        ordenarPor: 'nome',
      });
      setProdutos(resultado.items);
      setTotal(resultado.totalItems);
      setTotalPaginas(resultado.totalPages);
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível carregar os produtos.');
    } finally {
      setCarregando(false);
    }
  }, [buscaAplicada, situacao, pagina]);

  useEffect(() => {
    void carregar();
  }, [carregar]);

  function pesquisar(evento: FormEvent) {
    evento.preventDefault();
    setPagina(1);
    setBuscaAplicada(busca.trim());
  }

  async function desativar(produto: Produto) {
    if (!window.confirm(`Desativar o produto "${produto.nome}"? O histórico é preservado.`)) return;
    try {
      await produtosService.desativar(produto.id);
      sucesso('Produto desativado.');
      await carregar();
    } catch (e) {
      avisarErro(e instanceof ApiError ? e.message : 'Não foi possível desativar o produto.');
    }
  }

  return (
    <>
      <CabecalhoPagina
        titulo="Produtos"
        descricao="Cadastro, preços e níveis de estoque. A exclusão é lógica e preserva o histórico."
        acoes={<Botao onClick={() => { setEditando(null); setModalAberto(true); }}>Novo produto</Botao>}
      />
      <section className="painel">
        <form className="barra-filtros" onSubmit={pesquisar}>
          <Entrada placeholder="Buscar por nome ou código de barras" value={busca}
            onChange={(e) => setBusca(e.target.value)} aria-label="Buscar produtos" />
          <Selecao value={situacao} onChange={(e) => { setSituacao(e.target.value as 'true' | 'false' | 'todos'); setPagina(1); }}>
            <option value="true">Somente ativos</option>
            <option value="false">Somente inativos</option>
            <option value="todos">Todos</option>
          </Selecao>
          <Botao type="submit" variante="secundario">Buscar</Botao>
        </form>
        {erro && <Alerta tipo="erro">{erro}</Alerta>}
        {carregando ? <Spinner texto="Carregando produtos..." /> : produtos.length === 0 ? (
          <Vazio titulo="Nenhum produto encontrado" descricao="Ajuste os filtros ou cadastre um novo produto." />
        ) : (
          <div className="tabela-scroll">
            <table className="tabela">
              <thead><tr><th>Código</th><th>Produto</th><th className="direita">Venda</th><th className="direita">Custo</th><th className="direita">Estoque</th><th>Situação</th><th className="direita">Ações</th></tr></thead>
              <tbody>
                {produtos.map((produto) => {
                  const baixo = produto.estoque <= produto.estoqueMinimo;
                  return (
                    <tr key={produto.id}>
                      <td className="mono">{produto.codigoBarras ?? '—'}</td>
                      <td><strong>{produto.nome}</strong><small className="bloco texto-suave">{produto.categoria ?? 'Sem categoria'}</small></td>
                      <td className="direita">{brl(produto.preco)}</td>
                      <td className="direita texto-suave">{brl(produto.precoCusto)}</td>
                      <td className="direita"><Etiqueta tom={baixo ? 'vermelho' : 'verde'}>{numero(produto.estoque)} un.</Etiqueta><small className="bloco texto-suave">mín. {produto.estoqueMinimo}</small></td>
                      <td><Etiqueta tom={produto.ativo ? 'azul' : 'neutro'}>{produto.ativo ? 'Ativo' : 'Inativo'}</Etiqueta></td>
                      <td className="direita acoes-tabela">
                        <Botao variante="fantasma" onClick={() => { setEditando(produto); setModalAberto(true); }}>Editar</Botao>
                        {produto.ativo && <Botao variante="fantasma" className="texto-perigo" onClick={() => void desativar(produto)}>Desativar</Botao>}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
        <Paginacao page={pagina} totalPages={totalPaginas} totalItems={total} onPage={setPagina} />
      </section>
      {modalAberto && (
        <ProdutoForm produto={editando} onFechar={() => setModalAberto(false)} onSalvo={() => { setModalAberto(false); void carregar(); }} />
      )}
    </>
  );
}
