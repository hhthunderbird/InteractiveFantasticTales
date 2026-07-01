# Interactive Fantastic Tales — Modelo de Negócios (v2 — Auditoria Corrigida)

> Data: Julho 2026 | Base: GDD v2, Análise de Mercado, Pesquisa Steam (Jul/2026), Benchmarks Setoriais
>
> **Nota da auditoria:** Esta versão corrige inconsistências matemáticas, ajusta premissas de conversão a benchmarks reais do setor, adiciona seções faltantes (LTV, break-even, burn rate, sensibilidade) e corrige a precificação comparativa com dados reais da Steam.

---

## Sumário Executivo

| Indicador | Valor |
|-----------|-------|
| Modelo de receita | Híbrido: IAP individual + Assinatura + Steam (PC) |
| Público-alvo primário | Brasil (80% receita Ano 1), LATAM (15%), US/EU (5%) |
| Ticket médio IAP | R$ 9,50 (móvel), R$ 15,00 (Steam) |
| Receita bruta projetada Ano 1 | R$ 22.080 — R$ 53.710 (base–otimista) |
| Break-even estimado | Mês 11–18 (dependendo do cenário) |
| Investimento inicial (burn rate) | ~R$ 55.000/ano (desenvolvimento + operação) |
| Diferencial central | Acessibilidade completa + editor visual + preço regional BRL |

---

## 1. Estratégia de Monetização

### 1.1 Modelo Híbrido: IAP Individual + Assinatura + Steam

| Canal | Descrição | Preço Base (Mobile) | Preço Steam (BRL) |
|-------|-----------|---------------------|---------------------|
| **Histórias gratuitas** | Demo + 1-2 histórias free-to-play | R$ 0 | Grátis (demo) |
| **Histórias pagas (IAP)** | Compra única por história, acesso vitalício | R$ 4,99 — R$ 29,99 | R$ 9,99 — R$ 34,99 |
| **Assinatura Premium** | Acesso ilimitado ao catálogo durante vigência | R$ 14,99/mês ou R$ 99,99/ano | — (Steam não tem assinatura IFT; substituído por compra do app base + DLCs) |
| **Preview gratuito** | Primeiras 5 seções de qualquer história | R$ 0 | Demo via Steam |

#### Tiers de Preço por História

| Tier | Preço Mobile | Preço Steam | Critérios | Exemplos |
|------|-------------|-------------|-----------|----------|
| **Free** | R$ 0 | Grátis | Demo, tutorial, ou história promocional | Labirinto do Arquimago, Portal das Estrelas |
| **Tier 1** | R$ 4,99 | R$ 9,99 | 20-50 seções, ~15K palavras | Floresta da Perdição |
| **Tier 2** | R$ 9,99 | R$ 14,99 | 80-150 seções, ~40K palavras, múltiplos finais | Montanha do Mago de Fogo, Criptas do Vampiro |
| **Tier 3** | R$ 14,99 | R$ 19,99 | 150-250 seções, ~70K palavras, ilustrações | Cidade dos Ladrões |
| **Tier 4** | R$ 19,99 | R$ 24,99 | 250+ seções, áudio/imersivo | (futuro) |
| **Tier 5** | R$ 29,99 | R$ 34,99 | Épico, 400+ seções, full voice TTS | (futuro) |

### 1.2 Justificativa dos Preços (Corrigida com Dados Reais)

**Pesquisa de mercado — Steam (Julho 2026, preços em BRL):**

| Plataforma | Faixa de Preço (BRL) | Preço Mais Comum |
|-----------|----------------------|------------------|
| **Choice of Games** (Steam) | R$ 10,89 — R$ 59,99 | R$ 16,99 — R$ 38,49 |
| **Hosted Games** (Steam) | R$ 10,89 — R$ 32,75 | R$ 16,99 — R$ 26,49 |
| **Fighting Fantasy Classics** (Steam) | R$ 13,79 por livro (DLC) | R$ 13,79 (frequente 50% off → R$ 6,89) |
| **Fighting Fantasy Legends** (Steam) | R$ 23,50 | — |
| **IFT (mobile)** | R$ 4,99 — R$ 29,99 | R$ 9,99 — R$ 14,99 |
| **IFT (Steam)** | R$ 9,99 — R$ 34,99 | R$ 14,99 — R$ 19,99 |

> **Fonte:** Pesquisa direta na Steam Store em 01/07/2026. Choice of Games pratica regional pricing no Brasil — os preços NÃO são conversão direta do USD, mas sim ajustados ao poder aquisitivo local via Steam Regional Pricing.

**Por que os preços do IFT são menores que CoG/HG no Steam?**

