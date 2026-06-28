import { useState } from 'react';

const steps = [
  {
    title: '1. Criar uma Nova Aventura',
    desc: 'Na tela inicial, clique em + Nova Aventura para começar. Sua história será salva automaticamente.',
    icon: '📖',
  },
  {
    title: '2. O Grafo — Seu Mapa da História',
    desc: 'Cada retângulo colorido é uma seção da história. A seção inicial tem o selo INÍCIO. As setas mostram os caminhos que o jogador pode seguir.',
    icon: '🗺️',
  },
  {
    title: '3. Criar Novas Seções',
    desc: 'Clique no botão + Nova Seção na barra superior e escolha o tipo. Você também pode clicar com o botão direito no grafo para criar, ou pressionar a tecla N.',
    icon: '➕',
  },
  {
    title: '4. Tipos de Seção',
    desc: '📖 Narrativa: texto + escolhas. ⚔️ Combate: luta contra inimigo com rolagem de dados. 🎲 Teste: teste de Habilidade ou Sorte. 🔑 Item Gate: verifica se você tem um item. 🔀 Aleatório: resultado decidido por probabilidade. 🏁 Final: vitória, derrota ou neutro.',
    icon: '🎨',
  },
  {
    title: '5. Editar uma Seção',
    desc: 'Clique em qualquer nó do grafo. O painel direito mostrará o editor onde você pode escrever o texto, adicionar escolhas, configurar combates, testes e muito mais.',
    icon: '✏️',
  },
  {
    title: '6. Conectar Seções (Criar Escolhas)',
    desc: 'Método 1: No painel direito, adicione escolhas e defina o número da seção destino. Método 2: Arraste do círculo inferior de um nó até o círculo superior de outro nó para criar uma conexão.',
    icon: '🔗',
  },
  {
    title: '7. Organizar o Grafo',
    desc: 'Clique em Organizar na barra superior para rearrumar os nós automaticamente. Use Centralizar (canto do grafo) para enquadrar tudo na tela. Use o scroll para zoom e arraste para mover a visão.',
    icon: '📐',
  },
  {
    title: '8. Preview — Testar a História',
    desc: 'Clique em Preview na barra superior para jogar sua história como um jogador. Navegue pelas seções, lute contra inimigos, faça testes e veja se o fluxo está correto.',
    icon: '▶️',
  },
  {
    title: '9. Auditoria — Verificar Problemas',
    desc: 'Clique em Auditoria para ver problemas no fluxo da história: becos sem saída, seções órfãs, itens sem uso e muito mais. Corrija os problemas antes de publicar.',
    icon: '🔍',
  },
  {
    title: '10. Salvar e Exportar',
    desc: 'Clique em Salvar para guardar na nuvem e baixar uma cópia JSON. No Modo Local, as histórias ficam salvas no seu navegador.',
    icon: '💾',
  },
  {
    title: 'Atalhos de Teclado',
    desc: 'N → Nova seção narrativa. Delete/Backspace → Excluir seção selecionada. Ctrl+S → Salvar. Clique duplo no grafo → Nova seção. Botão direito no nó → Menu de contexto.',
    icon: '⌨️',
  },
];

export function HelpPanel() {
  const [open, setOpen] = useState(false);

  return (
    <>
      <button
        onClick={() => setOpen(true)}
        className="fixed bottom-4 right-4 z-50 w-10 h-10 bg-[#e94560] text-white rounded-full text-lg shadow-lg hover:bg-[#d63850] transition-colors flex items-center justify-center"
        title="Ajuda"
      >
        ?
      </button>

      {open && (
        <div className="fixed inset-0 z-50 bg-black/60 flex items-center justify-center p-4" onClick={() => setOpen(false)}>
          <div
            className="bg-[#16213e] border border-[#2a2a4a] rounded-xl shadow-2xl w-full max-w-2xl max-h-[85vh] flex flex-col overflow-hidden"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between p-4 border-b border-[#2a2a4a] shrink-0">
              <h2 className="text-lg font-bold text-white">📚 Central de Ajuda</h2>
              <button onClick={() => setOpen(false)} className="text-[#a0a0b0] hover:text-white text-xl">✕</button>
            </div>

            <div className="flex-1 overflow-y-auto p-4 space-y-4">
              {steps.map((step, i) => (
                <div key={i} className="bg-[#0f3460] rounded-lg p-4">
                  <div className="flex items-start gap-3">
                    <span className="text-2xl shrink-0">{step.icon}</span>
                    <div>
                      <h3 className="text-sm font-semibold text-white mb-1">{step.title}</h3>
                      <p className="text-xs text-[#a0a0b0] leading-relaxed">{step.desc}</p>
                    </div>
                  </div>
                </div>
              ))}

              <div className="bg-[#f59e0b20] border border-[#f59e0b] rounded-lg p-4">
                <div className="flex items-start gap-3">
                  <span className="text-2xl shrink-0">💡</span>
                  <div>
                    <h3 className="text-sm font-semibold text-[#f59e0b] mb-1">Dica Importante</h3>
                    <p className="text-xs text-[#f59e0b] leading-relaxed">
                      Sempre verifique a Auditoria antes de considerar sua história pronta.
                      Uma história com becos sem saída ou sem final de vitória vai frustrar os jogadores!
                    </p>
                  </div>
                </div>
              </div>
            </div>

            <div className="p-3 border-t border-[#2a2a4a] text-center shrink-0">
              <button
                onClick={() => setOpen(false)}
                className="px-6 py-2 bg-[#e94560] text-white rounded-lg text-sm font-semibold hover:bg-[#d63850]"
              >
                Entendi!
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
