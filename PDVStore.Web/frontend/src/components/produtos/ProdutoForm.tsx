import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alerta, Botao, Campo, Entrada, Modal } from '../ui';
import { ApiError } from '../../services/api';
import { produtosService } from '../../services/produtos';
import { useToast } from '../../hooks/useToast';
import type { Produto, ProdutoPayload } from '../../types/api';
import { brl } from '../../utils/format';

interface Props { produto: Produto | null; onFechar: () => void; onSalvo: () => void; }

export function ProdutoForm({ produto, onFechar, onSalvo }: Props) {
  const { sucesso } = useToast();
  const [form, setForm] = useState<ProdutoPayload>({
    codigoBarras: produto?.codigoBarras ?? '',
    nome: produto?.nome ?? '',
    preco: produto?.preco ?? 0,
    precoCusto: produto?.precoCusto ?? 0,
    estoque: produto?.estoque ?? 0,
    estoqueMinimo: produto?.estoqueMinimo ?? 0,
    categoria: produto?.categoria ?? '',
    descricao: produto?.descricao ?? '',
  });
  const [erros, setErros] = useState<Record<string, string>>({});
  const [erroApi, setErroApi] = useState('');
  const [salvando, setSalvando] = useState(false);

  function alterar<K extends keyof ProdutoPayload>(campo: K, valor: ProdutoPayload[K]) {
    setForm((atual) => ({ ...atual, [campo]: valor }));
    setErros((atuais) => ({ ...atuais, [campo]: '' }));
  }

  function validar(): boolean {
    const novos: Record<string, string> = {};
    if (!form.codigoBarras.trim()) novos.codigoBarras = 'Código de barras é obrigatório.';
    if (!form.nome.trim()) novos.nome = 'Nome do produto é obrigatório.';
    if (form.preco < 0) novos.preco = 'Preço de venda não pode ser negativo.';
    if (form.precoCusto < 0) novos.precoCusto = 'Preço de custo não pode ser negativo.';
    if (!produto && (form.estoque ?? 0) < 0) novos.estoque = 'Estoque não pode ser negativo.';
    if (form.estoqueMinimo < 0) novos.estoqueMinimo = 'Estoque mínimo não pode ser negativo.';
    if (!form.categoria.trim()) novos.categoria = 'Categoria é obrigatória.';
    if (!form.descricao.trim()) novos.descricao = 'Descrição é obrigatória.';
    setErros(novos);
    return Object.keys(novos).length === 0;
  }

  async function salvar(evento: FormEvent) {
    evento.preventDefault();
    setErroApi('');
    if (!validar()) return;
    setSalvando(true);
    try {
      const payload: ProdutoPayload = {
        ...form,
        codigoBarras: form.codigoBarras.trim(),
        nome: form.nome.trim(),
        categoria: form.categoria.trim(),
        descricao: form.descricao.trim(),
      };
      if (produto) {
        const atualizacao = { ...payload };
        delete atualizacao.estoque;
        await produtosService.atualizar(produto.id, atualizacao);
        sucesso('Produto atualizado. O estoque não foi alterado.');
      } else {
        await produtosService.criar(payload);
        sucesso('Produto cadastrado com sucesso.');
      }
      onSalvo();
    } catch (e) {
      setErroApi(e instanceof ApiError ? [e.message, ...(e.details ?? [])].join(' ') : 'Erro ao salvar produto.');
    } finally { setSalvando(false); }
  }

  const margem = form.preco > 0 ? ((form.preco - form.precoCusto) / form.preco) * 100 : 0;

  return (
    <Modal titulo={produto ? `Editar produto #${produto.id}` : 'Novo produto'} onFechar={onFechar} largura="760px">
      <form onSubmit={salvar} className="formulario">
        {erroApi && <Alerta tipo="erro">{erroApi}</Alerta>}
        <div className="grade-2-form">
          <Campo label="Código de barras" obrigatorio erro={erros.codigoBarras}>
            <Entrada value={form.codigoBarras} onChange={(e) => alterar('codigoBarras', e.target.value)} maxLength={60} />
          </Campo>
          <Campo label="Categoria" obrigatorio erro={erros.categoria}>
            <Entrada value={form.categoria} onChange={(e) => alterar('categoria', e.target.value)} maxLength={120} />
          </Campo>
        </div>
        <Campo label="Nome" obrigatorio erro={erros.nome}>
          <Entrada value={form.nome} onChange={(e) => alterar('nome', e.target.value)} maxLength={200} />
        </Campo>
        <div className="grade-3-form">
          <Campo label="Preço de venda (R$)" obrigatorio erro={erros.preco}>
            <Entrada type="number" min="0" step="0.01" value={form.preco} onChange={(e) => alterar('preco', Number(e.target.value))} />
          </Campo>
          <Campo label="Preço de custo (R$)" obrigatorio erro={erros.precoCusto}>
            <Entrada type="number" min="0" step="0.01" value={form.precoCusto} onChange={(e) => alterar('precoCusto', Number(e.target.value))} />
          </Campo>
          <Campo label={produto ? 'Estoque atual (somente leitura)' : 'Estoque inicial'} erro={erros.estoque} dica={produto ? 'Use Estoque para movimentar o saldo.' : undefined}>
            <Entrada type="number" min="0" value={form.estoque ?? 0} disabled={Boolean(produto)} onChange={(e) => alterar('estoque', Number(e.target.value))} />
          </Campo>
        </div>
        <div className="grade-2-form">
          <Campo label="Estoque mínimo" obrigatorio erro={erros.estoqueMinimo} dica="Abaixo ou igual a este valor o produto gera alerta.">
            <Entrada type="number" min="0" value={form.estoqueMinimo} onChange={(e) => alterar('estoqueMinimo', Number(e.target.value))} />
          </Campo>
          <div className="campo">
            <span className="campo-label">Margem estimada</span>
            <div className="valor-calculado">{margem.toFixed(1)}% · lucro {brl(form.preco - form.precoCusto)}</div>
          </div>
        </div>
        <Campo label="Descrição" obrigatorio erro={erros.descricao}>
          <Entrada value={form.descricao} onChange={(e) => alterar('descricao', e.target.value)} maxLength={1000} />
        </Campo>
        <div className="modal-acoes">
          <Botao variante="secundario" type="button" onClick={onFechar}>Cancelar</Botao>
          <Botao type="submit" carregando={salvando}>{produto ? 'Salvar alterações' : 'Cadastrar produto'}</Botao>
        </div>
      </form>
    </Modal>
  );
}
