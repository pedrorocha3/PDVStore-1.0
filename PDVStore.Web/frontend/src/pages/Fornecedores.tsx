import { useCallback, useEffect, useState } from 'react';
import { Alerta, Botao, CabecalhoPagina, Entrada, Etiqueta, Paginacao, Selecao, Spinner, Vazio } from '../components/ui';
import { FornecedorForm } from '../components/fornecedores/FornecedorForm';
import { fornecedoresService } from '../services/fornecedores';
import { ApiError } from '../services/api';
import { useToast } from '../hooks/useToast';
import type { Fornecedor } from '../types/api';

export function Fornecedores() {
  const { sucesso, erro: avisarErro } = useToast();
  const [busca, setBusca] = useState('');
  const [buscaAplicada, setBuscaAplicada] = useState('');
  const [situacao, setSituacao] = useState<'true' | 'false' | 'todos'>('true');
  const [pagina, setPagina] = useState(1);
  const [fornecedores, setFornecedores] = useState<Fornecedor[]>([]);
  const [total, setTotal] = useState(0);
  const [totalPaginas, setTotalPaginas] = useState(0);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');
  const [editando, setEditando] = useState<Fornecedor | null>(null);
  const [formAberto, setFormAberto] = useState(false);

  const carregar = useCallback(async () => {
    setCarregando(true);
    setErro('');
    try {
      const r = await fornecedoresService.listar({
        busca: buscaAplicada || undefined,
        ativo: situacao === 'todos' ? undefined : situacao === 'true',
        page: pagina,
        pageSize: 15,
      });
      setFornecedores(r.items);
      setTotal(r.totalItems);
      setTotalPaginas(r.totalPages);
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível carregar os fornecedores.');
    } finally {
      setCarregando(false);
    }
  }, [buscaAplicada, situacao, pagina]);

  useEffect(() => { void carregar(); }, [carregar]);

  async function alternarStatus(fornecedor: Fornecedor) {
    try {
      await fornecedoresService.status(fornecedor.id, !fornecedor.ativo);
      sucesso(fornecedor.ativo ? 'Fornecedor desativado.' : 'Fornecedor reativado.');
      await carregar();
    } catch (e) {
      avisarErro(e instanceof ApiError ? e.message : 'Não foi possível alterar o status.');
    }
  }

  return (
    <>
      <CabecalhoPagina
        titulo="Fornecedores"
        descricao="Cadastro usado nas compras de reposição de estoque."
        acoes={<Botao onClick={() => { setEditando(null); setFormAberto(true); }}>Novo fornecedor</Botao>}
      />

      <section className="painel">
        <form className="barra-filtros" onSubmit={(e) => { e.preventDefault(); setPagina(1); setBuscaAplicada(busca.trim()); }}>
          <Entrada placeholder="Buscar por nome ou CNPJ" value={busca}
            onChange={(e) => setBusca(e.target.value)} aria-label="Buscar fornecedores" />
          <Selecao value={situacao} onChange={(e) => { setSituacao(e.target.value as 'true' | 'false' | 'todos'); setPagina(1); }}>
            <option value="true">Somente ativos</option>
            <option value="false">Somente inativos</option>
            <option value="todos">Todos</option>
          </Selecao>
          <Botao type="submit" variante="secundario">Buscar</Botao>
        </form>

        {erro && <Alerta tipo="erro">{erro}</Alerta>}

        {carregando ? <Spinner texto="Carregando fornecedores..." /> : fornecedores.length === 0 ? (
          <Vazio titulo="Nenhum fornecedor encontrado" descricao="Cadastre o fornecedor para registrar compras." />
        ) : (
          <div className="tabela-scroll">
            <table className="tabela">
              <thead>
                <tr><th>Fornecedor</th><th>CNPJ</th><th>Contato</th><th>Status</th><th className="direita">Ações</th></tr>
              </thead>
              <tbody>
                {fornecedores.map((fornecedor) => (
                  <tr key={fornecedor.id}>
                    <td><strong>{fornecedor.nome}</strong></td>
                    <td className="mono">{fornecedor.cnpj ?? '—'}</td>
                    <td>
                      <small className="bloco">{fornecedor.telefone ?? '—'}</small>
                      <small className="bloco texto-suave">{fornecedor.email ?? ''}</small>
                    </td>
                    <td><Etiqueta tom={fornecedor.ativo ? 'azul' : 'neutro'}>{fornecedor.ativo ? 'Ativo' : 'Inativo'}</Etiqueta></td>
                    <td className="direita acoes-tabela">
                      <Botao variante="fantasma" onClick={() => { setEditando(fornecedor); setFormAberto(true); }}>Editar</Botao>
                      <Botao variante="fantasma" className={fornecedor.ativo ? 'texto-perigo' : ''} onClick={() => void alternarStatus(fornecedor)}>
                        {fornecedor.ativo ? 'Desativar' : 'Reativar'}
                      </Botao>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <Paginacao page={pagina} totalPages={totalPaginas} totalItems={total} onPage={setPagina} />
      </section>

      {formAberto && (
        <FornecedorForm fornecedor={editando} onFechar={() => setFormAberto(false)}
          onSalvo={() => { setFormAberto(false); void carregar(); }} />
      )}
    </>
  );
}