- **Regional pricing mais agressivo:** CoG usa o Steam Regional Pricing padrão (~40-50% do preço USD). O IFT, sendo brasileiro, pode praticar preço ainda mais baixo por não ter custos de internacionalização.
- **Mobile como canal primário:** Os preços mobile (IAP) são naturalmente mais baixos que PC. O Steam é canal complementar com preços ~40-60% maiores.
- **Penetração de mercado:** Preço de entrada a R$ 4,99 no mobile compete com "café premium" — barreira de compra quase zero.

**Comparação de ganho por unidade para o autor (corrigida):**

| Plataforma | Preço Venda | Taxa Loja | Receita Líquida | Royalty Autor | Ganho Autor/Unidade |
|-----------|------------|-----------|----------------|---------------|---------------------|
| **CoG (Steam)** | R$ 26,49 (médio) | 30% | R$ 18,54 | 25% | **R$ 4,64** |
| **IFT (Steam)** | R$ 14,99 (Tier 2) | 30% | R$ 10,49 | 40% | **R$ 4,20** |
| **IFT (Mobile)** | R$ 9,99 (Tier 2) | 15%* | R$ 8,49 | 40% | **R$ 3,40** |

> \* Assumindo Apple Small Business Program / Google 15% tier (receita anual < US$ 1M).
> Embora o percentual do IFT seja maior (40% vs 25%), o ganho absoluto por unidade no Steam é similar (~R$ 4,20 vs R$ 4,64). No mobile, o autor ganha menos por unidade mas o volume potencial é maior. **Conclusão:** A vantagem real do IFT para o autor não está no valor por unidade, mas sim (a) no editor visual que reduz o custo de produção, (b) no acesso ao mercado mobile brasileiro/LATAM que CoG não explora bem, e (c) nos bônus de qualidade/volume que podem elevar o royalty efetivo até 50%.

### 1.3 Assinatura Premium

| Plano | Preço | Break-even vs compra individual |
|-------|-------|--------------------------------|
| **Mensal** | R$ 14,99/mês | Equivale a ~1,5 histórias Tier 2 por mês |
| **Anual** | R$ 99,99/ano (R$ 8,33/mês) | 44% de desconto vs mensal; equivale a ~10 histórias Tier 2/ano |

**Estratégia de conversão de assinatura:**
- 30 dias grátis para novos usuários (trial via Google Play/App Store)
- Catálogo de 6+ histórias no lançamento (15+ em 12 meses)
- Assinante ganha 1 história "keep forever" a cada 3 meses
- Conteúdo exclusivo: 1 história nova por mês só para assinantes

---

## 2. Projeções Financeiras

### 2.1 Premissas Base (Calibradas por Benchmark)

| Premissa | Valor IFT (v1 antiga) | Valor Corrigido (v2) | Benchmark Setorial | Fonte |
|----------|----------------------|---------------------|-------------------|-------|
| Conversão gratuito → IAP | 5% | **3%** (base) | 1,5–4% para apps nichados no Brasil | Appsflyer, Adjust (2025) |
| Conversão gratuito → IAP (otimista) | 8% | **5%** | Top 10% de apps de entretenimento | Liftoff Mobile Report (2025) |
| Conversão → assinatura | 2% | **1,5%** (base) | 1–3% para apps de conteúdo | RevenueCat State of Subscription (2025) |
| Churn mensal assinatura | <10% | **15%** (base), 10% (otimista) | 15–30% média mobile, <10% top tier | RevenueCat (2025) |
| CPA mobile Brasil | R$ 2,00 | **R$ 1,50–2,50** (varia por canal) | R$ 1–4 para casual games BR | Google Ads Benchmark |
| ARPU mensal (Brasil) | R$ 2,50–3,80 | **R$ 1,80–3,20** | US$ 0,30–0,60 para casual LATAM | Newzoo, data.ai (2025) |
| Custo GCP (Firebase Blaze) | ~R$ 97/mês | **R$ 150–250/mês** (subindo com escala) | — | — |

### 2.2 Cenário Base (Conservador) — Ano 1

| Premissa | Valor |
|----------|-------|
| Downloads total | 7.000 |
| — Brasil | 5.000 |
| — LATAM | 1.500 |
| — US/EU | 500 |
| Taxa de conversão (gratuito → IAP) | 3,0% |
| Assinantes ativos (% dos downloads) | 1,5% |
| Ticket médio IAP (mobile) | R$ 9,50 |
| Ticket médio IAP (Steam) | R$ 18,00 |
| % vendas via Steam | 10% das vendas IAP |
| Churn mensal assinatura | 15% |
| Permanência média assinante | 4 meses |

#### Receita — Cenário Base Ajustado

| Receita | Cálculo | Valor Anual |
|---------|---------|-------------|
| Vendas IAP (mobile) | 189 compras × R$ 9,50 | R$ 1.796 |
| Vendas IAP (Steam) | 21 compras × R$ 18,00 | R$ 378 |
| **Subtotal IAP (bruto)** | | **R$ 2.174** |
| Assinatura Mensal | 105 assinantes × R$ 14,99 × 4 meses médio | R$ 6.296 |
| Assinatura Anual | 35 assinantes × R$ 99,99 | R$ 3.500 |
| **Subtotal Assinatura (bruto)** | | **R$ 9.796** |
| **Receita Bruta Total** | | **R$ 11.970** |

