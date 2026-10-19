import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Alerta, Botao, CabecalhoPagina, Entrada, Etiqueta, Paginacao, Selecao, Spinner, Vazio } from '../components/ui';
import { ClienteForm } from '../components/clientes/ClienteForm';
import { clientesService } from '../services/clientes';
import { ApiError } from '../services/api';
import { useToast } from '../hooks/useToast';
import type { Cliente } from '../types/api';
import { brl } from '../utils/format';

export function Clientes() {
  const { sucesso, erro: avisarErro } = useToast();
  const [busca, setBusca] = useState('');
  const [buscaAplicada, setBuscaAplicada] = useState('');
  const [situacao, setSituacao] = useState<'true' | 'false' | 'todos'>('true');
  const [pagina, setPagina] = useState(1);
  const [clientes, setClientes] = useState<Cliente[]>([]);
  const [total, setTotal] = useState(0);
  const [totalPaginas, setTotalPaginas] = useState(0);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');
  const [modalAberto, setModalAberto] = useState(false);
  const [editando, setEditando] = useState<Cliente | null>(null);

  const carregar = useCallback(async () => {
    setCarregando(true);
    setErro('');
    try {
      const r = await clientesService.listar({
        busca: buscaAplicada || undefined,
        ativo: situacao === 'todos' ? undefined : situacao === 'true',
        page: pagina,
        pageSize: 15,
      });
      setClientes(r.items);
      setTotal(r.totalItems);
      setTotalPaginas(r.totalPages);
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível carregar os clientes.');
    } finally {
      setCarregando(false);
    }
  }, [buscaAplicada, situacao, pagina]);

  useEffect(() => { void carregar(); }, [carregar]);

  function pesquisar(evento: FormEvent) {
    evento.preventDefault();
    setPagina(1);
    setBuscaAplicada(busca.trim());
  }

  async function alternarStatus(cliente: Cliente) {
    try {
      await clientesService.status(cliente.id, !cliente.ativo);
      sucesso(cliente.ativo ? 'Cliente desativado.' : 'Cliente reativado.');
      await carregar();
    } catch (e) {
      avisarErro(e instanceof ApiError ? e.message : 'Não foi possível alterar o status.');
    }
  }

  async function receberFiado(cliente: Cliente) {
    const entrada = window.prompt(`Valor recebido de ${cliente.nome} (saldo ${brl(cliente.saldoDevedor)}):`, '');
    if (entrada === null) return;
    const valor = Number(entrada.replace(',', '.'));
    if (!Number.isFinite(valor) || valor <= 0) { avisarErro('Informe um valor maior que zero.'); return; }
    try {
      const atualizado = await clientesService.receberFiado(cliente.id, valor);
      sucesso(`Fiado recebido. Novo saldo: ${brl(atualizado.saldoDevedor)}.`);
      await carregar();
    } catch (e) {
      avisarErro(e instanceof ApiError ? e.message : 'Não foi possível baixar o fiado.');
    }
  }

  return (
    <>
      <CabecalhoPagina
        titulo="Clientes"
        descricao="Cadastro, limite de crédito e controle de fiado."
        acoes={<Botao onClick={() => { setEditando(null); setModalAberto(true); }}>Novo cliente</Botao>}
      />
      <section className="painel">
        <form className="barra-filtros" onSubmit={pesquisar}>
          <Entrada placeholder="Buscar por nome ou CPF/CNPJ" value={busca}
            onChange={(e) => setBusca(e.target.value)} aria-label="Buscar clientes" />
          <Selecao value={situacao} onChange={(e) => { setSituacao(e.target.value as 'true' | 'false' | 'todos'); setPagina(1); }}>
            <option value="true">Somente ativos</option>
            <option value="false">Somente inativos</option>
            <option value="todos">Todos</option>
          </Selecao>
          <Botao type="submit" variante="secundario">Buscar</Botao>
        </form>
        {erro && <Alerta tipo="erro">{erro}</Alerta>}
        {carregando ? <Spinner texto="Carregando clientes..." /> : clientes.length === 0 ? (
          <Vazio titulo="Nenhum cliente encontrado" descricao="Cadastre um cliente ou ajuste os filtros." />
        ) : (
          <div className="tabela-scroll">
            <table className="tabela">
              <thead><tr><th>Cliente</th><th>Contato</th><th className="direita">Limite</th><th className="direita">Saldo devedor</th><th>Status</th><th className="direita">Ações</th></tr></thead>
              <tbody>
                {clientes.map((cliente) => (
                  <tr key={cliente.id}>
                    <td><strong>{cliente.nome}</strong><small className="bloco texto-suave">{cliente.cpfCnpj ?? 'sem CPF/CNPJ'}</small></td>
                    <td><small className="bloco">{cliente.telefone ?? '—'}</small><small className="bloco texto-suave">{cliente.email ?? ''}</small></td>
                    <td className="direita">{cliente.limiteCredito > 0 ? brl(cliente.limiteCredito) : 'sem limite'}</td>
                    <td className="direita"><Etiqueta tom={cliente.saldoDevedor > 0 ? 'amarelo' : 'verde'}>{brl(cliente.saldoDevedor)}</Etiqueta></td>
                    <td><Etiqueta tom={cliente.ativo ? 'azul' : 'neutro'}>{cliente.ativo ? 'Ativo' : 'Inativo'}</Etiqueta></td>
                    <td className="direita acoes-tabela">
                      <Botao variante="fantasma" onClick={() => { setEditando(cliente); setModalAberto(true); }}>Editar</Botao>
                      {cliente.saldoDevedor > 0 && <Botao variante="fantasma" onClick={() => void receberFiado(cliente)}>Receber fiado</Botao>}
                      <Botao variante="fantasma" className={cliente.ativo ? 'texto-perigo' : ''} onClick={() => void alternarStatus(cliente)}>
                        {cliente.ativo ? 'Desativar' : 'Reativar'}
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
      {modalAberto && (
        <ClienteForm cliente={editando} onFechar={() => setModalAberto(false)} onSalvo={() => { setModalAberto(false); void carregar(); }} />
      )}
    </>
  );
}
