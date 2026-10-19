import { useCallback, useEffect, useState } from 'react';
import { Alerta, Botao, CabecalhoPagina, Campo, Entrada, Etiqueta, Spinner, Vazio } from '../components/ui';
import { caixaService } from '../services/caixa';
import { ApiError } from '../services/api';
import { useToast } from '../hooks/useToast';
import type { Caixa as CaixaDados } from '../types/api';
import { brl, dataHora } from '../utils/format';

export function Caixa() {
  const { sucesso, erro: avisarErro } = useToast();
  const [aberto, setAberto] = useState<CaixaDados | null>(null);
  const [historico, setHistorico] = useState<CaixaDados[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');
  const [valorInicial, setValorInicial] = useState(0);
  const [valorSangria, setValorSangria] = useState(0);
  const [operacao, setOperacao] = useState(false);

  const carregar = useCallback(async () => {
    setCarregando(true);
    setErro('');
    try {
      const [atual, lista] = await Promise.all([caixaService.aberto(), caixaService.listar(1, 20)]);
      setAberto(atual ?? null);
      setHistorico(lista.items);
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível carregar o caixa.');
    } finally {
      setCarregando(false);
    }
  }, []);

  useEffect(() => {
    void carregar();
  }, [carregar]);

  async function abrir() {
    setOperacao(true);
    try {
      await caixaService.abrir(valorInicial);
      sucesso('Caixa aberto com sucesso.');
      await carregar();
    } catch (e) {
      avisarErro(e instanceof ApiError ? e.message : 'Não foi possível abrir o caixa.');
    } finally {
      setOperacao(false);
    }
  }

  async function sangria() {
    if (!aberto) return;
    if (valorSangria <= 0) { avisarErro('Informe um valor de sangria maior que zero.'); return; }
    setOperacao(true);
    try {
      await caixaService.sangria(aberto.id, valorSangria);
      sucesso('Sangria registrada.');
      setValorSangria(0);
      await carregar();
    } catch (e) {
      avisarErro(e instanceof ApiError ? e.message : 'Não foi possível registrar a sangria.');
    } finally {
      setOperacao(false);
    }
  }

  async function fechar() {
    if (!aberto) return;
    if (!window.confirm(`Fechar o caixa #${aberto.id}? O valor final será calculado pela API.`)) return;
    setOperacao(true);
    try {
      const fechado = await caixaService.fechar(aberto.id);
      sucesso(`Caixa fechado. Valor final: ${brl(fechado.valorFinal)}.`);
      await carregar();
    } catch (e) {
      avisarErro(e instanceof ApiError ? e.message : 'Não foi possível fechar o caixa.');
    } finally {
      setOperacao(false);
    }
  }

  return (
    <>
      <CabecalhoPagina
        titulo="Abrir/Fechar Caixa"
        descricao="Controle do turno. Só pode existir um caixa aberto por vez."
      />

      {erro && <Alerta tipo="erro">{erro}</Alerta>}

      <section className={`painel status-caixa-painel ${aberto ? 'aberto' : 'fechado'}`}>
        {carregando ? <Spinner texto="Consultando caixa..." /> : aberto ? (
          <>
            <div>
              <Etiqueta tom="verde">Caixa aberto</Etiqueta>
              <h2>Caixa #{aberto.id}</h2>
              <p className="texto-suave">Aberto em {dataHora(aberto.abertura)} por {aberto.usuarioNome ?? '—'}</p>
            </div>
            <div className="numeros-caixa">
              <div><span>Valor inicial</span><strong>{brl(aberto.valorInicial)}</strong></div>
              <div><span>Sangrias</span><strong>{brl(aberto.sangria ?? 0)}</strong></div>
            </div>
          </>
        ) : (
          <>
            <div>
              <Etiqueta tom="vermelho">Caixa fechado</Etiqueta>
              <h2>Nenhum caixa aberto</h2>
              <p className="texto-suave">Abra o caixa com o fundo de troco para iniciar as vendas.</p>
            </div>
            <div className="numeros-caixa">
              <div className="campo-inline">
                <span>Fundo de troco</span>
                <Entrada type="number" min={0} step="0.01" value={valorInicial}
                  onChange={(e) => setValorInicial(Number(e.target.value))} />
              </div>
              <Botao onClick={() => void abrir()} carregando={operacao}>Abrir caixa</Botao>
            </div>
          </>
        )}
      </section>

      {aberto && (
        <div className="grade-2">
          <section className="painel">
            <header className="painel-topo"><h2>Registrar sangria</h2></header>
            <p className="texto-suave">Retirada de dinheiro do caixa durante o turno.</p>
            <Campo label="Valor da sangria (R$)">
              <Entrada type="number" min={0.01} step="0.01" value={valorSangria}
                onChange={(e) => setValorSangria(Number(e.target.value))} />
            </Campo>
            <Botao variante="secundario" onClick={() => void sangria()} carregando={operacao}>
              Registrar sangria
            </Botao>
          </section>

          <section className="painel">
            <header className="painel-topo"><h2>Fechar caixa</h2></header>
            <p className="texto-suave">
              O valor final é calculado como valor inicial + vendas concluídas − sangrias.
            </p>
            <Botao variante="perigo" onClick={() => void fechar()} carregando={operacao}>
              Fechar caixa #{aberto.id}
            </Botao>
          </section>
        </div>
      )}

      <section className="painel">
        <header className="painel-topo"><h2>Histórico de caixas</h2></header>
        {carregando ? <Spinner /> : historico.length === 0 ? (
          <Vazio titulo="Nenhum caixa registrado" />
        ) : (
          <div className="tabela-scroll">
            <table className="tabela">
              <thead>
                <tr>
                  <th>#</th><th>Abertura</th><th>Fechamento</th><th>Usuário</th>
                  <th className="direita">Inicial</th><th className="direita">Sangria</th>
                  <th className="direita">Final</th><th>Status</th>
                </tr>
              </thead>
              <tbody>
                {historico.map((caixa) => (
                  <tr key={caixa.id}>
                    <td className="mono">{caixa.id}</td>
                    <td>{dataHora(caixa.abertura)}</td>
                    <td>{caixa.fechamento ? dataHora(caixa.fechamento) : '—'}</td>
                    <td>{caixa.usuarioNome ?? '—'}</td>
                    <td className="direita">{brl(caixa.valorInicial)}</td>
                    <td className="direita">{brl(caixa.sangria ?? 0)}</td>
                    <td className="direita">{caixa.valorFinal === null ? '—' : brl(caixa.valorFinal)}</td>
                    <td><Etiqueta tom={caixa.status === 'Aberto' ? 'verde' : 'neutro'}>{caixa.status}</Etiqueta></td>
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