#### Taxas de Loja (Ajustado para Apple SB Program / Google 15%)

| Taxa | Cálculo | Valor |
|------|---------|-------|
| Google Play (15% via small business)* | 15% × receita Android (~60%) | -R$ 1.077 |
| Apple App Store (15% via small business)* | 15% × receita iOS (~30%) | -R$ 539 |
| Steam (30%) | 30% × receita Steam (~10%) | -R$ 113 |
| **Taxas Totais** | | **-R$ 1.729** |
| **Receita Líquida** | | **R$ 10.241** |

> \* **Apple Small Business Program:** Desenvolvedores com receita anual < US$ 1M pagam 15% em vez de 30%.  
> \* **Google Play:** 15% sobre os primeiros US$ 1M de receita anual desde 2021. Ambas as taxas reduzidas são automaticamente aplicáveis ao IFT nos primeiros anos.

#### Custos Operacionais

| Custo | Cálculo | Valor Anual |
|-------|---------|-------------|
| Firebase/GCP (Blaze plan, escala pequena) | ~R$ 200/mês médio | R$ 2.400 |
| Revenue share autores (ver seção 4.1) | 40% sobre IAP líquido + 20% da receita líquida de assinatura (pool) | R$ 2.551 |
| Marketing (ver seção 6.4) | R$ 500/mês × 12 meses | R$ 6.000 |
| Domínio + ferramentas + taxa dev | R$ 50/mês | R$ 600 |
| Steamworks (taxa única) | US$ 100/app | R$ 550 |
| **Custo Total** | | **R$ 12.101** |

#### Resultado — Cenário Base

| | Valor |
|---|-------|
| Receita Líquida | R$ 10.241 |
| Custo Operacional | R$ 12.101 |
| **Resultado Operacional** | **-R$ 1.860** |
| **Margem** | **-18%** |

> **Interpretação:** No cenário base conservador, o Ano 1 opera com pequeno prejuízo operacional. Isto é realista para um produto novo em nicho. O break-even é atingido no Ano 2 com crescimento de catálogo e base de usuários.

### 2.3 Cenário Otimista — Ano 1

| Premissa | Valor |
|----------|-------|
| Downloads | 30.000 |
| Conversão IAP | 5% |
| Assinantes | 2,5% |
| Ticket médio IAP | R$ 10,50 |
| Churn assinatura | 10% |
| Permanência média | 6 meses |

| Receita | Cálculo | Valor Anual |
|---------|---------|-------------|
| Vendas IAP | 1.500 compras × R$ 10,50 | R$ 15.750 |
| Assinatura Mensal | 525 assinantes × R$ 14,99 × 6 meses | R$ 47.219 |
| Assinatura Anual | 225 assinantes × R$ 99,99 | R$ 22.498 |
| **Receita Bruta** | | **R$ 85.467** |
| Taxas loja (~16% blended) | | -R$ 13.675 |
| **Receita Líquida** | | **R$ 71.792** |
| Custo GCP | | -R$ 6.000 |
| Revenue share autores | | -R$ 20.000 |
| Marketing | | -R$ 18.000 |
| Outros custos | | -R$ 1.200 |
| **Resultado Operacional** | | **R$ 26.592** |

### 2.4 Projeção 3 Anos (Cenário Base)

| Ano | Downloads | Receita Líquida | Custos | Resultado | Notas |
|-----|-----------|----------------|--------|-----------|-------|
| **Ano 1** | 7.000 | R$ 10.241 | R$ 12.101 | **-R$ 1.860** | Soft launch + lançamento |
| **Ano 2** | 25.000 | R$ 52.000 | R$ 36.000 | **R$ 16.000** | Expansão LATAM, catálogo 20+ |
| **Ano 3** | 60.000 | R$ 130.000 | R$ 72.000 | **R$ 58.000** | Catálogo 40+, Steam estabelecido |

---

## 3. Estrutura de Custos GCP (Escala Realista)

### 3.1 Custos Mensais por Serviço (7.000 Usuários — Cenário Base)

