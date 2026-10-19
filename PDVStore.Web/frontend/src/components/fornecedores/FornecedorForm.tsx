import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alerta, Botao, Campo, Entrada, Modal } from '../ui';
import { ApiError } from '../../services/api';
import { fornecedoresService } from '../../services/fornecedores';
import { useToast } from '../../hooks/useToast';
import type { Fornecedor, FornecedorPayload } from '../../types/api';

export function FornecedorForm({ fornecedor, onFechar, onSalvo }: {
  fornecedor: Fornecedor | null;
  onFechar: () => void;
  onSalvo: () => void;
}) {
  const { sucesso } = useToast();
  const [form, setForm] = useState<FornecedorPayload>({
    nome: fornecedor?.nome ?? '',
    cnpj: fornecedor?.cnpj ?? '',
    telefone: fornecedor?.telefone ?? '',
    email: fornecedor?.email ?? '',
  });
  const [erro, setErro] = useState('');
  const [salvando, setSalvando] = useState(false);

  async function salvar(evento: FormEvent) {
    evento.preventDefault();
    if (!form.nome.trim()) { setErro('Nome do fornecedor é obrigatório.'); return; }
    setSalvando(true);
    setErro('');
    try {
      const payload = {
        nome: form.nome.trim(),
        cnpj: form.cnpj?.trim(),
        telefone: form.telefone?.trim(),
        email: form.email?.trim(),
      };
      if (fornecedor) {
        await fornecedoresService.atualizar(fornecedor.id, payload);
        sucesso('Fornecedor atualizado.');
      } else {
        await fornecedoresService.criar(payload);
        sucesso('Fornecedor cadastrado.');
      }
      onSalvo();
    } catch (e) {
      setErro(e instanceof ApiError ? [e.message, ...(e.details ?? [])].join(' ') : 'Erro ao salvar fornecedor.');
    } finally {
      setSalvando(false);
    }
  }

  return (
    <Modal titulo={fornecedor ? `Editar fornecedor #${fornecedor.id}` : 'Novo fornecedor'} onFechar={onFechar}>
      <form className="formulario" onSubmit={salvar}>
        {erro && <Alerta tipo="erro">{erro}</Alerta>}
        <Campo label="Nome" obrigatorio>
          <Entrada value={form.nome} onChange={(e) => setForm({ ...form, nome: e.target.value })} maxLength={200} autoFocus />
        </Campo>
        <div className="grade-2-form">
          <Campo label="CNPJ">
            <Entrada value={form.cnpj ?? ''} onChange={(e) => setForm({ ...form, cnpj: e.target.value })} maxLength={20} />
          </Campo>
          <Campo label="Telefone">
            <Entrada value={form.telefone ?? ''} onChange={(e) => setForm({ ...form, telefone: e.target.value })} maxLength={30} />
          </Campo>
        </div>
        <Campo label="E-mail">
          <Entrada type="email" value={form.email ?? ''} onChange={(e) => setForm({ ...form, email: e.target.value })} maxLength={200} />
        </Campo>
        <div className="modal-acoes">
          <Botao variante="secundario" type="button" onClick={onFechar}>Cancelar</Botao>
          <Botao type="submit" carregando={salvando}>{fornecedor ? 'Salvar alterações' : 'Cadastrar fornecedor'}</Botao>
        </div>
      </form>
    </Modal>
  );
}
