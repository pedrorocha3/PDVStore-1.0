import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alerta, Botao, Campo, Entrada, Modal } from '../ui';
import { ApiError } from '../../services/api';
import { clientesService } from '../../services/clientes';
import { useToast } from '../../hooks/useToast';
import type { Cliente, ClientePayload } from '../../types/api';
import { brl } from '../../utils/format';

export function ClienteForm({ cliente, onFechar, onSalvo }: {
  cliente: Cliente | null;
  onFechar: () => void;
  onSalvo: () => void;
}) {
  const { sucesso } = useToast();
  const [form, setForm] = useState<ClientePayload>({
    nome: cliente?.nome ?? '',
    cpfCnpj: cliente?.cpfCnpj ?? '',
    telefone: cliente?.telefone ?? '',
    email: cliente?.email ?? '',
    endereco: cliente?.endereco ?? '',
    limiteCredito: cliente?.limiteCredito ?? 0,
  });
  const [erros, setErros] = useState<Record<string, string>>({});
  const [erroApi, setErroApi] = useState('');
  const [salvando, setSalvando] = useState(false);

  function alterar<K extends keyof ClientePayload>(campo: K, valor: ClientePayload[K]) {
    setForm((atual) => ({ ...atual, [campo]: valor }));
    setErros((atuais) => ({ ...atuais, [campo]: '' }));
  }

  async function salvar(evento: FormEvent) {
    evento.preventDefault();
    const novos: Record<string, string> = {};
    if (!form.nome.trim()) novos.nome = 'Nome do cliente é obrigatório.';
    if (form.limiteCredito < 0) novos.limiteCredito = 'Limite de crédito não pode ser negativo.';
    setErros(novos);
    if (Object.keys(novos).length) return;

    setSalvando(true);
    setErroApi('');
    try {
      const payload = {
        ...form,
        nome: form.nome.trim(),
        cpfCnpj: form.cpfCnpj?.trim(),
        telefone: form.telefone?.trim(),
        email: form.email?.trim(),
        endereco: form.endereco?.trim(),
      };
      if (cliente) {
        await clientesService.atualizar(cliente.id, payload);
        sucesso('Cliente atualizado.');
      } else {
        await clientesService.criar(payload);
        sucesso('Cliente cadastrado.');
      }
      onSalvo();
    } catch (e) {
      setErroApi(e instanceof ApiError ? [e.message, ...(e.details ?? [])].join(' ') : 'Erro ao salvar cliente.');
    } finally {
      setSalvando(false);
    }
  }

  return (
    <Modal titulo={cliente ? `Editar cliente #${cliente.id}` : 'Novo cliente'} onFechar={onFechar} largura="720px">
      <form className="formulario" onSubmit={salvar}>
        {erroApi && <Alerta tipo="erro">{erroApi}</Alerta>}

        <Campo label="Nome" obrigatorio erro={erros.nome}>
          <Entrada value={form.nome} onChange={(e) => alterar('nome', e.target.value)} maxLength={200} autoFocus />
        </Campo>

        <div className="grade-2-form">
          <Campo label="CPF / CNPJ">
            <Entrada value={form.cpfCnpj ?? ''} onChange={(e) => alterar('cpfCnpj', e.target.value)} maxLength={20} />
          </Campo>
          <Campo label="Telefone">
            <Entrada value={form.telefone ?? ''} onChange={(e) => alterar('telefone', e.target.value)} maxLength={30} />
          </Campo>
        </div>

        <div className="grade-2-form">
          <Campo label="E-mail">
            <Entrada type="email" value={form.email ?? ''} onChange={(e) => alterar('email', e.target.value)} maxLength={200} />
          </Campo>
          <Campo label="Limite de crédito (R$)" erro={erros.limiteCredito}
            dica="Limite 0 significa fiado sem limite, como no PDVStore.">
            <Entrada type="number" min={0} step="0.01" value={form.limiteCredito}
              onChange={(e) => alterar('limiteCredito', Number(e.target.value))} />
          </Campo>
        </div>

        <Campo label="Endereço">
          <Entrada value={form.endereco ?? ''} onChange={(e) => alterar('endereco', e.target.value)} maxLength={300} />
        </Campo>

        {cliente && (
          <div className="bloco-informacao">
            Saldo devedor atual: <strong>{brl(cliente.saldoDevedor)}</strong> — use “Receber fiado” para baixar.
          </div>
        )}

        <div className="modal-acoes">
          <Botao variante="secundario" type="button" onClick={onFechar}>Cancelar</Botao>
          <Botao type="submit" carregando={salvando}>{cliente ? 'Salvar alterações' : 'Cadastrar cliente'}</Botao>
        </div>
      </form>
    </Modal>
  );
}