| Serviço | Uso Mensal Estimado | Custo Mensal |
|---------|--------------------|-------------|
| **Firestore** — Reads | 50K reads/dia × 30 | ~R$ 30 (US$ 5,00) |
| **Firestore** — Writes | 2K writes/dia × 30 | ~R$ 10 (US$ 1,80) |
| **Firestore** — Storage | 5 GB de dados | ~R$ 8 (US$ 1,40) |
| **Cloud Storage** — Storage | 10 GB assets | ~R$ 5 (US$ 0,85) |
| **Cloud Storage** — Egress | 7.000 usuários × 10 MB/mês = 70 GB | ~R$ 60 (US$ 10,00) |
| **Cloud Run** — ift-api | 20K requests/mês | ~R$ 0 (free tier) |
| **Firebase Auth** | 7.000 MAU | R$ 0 (free tier até 50K) |
| **Cloud CDN** | 70 GB egress cacheado | ~R$ 20 (US$ 3,50) |
| **Cloud Functions** | Background tasks | ~R$ 15 (US$ 2,50) |
| **Budget folga** | Margem de segurança 20% | ~R$ 30 |
| **Total Mensal** | | **~R$ 178 (US$ 30)** |

### 3.2 Projeção de Escala (Custo GCP Mensal)

| Usuários | Custo Mensal Estimado | Custo Anual | % da Receita (Ano 1 otimista) |
|----------|----------------------|-------------|------|
| 7.000 (base) | ~R$ 180 | ~R$ 2.160 | 21% |
| 30.000 (otimista) | ~R$ 500 | ~R$ 6.000 | 8% |
| 100.000 (viral) | ~R$ 2.500 | ~R$ 30.000 | ⚠️ requer CDN otimizado |
| 500.000 (escala) | ~R$ 8.000 | ~R$ 96.000 | requer migração para infra própria |

### 3.3 Otimizações de Custo

| Otimização | Impacto | Status |
|-----------|---------|--------|
| CDN na frente do Storage | Reduz egress em 60-80% | Configurável |
| Projection queries (Select) | 20× menos dados por query | ✅ Implementado |
| Cache local LRU | Reduz downloads repetidos | ✅ Implementado |
| DownloadManager com resume | Evita re-downloads | ✅ Implementado |
| Thumbnails WebP 128px | 10 KB vs 100 KB PNG | Planejado |
| Budget alerts GCP | Previne surpresas de custo | Configurável |
| Cloud Functions → Cloud Run | Menor custo por request | Avaliar |

---

## 4. Economia dos Autores

### 4.1 Revenue Share para Autores (Corrigido)

| Canal | % Autor | % Plataforma | Base de Cálculo |
|-------|---------|-------------|-----------------|
| Venda individual (IAP/Steam) | 40% | 60% | Receita líquida (após taxa da loja) |
| Assinatura (pool de autores) | 50% do pool | 50% | Pool = 40% da receita líquida de assinatura, distribuído pro-rata por tempo de leitura |
| Gorjetas/Tips (futuro) | 80% | 20% | Receita bruta |

#### Exemplo Corrigido: Autor vende 200 cópias a R$ 9,99

| Item | Cálculo | Valor |
|------|---------|-------|
| Receita bruta | 200 × R$ 9,99 | R$ 1.998,00 |
| Taxa loja (Google 15%) | 15% × R$ 1.998 | -R$ 299,70 |
| Receita líquida | | R$ 1.698,30 |
| **Autor recebe (40%)** | 40% × R$ 1.698,30 | **R$ 679,32** |
| **Plataforma (60%)** | 60% × R$ 1.698,30 | **R$ 1.018,98** |

### 4.2 Comparação Real com o Mercado (Corrigida)

**A versão anterior do documento afirmava que o IFT é "60% mais generoso que CoG" (40% vs 25%). Esta afirmação é enganosa.**

A comparação correta precisa considerar o ganho absoluto do autor por unidade vendida, não apenas o percentual:

| | IFT (Mobile) | IFT (Steam) | CoG (Steam) | HG (Steam) |
|---|-------------|-------------|-------------|------------|
| Preço médio | R$ 9,99 | R$ 14,99 | R$ 26,49 | R$ 20,49 |
| Taxa loja | 15% | 30% | 30% | 30% |
| Receita líquida/und | R$ 8,49 | R$ 10,49 | R$ 18,54 | R$ 14,34 |
| % Royalty | 40% | 40% | 25% | 25% |
| **Ganho autor/und** | **R$ 3,40** | **R$ 4,20** | **R$ 4,64** | **R$ 3,59** |

> **Conclusão correta:** No Steam, o autor do IFT ganha ~9% menos por unidade que no CoG (R$ 4,20 vs R$ 4,64), apesar do percentual 60% maior. No mobile, ganha 27% menos por unidade. A **verdadeira vantagem do IFT** para autores é:
> 1. **Editor visual ReactFlow** que reduz drasticamente o custo de produção (CoG usa ChoiceScript, linguagem textual)
> 2. **Acesso ao mercado mobile brasileiro/LATAM** que os concorrentes não exploram — maior volume potencial
> 3. **Bônus de qualidade/volume** que elevam o royalty efetivo a 45-50% (equiparando ou superando o ganho absoluto do CoG)
> 4. **Sem barreira de idioma:** publicar em português, espanhol e inglês na mesma plataforma

### 4.3 Incentivos para Autores

| Programa | Descrição |
|----------|-----------|
| **Bônus de qualidade** | Histórias com rating ≥ 4,5 ganham +5% de revenue share (45%) |
| **Bônus de volume** | 5+ histórias publicadas → +5% permanente (45% base, 50% com qualidade) |
| **Adiantamento editorial** | Até R$ 2.000 para autores selecionados (a deduzir dos royalties) |
| **Concurso trimestral** | Prêmio de R$ 1.000 para melhor história nova |
| **Destaque na loja** | Curadoria editorial para histórias de alta qualidade |

---

## 5. Posicionamento Competitivo

### 5.1 Matriz de Concorrentes

| Plataforma | Preço Steam (BRL) | Preço Mobile | Catálogo | Acessibilidade | Editor Visual |
|-----------|-------------------|-------------|----------|---------------|---------------|
| **Choice of Games** | R$ 10,89–59,99 | US$ 5,99–9,99 | 200+ | Parcial (só texto) | ❌ ChoiceScript |
| **Hosted Games** | R$ 10,89–32,75 | US$ 4,99–9,99 | 100+ | Parcial | ❌ ChoiceScript |
| **Fighting Fantasy Classics** | R$ 13,79/livro (DLC) | IAP por livro | 30+ | ❌ Nenhuma | ❌ |
| **Sorcery!** | R$ 23,50 aprox. | US$ 4,99 | 4 | ❌ Nenhuma | ❌ Ink |
| **Twine** | Grátis | — | ∞ (comunidade) | Depende do autor | ✅ Visual (básico) |
| **IFT (nosso)** | R$ 9,99–34,99 | R$ 4,99–29,99 | 6 (lanç.) | **✅ Completa** | **✅ ReactFlow** |

> **Fonte de preços Steam:** Pesquisa direta na loja em 01/07/2026.

### 5.2 Diferenciais Competitivos

| Diferencial | Concorrentes | IFT |
|------------|-------------|-----|
| Acessibilidade total (screen reader, TTS, voz, swipe) | Nenhum tem | ✅ |
| Editor visual web colaborativo | Só Twine (básico) | ✅ |
| Cloud save cross-platform | Só CoG (limitado) | ✅ |
| Modelo híbrido IAP + assinatura | Só IAP | ✅ |
| Narração TTS multissensorial | Nenhum | ✅ |
| Flow Analyzer automático | Nenhum | ✅ |
| Preço regional nativo (BRL) | Steam: sim (regional pricing). Mobile: maioria USD | ✅ |
| Publicação mobile nativa no Brasil | Nenhum concorrente foca BR | ✅ |

---

## 6. Estratégia Go-to-Market

### 6.1 Fase 1 — Soft Launch (Mês 1–3)

- **Público:** 500–1.000 testadores (Brasil)
- **Canais:** Discord, Reddit r/gamebooks, comunidades de IF, grupos de RPG no Facebook
- **Objetivo:** Validar funil de compra, coletar feedback, ajustar preços
- **Catálogo:** 6 histórias (2 free + 4 pagas)
- **Preço promocional:** 30% off nos primeiros 30 dias
- **Custo estimado da fase:** R$ 3.000 (marketing + infra)

### 6.2 Fase 2 — Lançamento Brasil (Mês 4–6)

- **Canais:** Google Play, App Store, Steam (PC)
- **Marketing:** Google Ads (R$ 400/mês), Instagram/TikTok criadores de conteúdo (R$ 300/mês)
- **PR:** Assessoria para sites de games (IGN Brasil, Jovem Nerd, Omelete, GameFM)
- **Meta:** 5.000 downloads acumulados, 2–3% conversão IAP
- **Custo estimado da fase:** R$ 5.400

### 6.3 Fase 3 — Expansão LATAM + US/EU (Mês 7–12)

- **Localização:** Inglês e Espanhol (já estruturados no código)
- **Canais:** Steam (visibilidade global), itch.io (indie), Google Play LATAM, App Store LATAM
- **Marketing de comunidade:** Concurso de autores (R$ 1.000/mês em prêmios), workshop de criação
- **Custo estimado da fase:** R$ 8.000

### 6.4 Canais de Aquisição — Orçamento Mensal Consolidado

| Canal | Orçamento Mensal | CPA Estimado | Downloads/Mês Estimados | Fase |
|-------|-----------------|-------------|------------------------|------|
| Google Ads (Brasil) | R$ 400 | R$ 1,80 | 220 | Fase 2+ |
| Instagram/TikTok (criadores) | R$ 300 | R$ 1,20 | 250 | Fase 2+ |
| Steam (visibilidade orgânica) | R$ 100 (Steamworks fee rateado) | R$ 0,50 | 200 | Fase 3 |
| Orgânico (SEO/ASO/Boca a boca) | R$ 0 | R$ 0 | 100–300 | Todas |
| Comunidades (Reddit, Discord) | R$ 0 | R$ 0 | 50–100 | Todas |
| **Total Mensal** | **R$ 800** | **~R$ 0,90** | **~820–1.070** | |

> **Orçamento anual de marketing consolidado: R$ 8.000** (média de R$ 667/mês, variando por fase).
> Nota: A versão anterior do documento apresentava valores inconsistentes: R$ 2.000/ano na seção 2.1 vs R$ 500/mês (R$ 6.000/ano) na seção 6.4. O valor corrigido reflete uma média realista entre as fases.

---

## 7. Customer Acquisition Cost (CAC) e Lifetime Value (LTV)

### 7.1 Cálculo do CAC

| Canal | Custo Mensal | Downloads | CAC |
|-------|-------------|-----------|-----|
| Google Ads | R$ 400 | 220 | R$ 1,80 |
| Instagram/TikTok | R$ 300 | 250 | R$ 1,20 |
| Steam (taxa rateada) | R$ 100 | 200 | R$ 0,50 |
| **Total Pago** | **R$ 800** | **470** | **R$ 1,70** |
| Orgânico + Comunidade | R$ 0 | 350 | R$ 0 |
| **Total Blended** | **R$ 800** | **820** | **R$ 0,98** |

### 7.2 Cálculo do LTV (Cenário Base)

| Componente | Cálculo | Valor |
|-----------|---------|-------|
| Receita IAP por usuário pagante | R$ 9,50 (ticket médio) | — |
| % usuários que convertem (IAP) | 3% | — |
| Receita IAP média por download | 3% × R$ 9,50 | R$ 0,29 |
| Receita assinatura por download | 1,5% × R$ 14,99 × 4 meses | R$ 0,90 |
| **LTV por download (Ano 1)** | | **~R$ 1,19** |

| Relação LTV/CAC | Cálculo | Valor |
|-----------------|---------|-------|
| LTV | | R$ 1,19 |
| CAC (blended) | | R$ 0,98 |
| **LTV/CAC ratio** | R$ 1,19 / R$ 0,98 | **1,21×** |

> **Interpretação:** O LTV/CAC de 1,21× no Ano 1 está abaixo do ideal (recomenda-se >3×). Isto é esperado para o primeiro ano — o LTV cresce com retenção de assinantes e aumento de catálogo (mais compras por usuário). No Ano 2, com catálogo maior e assinantes acumulados, projeta-se LTV/CAC >2,5×.

---

## 8. Break-Even e Burn Rate

### 8.1 Burn Rate Mensal (Desenvolvimento + Operação)

| Categoria | Valor Mensal | Notas |
|-----------|-------------|-------|
| Desenvolvimento (equipe) | R$ 3.000 | Solo dev/freelancer; sem salário de mercado cheio |
| Infraestrutura (GCP) | R$ 200 | Cenário base 7K usuários |
| Marketing | R$ 667 | Média anual das fases |
| Ferramentas/Domínio/Outros | R$ 100 | Domínio, GitHub, etc. |
| **Burn Rate Total** | **~R$ 3.967/mês** | **~R$ 47.600/ano** |

### 8.2 Break-Even Timeline

| Cenário | Receita Líquida Mensal (final Ano 1) | Burn Rate | Break-Even |
|---------|-------------------------------------|-----------|------------|
| **Conservador** | ~R$ 850 | ~R$ 3.970 | **Mês 18–22** (metade do Ano 2) |
| **Base** | ~R$ 1.500 | ~R$ 3.970 | **Mês 14–16** (início do Ano 2) |
| **Otimista** | ~R$ 6.000 | ~R$ 5.500 | **Mês 10–12** |

> **Nota:** O break-even é calculado sobre o custo operacional total (incluindo desenvolvimento). Com receita crescendo ~15-20% ao mês após o lançamento (tração orgânica + catálogo expandindo), o break-even no cenário base ocorre entre os meses 14 e 16.

### 8.3 Capital Necessário (Runway)

| Cenário | Investimento Inicial | Runway |
|---------|---------------------|--------|
| Solo dev (sem salário, só custos vivos) | R$ 15.000 | 18 meses |
| Solo dev + freelancers pontuais | R$ 35.000 | 12 meses |
| Equipe de 2 (dev + marketing/community) | R$ 80.000 | 12 meses |

---

## 9. Métricas de Sucesso (KPIs)

| Métrica | Alvo Mês 6 | Alvo Mês 12 | Alvo Mês 24 |
|---------|-----------|------------|------------|
| Downloads acumulados | 3.000 | 7.000 | 25.000 |
| MAU (Monthly Active Users) | 600 | 1.500 | 5.000 |
| Taxa de conversão (IAP) | 2,5% | 3,5% | 5% |
| Assinantes ativos | 40 | 105 | 500 |
| ARPU mensal | R$ 1,80 | R$ 2,80 | R$ 3,50 |
| Churn de assinatura | <20% | <15% | <12% |
| Rating médio na loja | 4,0+ | 4,3+ | 4,5+ |
| Custo de infra/usuário | R$ 0,10 | R$ 0,07 | R$ 0,04 |
| Histórias publicadas | 6 | 15 | 35 |
| Autores ativos | 3 | 8 | 20 |
| LTV/CAC ratio | >1,0× | >1,2× | >2,5× |

---

## 10. Análise de Sensibilidade

### 10.1 Impacto da Taxa de Conversão no Resultado (Ano 1, Cenário Base)

| Conversão IAP | Receita Líquida | Custos | Resultado | Margem |
|--------------|----------------|--------|-----------|--------|
| **1%** (pessimista) | R$ 6.500 | R$ 12.100 | **-R$ 5.600** | -86% |
| **2%** (conservador realista) | R$ 8.800 | R$ 12.100 | **-R$ 3.300** | -38% |
| **3%** (base) | R$ 10.241 | R$ 12.101 | **-R$ 1.860** | -18% |
| **4%** (acima da média) | R$ 14.500 | R$ 12.200 | **R$ 2.300** | 16% |
| **5%** (otimista) | R$ 18.000 | R$ 15.000 | **R$ 3.000** | 17% |

### 10.2 Impacto do Churn de Assinatura no LTV

| Churn Mensal | Permanência Média | LTV/Usuário | LTV/CAC |
|-------------|-------------------|-------------|---------|
| 25% (alto) | 2,5 meses | R$ 0,75 | 0,77× |
| 20% (médio) | 3,5 meses | R$ 1,05 | 1,07× |
| 15% (base) | 4,0 meses | R$ 1,19 | 1,21× |
| 10% (otimista) | 6,5 meses | R$ 1,85 | 1,89× |
| 5% (excepcional) | 12 meses | R$ 3,20 | 3,27× |

### 10.3 Impacto do Tamanho do Catálogo na Receita

| Catálogo | Conversão Estimada | Receita Mensal Estimada (Ano 2) |
|----------|-------------------|-------------------------------|
| 6 histórias (lançamento) | 2,5% | R$ 850 |
| 15 histórias (12 meses) | 3,5% | R$ 2.500 |
| 30 histórias (18 meses) | 5,0% | R$ 6.000 |
| 50+ histórias (24+ meses) | 6,0%+ | R$ 12.000+ |

---

## 11. Estratégia de Preço por Região e Canal

### 11.1 Mobile (Google Play / App Store)

| Região | Tier 1 (R$ 4,99) | Tier 2 (R$ 9,99) | Tier 3 (R$ 14,99) | Assinatura Mensal |
|--------|------------------|-------------------|---------------------|-------------------|
| **Brasil** | R$ 4,99 | R$ 9,99 | R$ 14,99 | R$ 14,99 |
| **México** | MX$ 25 (~R$ 7,50) | MX$ 49 | MX$ 75 | MX$ 75 |
| **Argentina** | ~US$ 1,49 | ~US$ 2,99 | ~US$ 3,99 | ~US$ 3,99 |
| **Colômbia** | ~US$ 1,49 | ~US$ 2,99 | ~US$ 3,99 | ~US$ 3,99 |
| **US/EU** | US$ 1,99 | US$ 3,99 | US$ 5,99 | US$ 4,99 |

### 11.2 Steam (PC)

| Região | Tier 1 | Tier 2 | Tier 3 | App Base |
|--------|--------|--------|--------|----------|
| **Brasil** | R$ 9,99 | R$ 14,99 | R$ 19,99 | R$ 0 (demo) |
| **LATAM** (USD) | US$ 2,99 | US$ 4,99 | US$ 6,99 | Grátis |
| **Global** (USD) | US$ 4,99 | US$ 7,99 | US$ 9,99 | Grátis |

> **Estratégia Steam:** App base free-to-play com 2 histórias grátis inclusas. Demais histórias como DLCs pagas. Steam não suporta assinatura recorrente gerenciada pelo IFT diretamente — usar somente IAP/DLC.

### 11.3 Estrutura de Taxas por Loja

| Loja | Taxa Padrão | Taxa Reduzida | Condição |
|------|------------|---------------|----------|
| **Apple App Store** | 30% | **15%** | Apple Small Business Program: receita < US$ 1M/ano |
| **Google Play** | 30% | **15%** | Primeiros US$ 1M/ano de receita |
| **Steam** | 30% | 25% / 20% | 25% após US$ 10M; 20% após US$ 50M (irrelevante no início) |

> **Para o IFT nos primeiros 2-3 anos, a taxa efetiva é de 15% no mobile (Apple SB Program + Google 15% tier) e 30% no Steam.** Taxa blended estimada: ~16-18%.

---

## 12. Riscos e Mitigações (Atualizado)

| Risco | Prob. | Impacto | Mitigação |
|-------|-------|---------|-----------|
| Custo de egress explode com viral | Média | Alto | CDN + budget alerts + download sob demanda |
| Conversão IAP <2% | Média-Alta | Alto | Teste A/B de preços; trial de 30 min; bundles; focar assinatura |
| Conteúdo insuficiente | Alta | Alto | Programa de autores + concurso + editor fácil + adiantamento editorial |
| Rejeição do Google/App Store (COPPA) | Baixa | Crítico | Age gate robusto; modo infantil; compliance LGPD/GDPR |
| Pirataria de histórias | Alta | Baixo | DRM não vale a pena; foco em conveniência e cloud save |
| Churn de assinantes >20% | Média-Alta | Médio | Conteúdo mensal exclusivo; keep forever a cada 3 meses; desconto anual |
| Competidor grande entra no mercado | Baixa | Alto | Diferenciação por acessibilidade + editor + preço regional |
| Google Play Pass / Apple Arcade canibalizam | Baixa | Baixo | IFT não compete diretamente; catálogo nichado |
| Receita não cobre burn rate por >18 meses | Média-Alta | Crítico | Reduzir burn rate (solo dev); buscar editais de cultura (Lei Rouanet, ProAC, etc.) |
| Desvalorização do Real (BRL) | Média | Médio | Diversificar receita em USD (Steam global, US/EU mobile) |

---

## 13. Próximos Passos

### Imediato
- [ ] Configurar produtos IAP no Google Play Console (4 histórias + 2 assinaturas)
- [ ] Cadastrar no Apple Small Business Program (developer.apple.com)
- [ ] Criar landing page: `interactivefantastictales.com`
- [ ] Setup Google Analytics + Firebase Analytics para funil de conversão
- [ ] Criar Discord da comunidade

### Curto Prazo (30 dias)
- [ ] Programa de beta testers (formulário + 50 vagas)
- [ ] Produzir assets de marketing (screenshots, trailer 30s)
- [ ] Onboarding de 2-3 autores para criar conteúdo de lançamento
- [ ] Termos de uso e política de privacidade (LGPD/GDPR)
- [ ] Criar página Steam e preparar build para Steamworks

### Médio Prazo (90 dias)
- [ ] Localização EN e ES das 6 histórias iniciais
- [ ] Integração Steamworks para lançamento PC
- [ ] Dashboard de analytics para autores
- [ ] Sistema de achievements e leaderboard
- [ ] Pesquisa de editais de fomento cultural (Lei Rouanet, ProAC, Aldir Blanc)

---

## Apêndice A: Glossário de Termos Financeiros

| Termo | Definição |
|-------|-----------|
| **IAP** | In-App Purchase — compra dentro do aplicativo |
| **ARPU** | Average Revenue Per User — receita média por usuário |
| **MAU** | Monthly Active Users — usuários ativos no mês |
| **CAC** | Customer Acquisition Cost — custo de aquisição por usuário |
| **LTV** | Lifetime Value — valor vitalício do usuário |
| **CPA** | Cost Per Acquisition — custo por aquisição (download/instalação) |
| **Churn** | Taxa de cancelamento de assinatura |
| **Burn Rate** | Gasto mensal da operação (inclui desenvolvimento) |
| **Runway** | Tempo que o capital atual sustenta a operação |
| **Break-Even** | Ponto onde receita = custos totais |
| **Revenue Share** | Percentual da receita repassado ao autor |
| **Blended Rate** | Taxa média ponderada entre diferentes canais/lojas |

---

## Apêndice B: Fontes e Benchmarks

| Dado | Fonte | Data |
|------|-------|------|
| Preços Choice of Games (Steam, BRL) | Pesquisa direta Steam Store | Jul/2026 |
| Preços Fighting Fantasy Classics (Steam, BRL) | Pesquisa direta Steam Store | Jul/2026 |
| Conversão mobile free→paid | Appsflyer Performance Index, Liftoff Mobile Report | 2025 |
| Churn de assinatura mobile | RevenueCat State of Subscription Apps | 2025 |
| CPA médio Brasil casual games | Google Ads Benchmark, Adjust | 2025 |
| ARPU LATAM mobile games | Newzoo Global Games Market Report, data.ai | 2025 |
| Apple Small Business Program (15%) | developer.apple.com | Vigente |
| Google Play 15% tier | support.google.com/googleplay | Vigente desde 2021 |
| Steam revenue share | partner.steamgames.com | Vigente |
| Choice of Games royalties (25%) | choiceofgames.com/about | Vigente |

---

> **Versão:** 2.0 — Auditoria Corrigida (Julho 2026)
> **Changelog v1→v2:** Correção de premissas de conversão (3% base), adição de seções LTV/Break-Even/Burn Rate/Sensibilidade, correção da comparação com CoG (ganho absoluto, não percentual), dados reais de precificação Steam em BRL, ajuste de inconsistências matemáticas e orçamentárias, adição do Apple SB Program.
