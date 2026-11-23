# CANAL 420 — Roteiro Completo e Catálogo Geral de Textos do Jogo

> **Documento-mestre sincronizado 100% com o código atual do projeto.**  
> Reúne, em seções separadas e verbatim, **todas as falas e monólogos**, **todos os documentos escritos**, **todos os prompts de interação**, **todos os comandos e textos do HUD de tarefas e menus**, e **todos os textos impressos nos props 3D e na tela CRT**.
>
> **Fontes de verdade auditadas:** `DayManager.cs`, `TVController.cs`, `SofaTVProxy.cs`, `InteractionSystem.cs`, `InspectableObject.cs`, `DoorInteractable.cs`, `AnimatedContainerInteractable.cs`, `SafeBoxPuzzle.cs`, `FallingPictureFrame.cs`, `HallucinationTrigger.cs`, `MirrorHauntController.cs`, `PsychologicalHorrorDirector.cs`, `ObjectiveHUD.cs`, `GameSettingsUI.cs`, `FlashlightController.cs`, `ProceduralCRTFeed.cs`, `Canal420GreyboxBuilder.cs`, `Canal420PropDetailBuilders.cs` e `Canal420PrintDetailsGenerator.cs`.

---

## Sumário

- [Visão Geral e Personagens](#visão-geral-e-personagens)
- [1. Falas, Monólogos, Transmissões e Flashes de Dissociação](#1-falas-monólogos-transmissões-e-flashes-de-dissociação)
  - [1.1 Cartões de Transição e Encerramento](#11-cartões-de-transição-e-encerramento)
  - [1.2 Dia 1 — Burnout e Rotina (Domingo, 29/10/1995)](#12-dia-1--burnout-e-rotina-domingo-29101995)
  - [1.3 Dia 2 — O Efeito Depressor (Segunda-feira)](#13-dia-2--o-efeito-depressor-segunda-feira)
  - [1.4 Dia 3 — Apagão, Estimulantes e Revelação (Terça-feira, madrugada)](#14-dia-3--apagão-estimulantes-e-revelação-terça-feira-madrugada)
  - [1.5 Dia 4 — Epílogo / Prelúdio (Quarto das Filhas)](#15-dia-4--epílogo--prelúdio-quarto-das-filhas)
  - [1.6 Objetos Permanentes da Casa Base, Portas e Contêineres](#16-objetos-permanentes-da-casa-base-portas-e-contêineres)
  - [1.7 Flashes de Dissociação (Verdade Reprimida)](#17-flashes-de-dissociação-verdade-reprimida)
- [2. Documentos Escritos (Leitor Modal)](#2-documentos-escritos-leitor-modal)
- [3. Prompts de Interação (`[E]`)](#3-prompts-de-interação-e)
- [4. Comandos e Textos do HUD de Tarefas e Outras Interfaces (UI)](#4-comandos-e-textos-do-hud-de-tarefas-e-outras-interfaces-ui)
  - [4.1 HUD de Tarefas (`ObjectiveHUD`)](#41-hud-de-tarefas-objectivehud)
  - [4.2 HUD da Lanterna (`FlashlightController`)](#42-hud-da-lanterna-flashlightcontroller)
  - [4.3 Interface do Leitor de Documentos (`InteractionSystem`)](#43-interface-do-leitor-de-documentos-interactionsystem)
  - [4.4 Interface do Cofre Numérico (`SafeBoxPuzzle`)](#44-interface-do-cofre-numérico-safeboxpuzzle)
  - [4.5 Menu de Opções (`GameSettingsUI`)](#45-menu-de-opções-gamesettingsui)
- [5. Textos Escritos em Props, Rótulos, Papéis 3D e Tela da TV (`PropDetails` / `PrintDetails` / `CRT`)](#5-textos-escritos-em-props-rótulos-papéis-3d-e-tela-da-tv-propdetails--printdetails--crt)
  - [5.1 Calendários de Parede](#51-calendários-de-parede)
  - [5.2 Medicamentos, Frascos, Cartelas e Caixas](#52-medicamentos-frascos-cartelas-e-caixas)
  - [5.3 Áudio, Vídeo, Pilhas e Relógios](#53-áudio-vídeo-pilhas-e-relógios)
  - [5.4 Embalagens Domésticas (Cozinha e Banheiro)](#54-embalagens-domésticas-cozinha-e-banheiro)
  - [5.5 Papéis, Bilhetes, Jornal, Envelopes e Desenhos 3D](#55-papéis-bilhetes-jornal-envelopes-e-desenhos-3d)
  - [5.6 Interações e Documentos Definidos em `Canal420PropDetailBuilders.cs`](#56-interações-e-documentos-definidos-em-canal420propdetailbuilderscs)
  - [5.7 Textos na Tela da TV CRT — OSD e Tickers (`ProceduralCRTFeed.cs`)](#57-textos-na-tela-da-tv-crt--osd-e-tickers-proceduralcrtfeedcs)

---

## Visão Geral e Personagens

- **Cian Nabs (34 anos):** Analista contábil pleno na Baixada Santista. Na sua ilusão dissociativa, é **domingo, 29 de outubro de 1995**, e ele está apenas esgotado pelas cobranças do chefe. Na realidade reprimida, uma tragédia familiar aconteceu na noite de **sábado, 10 de setembro de 1994** (`10/09`).
- **Margaret Nabs (32 anos):** Esposa de Cian. Presente em listas, agenda rasgada, bilhete dobrado, roupas intocadas, retrato do corredor e no desfecho do Dia 3.
- **As Duas Filhas (6 e 8 anos):** Presentes pelo quarto trancado no fim do corredor leste, pelo choro abafado na gravação da secretária e pelos desenhos infantis.
- **A. Tavares:** Gerente operacional de Cian; âncora de estresse corporativo usada por Cian para não encarar o luto.
- **Canal 420:** Frequência fantasma na TV de tubo que rompe gradualmente a negação de Cian.

---

## 1. Falas, Monólogos, Transmissões e Flashes de Dissociação

### 1.1 Cartões de Transição e Encerramento

| Momento | Título | Subtítulo | Dica / Comandos | Origem |
|---|---|---|---|---|
| **Início do Dia 1** | `DIA 1` | `Domingo, 29 de outubro de 1995` | — | `DayManager.cs` |
| **Início do Dia 2** | `DIA 2` | `Segunda-feira, 30 de outubro de 1995` | — | `DayManager.cs` |
| **Início do Dia 3** | `DIA 3` | `Terça-feira, ?? de ?? de 19??` | — | `DayManager.cs` |
| **Início do Dia 4** | *(corte seco, sem cartão)* | — | — | `DayManager.cs` |
| **Encerramento da Demo** | `CANAL 420` | `Fim da demonstração. Obrigado por jogar.` | `[R] Reiniciar   \|   [ESC] Sair` | `DayManager.cs` |

---

### 1.2 Dia 1 — Burnout e Rotina (Domingo, 29/10/1995)

#### Despertar e Fluxo Obrigatório
- **Ao acordar no Quarto do Casal (`DayManager.cs`):**
  > "Já escureceu. Preciso jogar uma água no rosto."
- **Pia do Banheiro (`lavar_rosto` — `Pia_Teste`):**
  - *1ª vez:* "Água gelada. Essa dor atrás do olho não sai por nada."
  - *Repetição:* "Mais água não vai resolver dormir duas horas por noite."
- **Secretária Eletrônica (`checar_secretaria` — `Secretaria_Eletronica_D1`):**
  - *1ª vez:* `[BIP] "Cian? Tavares aqui. Liguei no seu ramal e nada... O lote de outubro voltou inteiro errado. Segunda, oito em ponto, resolve isso na minha sala."`
  - *Repetição:* "Não vou ficar ouvindo cobrança num domingo à noite."
- **Porta do Escritório (`Porta_Escritorio_Pivot` — trancada sem chave):**
  > "Trancada. Passei a chave ontem à noite."
- **Pratinho de Chaves na Copa (`pegar_chave_escritorio` — `Pratinho_Chaves_Copa`):**
  > "Tá aqui. A da fita azul é do escritório."
- **Porta do Escritório (`Porta_Escritorio_Pivot` — ao destrancar):**
  > "Cheiro de papel velho..."
- **Papéis na Escrivaninha (`olhar_papeis` — `Papeis_Teste` — após ler Documento #1):**
  - *Pós-leitura:* "Terceira vez que devolvem esse fechamento..."
  - *Repetição:* "Os números estão todos embaralhados."
- **Calendário do Escritório (`checar_calendario` — `Calendario_Dia1_Interagir`):**
  - *1ª vez:* "Domingo, 29 de outubro de 1995. Mais uma semana inteira riscada."
  - *Repetição:* "29 de outubro. Amanhã cedo começa tudo de novo."
- **Tentar sentar na TV antes de concluir as tarefas do Dia 1 (`DayManager.cs`):**
  > "Se eu sentar agora, eu apago. Preciso resolver o que ficou pendente antes."
- **Tentar sentar na TV sem pilhas no controle (`DayManager.cs`):**
  > "O botão do painel tá afundado e o controle sem pilhas. Acho que tem alguma naquela lanterna da cozinha."
- **Pilhas na Primeira Gaveta da Cozinha (`pilhas_controle_task` — `Pilhas_Controle_D1`):**
  > "Duas pilhas. Serve pro controle."

#### Sequência da TV de Tubo no Dia 1 (`TVController.cs`)
- **Canal 04 (Telejornal):**
  > `[CANAL 04]: "...o enxugamento no setor administrativo já fechou mais de quatro mil vagas de escritório só neste semestre no estado..."`
- **Canal 09 (Previsão do Tempo):**
  > `[CANAL 09]: "...frente fria estacionada na serra mantém neblina fechada na Baixada até quarta-feira..." A antena tá puxando dois canais juntos.`
- **Canal 17 (Televendas):**
  > `[CANAL 17]: "...café passado em quatro minutos, direto na jarra térmica! Ligue 0800..."`
- **Canal 420 — 1ª sintonização:**
  > "Nunca entrou sinal nessa faixa. Essas cores estão dando um sono estranho."
- **Canal 420 — 2ª tentativa (trocar de canal):**
  > "Não mudou. A imagem tá derretendo..."
- **Canal 420 — 3ª tentativa (desligar / apagão para o Dia 2):**
  > "Apertei pra desligar e não vai. Meu braço tá pesado... não tô conseguindo levantar."

#### Inspeções Opcionais do Dia 1 (`Canal420GreyboxBuilder.cs`)
- **Quadro de Força (`quadro_forca_opc` — `Quadro_Forca_Dia1`):**
  - *1ª vez:* "Corredor, sala, cozinha..."
  - *Repetição:* "Tudo armado."
- **Espelho do Banheiro (`espelho_dia1_opc` — `Espelho_Dia1`):**
  - *1ª vez:* "Olheira funda, barba por fazer. Pareço mais velho do que sou."
  - *Repetição:* "Melhor sair da frente do vidro."
- **Fotografias do Corredor (`quadros_familia_d1_opc`):**
  - *1ª vez:* "Eu bati essa foto num domingo qualquer no quintal. Nunca paro aqui pra olhar."
  - *Repetição:* "Passo reto por esse corredor todo dia."
- **Retrato de Margaret (`retrato_margaret_d1_opc`):**
  - *1ª vez:* "Faz tempo que ninguém tira isso pra limpar."
  - *Repetição:* "O vidro tá opaco de poeira."
- **Casaco Cinza de Margaret no Guarda-Roupa (`guarda_roupa_d1_opc` — `Casaco_Margaret_GuardaRoupa_D1`):**
  - *1ª vez:* "As roupas dela continuam penduradas no mesmo canto. Não mexo aí."
  - *Repetição:* "Cheiro forte."

---

### 1.3 Dia 2 — O Efeito Depressor (Segunda-feira)

#### Despertar e Fluxo Obrigatório
- **Ao acordar no Sofá (`DayManager.cs`):**
  > "Que gosto ruim na boca..."
- **Frascos e Receituário na Mesa de Centro (`frascos_calmantes_dia2` — `Frascos_MesaCentro` — após ler Documento #2):**
  - *Pós-leitura:* "A receita venceu faz quase um ano."
  - *Repetição:* "Os dois frascos tão vazios."
- **Secretária Eletrônica sem fita (`secretaria_fita_dia2` — `Secretaria_Eletronica_D2`):**
  > "Tá sem fita, só apitando. Devo ter deixado em algum canto ontem à noite."
- **Fita Cassete na Borda da Pia (`pegar_fita_banheiro_dia2` — `Fita_Cassete_D2`):**
  > "Tava jogada aqui na pia molhada. Devo ter arrancado da secretária ontem de noite sem perceber."
- **Secretária Eletrônica com a fita (`secretaria_fita_dia2` — `Secretaria_Eletronica_D2`):**
  - *1ª vez:* `[BIP] Sexta-feira, 23h58. Só chiado e alguém respirando...`
  - *Repetição:* "Não era recado de trabalho..."

#### Eventos Psicológicos, Corredor e Espelho no Dia 2
- **Gatilho 1 — Vulto no Corredor (`vulto_corredor` — `Trigger_1_VultoCorredor`):**
  > "Tem alguém aqui?... Não. Deve ser o efeito do remédio na vista."
- **Gatilho 2 — Quadros Familiares Rachando (`quadros_familia_alterados` — `Trigger_2_QuadrosFamilia`):**
  > "Os quadros racharam..."
- **Gatilho 3 — Sussurros atrás da Porta (`pre_sussurros_corredor` — `Trigger_3_SussurrosPorta`):**
  - *(Sem legenda — apenas áudio abafado de sussurros e canção de ninar.)*
- **Porta Trancada do Fundo no Dia 2 (`Porta_Quarto_Pivot` — configurado por `DayManager.cs`):**
  - *Antes de ouvir a fita da secretária:* "Trancada... A madeira inchada estala com a umidade."
  - *Após ouvir a fita da secretária:* "Não tem ninguém aí dentro... Preciso voltar pra sala."
- **Aparição no Espelho do Banheiro (`MirrorHauntController.cs`):**
  - *Ao se virar após ver o vulto na porta pelo reflexo:* "Tenho certeza que tinha alguém ali... Não tem ninguém aqui."
  - *Discrepância do estado da porta no reflexo (se a porta física está fechada):* "No espelho a porta tava entreaberta... Ela tá fechada."
  - *Discrepância do estado da porta no reflexo (se a porta física está aberta):* "No espelho a porta tava quase fechada..."
- **Guarda-Roupa Entreaberto Sozinho no Dia 2 (`AnimatedContainerInteractable.cs` / `DayManager.cs`):**
  > "Pensei ter deixado tudo fechado ontem."

#### Sequência da TV Autônoma no Dia 2 (`TVController.cs`)
- **Tocar na TV enquanto desligada (`Day2_OffIdle`):**
  > "O vidro ainda tá morno. Não lembro a que horas desliguei isso."
- **TV ligando sozinha na sala (`ActivateDay2AutonomousNews`):**
  - *Se o jogador já estiver na sala:* "A televisão ligou sozinha..."
  - *Se o jogador estiver fora da sala:* "Tem alguém na sala? A televisão ligou sozinha."
- **Plantão Especial de 1994 (Parte 1):**
  > `[PLANTÃO]: "...a Polícia Civil isolou agora à noite uma casa no fim da Rua das Acácias, após vizinhos ligarem pro 190 ouvindo gritos e pancadas..."`
- **Plantão Especial de 1994 (Parte 2):**
  > `[PLANTÃO]: "...a mulher e as duas filhas foram encontradas mortas dentro do quarto dos fundos, trancado por fora. O marido continuava sentado na sala."`
- **Reação de Cian:**
  > "Mortas...? Dez de setembro de noventa e quatro. Minha cabeça dói, como se eu não pudesse compreender isso. Este é o inferno?"
- **Invasão do Canal 420 (transição para o Dia 3):**
  > "Tá entrando aquele mesmo canal por cima!"

#### Inspeções Opcionais do Dia 2 (`Canal420GreyboxBuilder.cs`)
- **Espelho do Banheiro (`espelho_dia2_opc` — `Espelho_Dia2`):**
  - *Flash de dissociação:* `A PORTA DO FUNDO`
  - *1ª vez:* "Pálpebra caída, boca pastosa de remédio."
  - *Repetição:* "Não quero ficar olhando pra isso."
- **Calendário do Escritório (`calendario_d2_opc`):**
  - *1ª vez:* "Outubro de 1995. Segunda, eu acho."
  - *Repetição:* "Outubro de 1995."
- **Lanterna Vazia na Gaveta da Cozinha (`lanterna_vazia_d2_opc`):**
  - *1ª vez:* "Sem pilha nenhuma. Tirei ontem de noite."
  - *Repetição:* "Vazia."
- **Quadro de Força (`quadro_forca_d2_opc`):**
  - *1ª vez:* "Tem um chiado elétrico dentro dessa caixa."
  - *Repetição:* "Melhor não mexer nisso agora."
- **Casaco Cinza de Margaret (`casaco_margaret_d2_opc`):**
  - *1ª vez:* "A manga do casaco encostou no meu braço..."
  - *Repetição:* "Deixa a porta fechada."

---

### 1.4 Dia 3 — Apagão, Estimulantes e Revelação (Terça-feira, madrugada)

#### Despertar e Bloqueios Iniciais (`DayManager.cs`)
- **Ao acordar no escuro na Sala (`DayManager.cs`):**
  > "O que aconteceu ontem... Caiu a força da casa inteira. Preciso da lanterna."
- **Tentar mexer no Quadro de Força ou Retrato de Margaret sem a lanterna:**
  > "Tá escuro demais pra mexer nisso agora. A lanterna tá na primeira gaveta da cozinha."
- **Tentar mexer nas Cápsulas Estimulantes sem a lanterna:**
  > "Tá escuro demais pra mexer nesses remédios agora. Preciso pegar a lanterna primeiro."
- **Tentar mexer no Quadro de Força ou Retrato de Margaret antes de engolir a 1ª cápsula:**
  > "Minha vista tá escurecendo de novo... Se eu não tomar aquelas pílulas na mesa da sala, vou apagar aqui."
- **Tentar tomar cápsula fora de contexto:**
  > "Agora não."

#### Progressão Principal do Dia 3
- **Lanterna na Primeira Gaveta da Cozinha (`pegar_lanterna_dia3` — `Lanterna_Dia3`):**
  > "Me lembro de ter tirado as pilhas dessa lanterna... Pelo menos tá acendendo."
- **Frasco Manipulado na Mesa de Centro (`estimulante_frasco_dia3_opc` — `Frasco_Estimulante_Dia3` — após ler Documento #3):**
  - *Pós-leitura:* "Fenproporex com cafeína. A data tá raspada... parece etiqueta de farmácia ou de clínica. Preciso engolir uma cápsula antes de apagar de novo."
  - *Repetição:* "Fenproporex com cafeína."
- **Cápsulas Estimulantes (`engolir_capsula_dia3` — `Capsulas_Estimulante_Dia3`):**
  - *1ª cápsula:* "Agora consigo checar o quadro de força."
  - *2ª cápsula (última):* "A última cápsula."
  - *Se tentar repetir durante cooldown:* "O coração já tá disparado. Preciso esperar antes de engolir outra."
- **Quadro de Força sem fusível (`restaurar_energia_dia3` — `Quadro_Forca_Dia3`):**
  > "O cartucho queimou na chave do corredor. Deve ter um reserva na caixa de ferramentas."
- **Fusível na Caixa de Ferramentas do Banheiro (`pegar_fusivel_dia3` — `Fusivel_Ceramico_Pickup_D3`):**
  > "Achei um fusível no fundo da caixa."
- **Quadro de Força com fusível (`restaurar_energia_dia3` — `Quadro_Forca_Dia3`):**
  - *Ao trocar:* "Estalou. A luz do corredor voltou."
  - *Repetição:* "Já tá ligado."
- **Retrato de Margaret no Corredor Leste (`FallingPictureFrame.cs` — `Quadro_Margaret_Parede`):**
  > "O arame arrebentou... Não queria me lembrar desse cofre."
- **Cofre de Parede (`SafeBoxPuzzle.cs` — `Cofre_Parede_1009`):**
  - *Sem energia no corredor:* "O teclado tá apagado. Sem força nessa parede... tenho que olhar o quadro de força na cozinha."
  - *Ao digitar `1009` e abrir:* "Uma chave... É a da porta do fundo."
- **Chave Escondida no Casaco Cinza (`chave_arquivo_pessoal_dia3` — `Guarda_Roupa_Casacos_D3`):**
  > "Uma chave escondida no fundo do bolso dela. É da gaveta alta do meu arquivo no escritório."

#### Pistas Documentais e Inspeções Opcionais do Dia 3
- **Agenda Rasgada na Cabeceira (`agenda_margaret_dia3_opc` — após ler Documento #4):**
  - *Flash de dissociação:* `10 DE SETEMBRO - EU TRANQUEI`
  - *Pós-leitura:* "Ela escreveu isso no sábado de manhã..."
  - *Repetição:* "Ela só queria conversar comigo naquela noite."
- **Bilhete Dobrado no Escritório (`bilhete_margaret_dia3_opc` — após ler Documento #5):**
  - *Pós-leitura:* "Tava dobrado debaixo das pastas desde aquela sexta... Nem li o que ela pediu."
  - *Repetição:* "Ela deixou o jantar no forno e foi deitar com as meninas."
- **Folha de Baixo do Calendário (`calendario_folha_anterior_opc` — após ler Documento #6):**
  - *Pós-leitura:* "Folha de setembro do ano passado embaixo do bloco novo. Não me lembrava que ainda estava ai."
  - *Repetição:* "O último risco quase furou o papel no dia nove."
- **Calendário do Escritório (`calendario_d3_opc`):**
  - *1ª vez:* "Um mês inteiro riscado em cima do outro... Eu só troquei a folha pra fingir que o ano continuou."
  - *Repetição:* "Outubro por cima de setembro."
- **Secretária Eletrônica (`secretaria_d3_opc`):**
  - *1ª vez:* "Arranquei da tomada. Não aguentava mais esse apito."
  - *Repetição:* "Fora da tomada."
- **Evento Psicológico — LED Fantasma na Secretária (`PsychologicalHorrorDirector.cs`, após ler o recorte de jornal):**
  > "Tá fora da tomada..."
- **TV Desligada no Dia 3 (`TVController.cs` — `Day3_SilentGlass`):**
  > "Sem luz na sala, mas o tubo ainda dá uns estalos por dentro."
- **Espelho do Banheiro (`espelho_dia3_opc` — `Espelho_Dia3`):**
  - *Flash de dissociação:* `NÃO SOU EU`
  - *1ª vez:* "Pupila aberta demais na penumbra. Parece outra pessoa encarando de volta."
  - *Repetição:* "O vidro escuro devolve mais sombra do que rosto."

#### Clímax — Destrancando a Porta do Quarto das Filhas (`DayManager.cs`)
- **Ao destrancar `Porta_Quarto_Pivot` com a chave do cofre:**
  > "..."
- **Se Cian hesitar na soleira por 5 segundos sem entrar (`RunMargaretDoorTrapSequenceRoutine`):**
  > "Abriu... Tá tudo quieto lá dentro."
- **Se Cian tentar recuar de volta pelo corredor (`z < 3.10`):**
  > "Não dá mais pra voltar pro sofá."
- **Ao ver Margaret dentro do quarto (antes do ataque):**
  > "Margaret..."

---

### 1.5 Dia 4 — Epílogo / Prelúdio (Quarto das Filhas)

*(Cian está sentado dentro do quarto iluminado pela manhã; movimentação bloqueada, olhar restrito.)*

- **Fala 1 (abertura do Dia 4 — `DayManager.cs`):**
  > "O cobertor pequeno continua dobrado no pé da cama, do jeito que ela deixou."
- **Fala 2 (confissão — `DayManager.cs`):**
  > "Eu girei a chave pelo lado de fora, tomei dois comprimidos pra não ouvir mais nada... e aumentei o volume da TV."
- **Fala 3 (encerramento — `DayManager.cs`):**
  > "Não sei mais que ano é hoje... nem se algum dia eu realmente saí dessa sala."

---

### 1.6 Objetos Permanentes da Casa Base, Portas e Contêineres

#### Objetos Dinâmicos por Dia (`DayManager.cs`)
- **Porta da Rua (`Porta_Rua_Interagir`):**
  - *Base (`Canal420GreyboxBuilder.cs`):* "Duas voltas na chave e corrente passada. Não abro essa porta pra ninguém." | *Rep:* "Cheia de folheto acumulado por baixo."
  - *Dia 1:* "Duas voltas na chave e corrente passada. Nem um carro passa na rua lá fora... Não abro essa porta pra ninguém." | *Rep:* "Cheia de folheto sem data acumulado por baixo."
  - *Dia 2:* "A corrente tá esticada e a tranca pesada demais." | *Rep:* "Duas voltas na chave. A rua parece longe demais."
  - *Dia 3 (antes de abrir o cofre):* "Não adianta puxar a corrente no escuro... Tudo o que eu escondi tá do outro lado do corredor, não lá fora." | *Rep:* "Não tem saída por aqui."
  - *Dia 3 (após pegar a chave do cofre):* "Essa chave de latão não é daqui. Eu sei muito bem qual porta ela abre." | *Rep:* "Não é por aqui que isso acaba."
- **Despertador do Quarto (`despertador_quarto_opc`):**
  - *Dia 1 (17h40):* "Cinco e quarenta. Olho pra esse relógio toda hora esperando dar segunda." | *Rep:* "Cinco e quarenta."
  - *Dia 2 (10h15):* "Passa das dez da manhã... O expediente no escritório já começou faz tempo." | *Rep:* "Dez e quinze. Nem liguei pra avisar."
  - *Dia 3 (03h12):* "Três e doze... Não sei se é madrugada de terça ou se esse relógio parou faz tempo." | *Rep:* "Três e doze."

#### Portas (`DoorInteractable.cs` e `Canal420GreyboxBuilder.cs`)
- **Fallback genérico de porta (`DoorInteractable.cs`):**
  - *Trancada:* "Está trancada."
  - *Destrancada:* "Destranquei a porta."
- **Porta do Quarto das Filhas (`Porta_Quarto_Pivot` — padrão Dia 1 e Dia 3 antes da chave):**
  > "Trancada por fora faz tempo. Não tenho nada pra pegar aí dentro."

#### Objetos Permanentes Opcionais da Casa (`Canal420GreyboxBuilder.cs` e `Canal420PropDetailBuilders.cs`)
- **Dentro do Rack da TV (`compartimento_rack_opc`):**
  - *1ª vez:* "Fitas velhas da família e carnês vencidos no fundo do rack." | *Rep:* "Só poeira e papel velho."
- **Estante da Sala (`estante_sala_opc`):**
  - *1ª vez:* "Virei esse retrato pra parede faz meses. Nem sei por que ainda não tirei daqui." | *Rep:* "Deixa virado."
- **Poltrona da Sala (`poltrona_opc`):**
  - *1ª vez:* "A manta dela continua dobrada no braço. Ninguém senta aqui faz tempo." | *Rep:* "Cheiro de pano guardado."
- **Geladeira — Bilhetes e Boletos (`geladeira_bilhetes_opc`):**
  - *1ª vez:* "Boletos vencidos e a lista dela no ímã: leite, maçã, sabonete infantil. Nunca tirei daí." | *Rep:* "A tinta da caneta já tá desbotada."
- **Embaixo da Pia da Cozinha (`armario_pia_cozinha_opc`):**
  - *1ª vez:* "Resto de detergente e jornal velho cobrindo a madeira úmida." | *Rep:* "Cheiro de madeira molhada."
- **Gaveta de Talheres (`gaveta_talheres_opc`):**
  - *1ª vez / Rep:* "Só uso o mesmo garfo e a mesma caneca todo dia."
- **Fogão (`fogao_opc`):**
  - *1ª vez:* "Faz dias que só uso uma boca pra esquentar água pro café." | *Rep:* "Frio."
- **Mesa de Jantar / Copa (`mesa_jantar_opc`):**
  - *1ª vez:* "Cobrança e sujeira." | *Rep:* "Já esteve cheia."
- **Gabinete do Banheiro (`gabinete_banheiro_opc`):**
  - *1ª vez:* "Pano encardido e água sanitária. Faz tempo que não limpo essa casa." | *Rep:* "Nada de útil aqui."
- **Armário de Remédios (`armario_remedios_opc`):**
  - *1ª vez:* "Tomo quase todo dia pra conseguir dormir." | *Rep:* "Metade das caixas já acabou."
- **Box / Chuveiro (`chuveiro_opc`):**
  - *1ª vez:* "Não tomo banho. Sinto que sou observado quando tomo." | *Rep:* "O ralo tá seco."
- **Outro Lado da Cama (`lado_cama_margaret_opc`):**
  - *1ª vez:* "Mesmo sozinho, eu só durmo na minha ponta." | *Rep:* "Não passo da metade."
- **Gaveta do Criado-Mudo de Cian (`gaveta_criado_cian_opc`):**
  - *1ª vez:* "Cartelas vazias e recibo velho." | *Rep:* "Só lixo de remédio."
- **Gaveta do Criado-Mudo de Margaret (`gaveta_margaret_trancada` — `AnimatedContainerInteractable`):**
  - *1ª vez:* "Trancada. Nunca tive a chave desse lado." | *Rep:* "Continua trancada."
- **Guarda-Roupa — Ternos (`guarda_roupa_ternos_opc`):**
  - *1ª vez:* "Cheiro de mofo e naftalina. Uso o mesmo terno toda semana." | *Rep:* "Sempre a mesma roupa."
- **Cômoda — Gaveta Superior (`comoda_quarto_opc`):**
  - *1ª vez:* "Faz meses que só revezo as mesmas três camisas sociais." | *Rep:* "Nada que preste aqui."
- **Cômoda — Gaveta do Meio (`gaveta_comoda_inferior` — `AnimatedContainerInteractable`):**
  - *1ª abertura:* "Inchada de umidade. Só cobertor velho guardado aí embaixo."
- **Cabideiro do Hall (`cabideiro_opc`):**
  - *1ª vez:* "Esse guarda-chuva tá pendurado desde o ano passado." | *Rep:* "Ninguém mexe nisso."
- **Estante do Escritório (`estante_escritorio_opc`):**
  - *1ª vez:* "Cinco anos de livro-caixa em ordem. Aqui dentro pelo menos os números fazem sentido." | *Rep:* "Os números batem. O resto não importa."
- **Formulário de Afastamento na Gaveta da Escrivaninha (`gaveta_afastamento_opc` — após ler Documento #7):**
  - *Pós-leitura:* "Escrevi isso em agosto e larguei no fundo da gaveta. Se eu entregar pro Tavares, tô na rua."
  - *Repetição:* "Quem pede licença lá dentro perde a mesa."
- **Arquivo Metálico — Gaveta do Meio (`arquivo_gaveta_media_opc` — definido em `Canal420PropDetailBuilders.cs`):**
  - *1ª vez:* "Declarações de 92 e 93. Coisa antiga do escritório, tudo em ordem." | *Rep:* "Só arquivo antigo."
- **Arquivo Metálico — Gaveta Alta (`arquivo_gaveta_alta` — definido em `Canal420PropDetailBuilders.cs`):**
  - *Trancada sem chave:* "Não abre. A fechadura tá toda riscada."
  - *Ao destrancar com a chave do casaco:* "A chave do bolso dela serviu."
- **Recorte de Jornal de 1994 (`arquivo_metalico_opc` — definido em `Canal420PropDetailBuilders.cs` — após ler Documento #8):**
  - *Pós-leitura:* "O pedaço onde dizia o desfecho foi arrancado..."
  - *Repetição:* "..."

---

### 1.7 Flashes de Dissociação (Verdade Reprimida)

Textos exibidos em glitch rápido (`InteractionSystem.ShowDissociationGlitchSubtitle`) logo antes da legenda normal:

| Objeto / ID | Dia | Texto do Flash | Origem |
|---|---|---|---|
| **Espelho do Banheiro (`espelho_dia2_opc`)** | Dia 2 | `A PORTA DO FUNDO` | `Canal420GreyboxBuilder.cs` |
| **Espelho do Banheiro (`espelho_dia3_opc`)** | Dia 3 | `NÃO SOU EU` | `Canal420GreyboxBuilder.cs` |
| **Agenda de Margaret (`agenda_margaret_dia3_opc`)** | Dia 3 | `10 DE SETEMBRO - EU TRANQUEI` | `Canal420GreyboxBuilder.cs` |

---

## 2. Documentos Escritos (Leitor Modal)

Todos os **8 documentos** abertos em tela cheia via `InteractionSystem.ShowDocument`:

### Documento 1 — Memorando de Tavares (`olhar_papeis`)
- **Dia / Local:** Dia 1 — Escrivaninha do Escritório (`Papeis_Teste`)
- **Estilo / Carimbo:** `CorporateMemo` → **`COMUNICAÇÃO INTERNA`**
- **Título:** `COMUNICAÇÃO INTERNA Nº 048/95`
- **Texto integral:**
```text
PARA: Cian Nabs - Analista Contábil Pleno
DE: A. Tavares - Gerência Operacional

Cian, os lotes 03, 07 e 11 da conciliação de outubro voltaram da
diretoria com divergência de saldo de novo. Não sei o que está
acontecendo com você nas últimas semanas.

Segunda-feira, às 08h00 em ponto, quero as três pastas refeitas
na minha mesa. Outro atraso desses vai pro Departamento Pessoal.
```
- **Pensamento pós-leitura:** `"Terceira vez que devolvem esse fechamento..."`
- **Pensamento na releitura:** `"Os números estão todos embaralhados."`
- **Origem:** `Canal420GreyboxBuilder.cs`

---

### Documento 2 — Receituário de Clonazepam (`frascos_calmantes_dia2`)
- **Dia / Local:** Dia 2 — Mesa de Centro da Sala (`Frascos_MesaCentro`)
- **Estilo / Carimbo:** `MedicalPrescription` → **`RECEITUÁRIO DE CONTROLE ESPECIAL`**
- **Título:** `NOTIFICAÇÃO DE RECEITA B1 - CONTROLE ESPECIAL`
- **Texto integral:**
```text
PACIENTE: Cian Nabs
MEDICAMENTO: Clonazepam 2 mg - 02 caixas (60 comprimidos)

POSOLOGIA: Tomar 01 comprimido antes de deitar.
NÃO REPETIR SEM NOVA AVALIAÇÃO MÉDICA.

DISPENSADO EM: 24/10/1994  |  FARMÁCIA CENTRAL - SANTOS/SP
```
- **Pensamento pós-leitura:** `"A receita venceu faz quase um ano."`
- **Pensamento na releitura:** `"Os dois frascos tão vazios."`
- **Origem:** `Canal420GreyboxBuilder.cs`

---

### Documento 3 — Rótulo da Fórmula Manipulada (`estimulante_frasco_dia3_opc`)
- **Dia / Local:** Dia 3 — Mesa de Centro da Sala (`Frasco_Estimulante_Dia3`)
- **Estilo / Carimbo:** `MedicalPrescription` → **`RECEITUÁRIO DE CONTROLE ESPECIAL`**
- **Título:** `FÓRMULA MANIPULADA — REGISTRO / FARMÁCIA CENTRAL`
- **Texto integral:**
```text
PACIENTE: Cian Nabs  |  PRONTUÁRIO / PEDIDO Nº 4182
COMPOSIÇÃO POR CÁPSULA:
Fenproporex 25 mg + Cafeína Anidra 200 mg (30 cáps.)

INDICAÇÃO: 01 cápsula pela manhã.
ADVERTÊNCIA: Não associar a outros estimulantes.
Risco de taquicardia, tremor, agitação psicomotora,
insônia severa e confusão sensorial.

[O frasco está derrubado sobre a mesa, com o rótulo borrado de suor e a data ilegível.]
```
- **Pensamento pós-leitura:** `"Fenproporex com cafeína. A data tá raspada... parece etiqueta de farmácia ou de clínica. Preciso engolir uma cápsula antes de apagar de novo."`
- **Pensamento na releitura:** `"Fenproporex com cafeína. O corpo corre, mas a cabeça não desliga."`
- **Origem:** `Canal420GreyboxBuilder.cs`

---

### Documento 4 — Agenda Rasgada de Margaret (`agenda_margaret_dia3_opc`)
- **Dia / Local:** Dia 3 — Criado-Mudo do Quarto do Casal (`Agenda_Margaret_D3`)
- **Estilo / Carimbo:** `HandwrittenNote` → **`ANOTAÇÃO PESSOAL`**
- **Título:** `AGENDA PESSOAL`
- **Texto integral:**
```text
SÁBADO - 10 DE SETEMBRO

- Deixar as meninas na Dona Lúcia às 10h
- Buscar o terno cinza do Cian na lavanderia até meio-dia
- Farmácia: xarope infantil e termômetro
- Ligar pra minha mãe à tarde

Conversar com o Cian hoje de noite.
```
- **Flash de dissociação:** `10 DE SETEMBRO - EU TRANQUEI`
- **Pensamento pós-leitura:** `"Ela escreveu isso no sábado de manhã..."`
- **Pensamento na releitura:** `"Ela só queria conversar comigo naquela noite."`
- **Origem:** `Canal420GreyboxBuilder.cs`

---

### Documento 5 — Bilhete Dobrado de Margaret (`bilhete_margaret_dia3_opc`)
- **Dia / Local:** Dia 3 — Escrivaninha do Escritório (`Bilhete_Margaret_Dia3`)
- **Estilo / Carimbo:** `HandwrittenNote` → **`ANOTAÇÃO PESSOAL`**
- **Título:** `BILHETE DE MARGARET`
- **Texto integral:**
```text
Cian,

Deixei o seu prato tampado no forno desligado, é só esquentar.
Fiquei te esperando até onze e meia, mas as meninas não
aguentaram ficar acordadas.

Quando você terminar aí, encosta a porta do corredor
devagar. Amanhã cedo a gente conversa direito, sem grito na
frente delas.

Margaret
```
- **Pensamento pós-leitura:** `"Tava dobrado debaixo das pastas desde aquela sexta... Nem li o que ela pediu."`
- **Pensamento na releitura:** `"Ela deixou o jantar no forno e foi deitar com as meninas."`
- **Origem:** `Canal420GreyboxBuilder.cs`

---

### Documento 6 — Folha Anterior do Calendário (`calendario_folha_anterior_opc`)
- **Dia / Local:** Dia 3 — Parede do Escritório (folha sob outubro/95)
- **Estilo / Carimbo:** `HandwrittenNote` → **`ANOTAÇÃO PESSOAL`**
- **Título:** `SETEMBRO / 94`
- **Texto integral:**
```text
SEG 05    fechamento mensal               x
TER 06    revisar lote B                  x
QUA 07    feriado                         x
QUI 08    reunião Tavares                 x
SEX 09                                    x
SÁB 10
DOM 11
SEG 12
TER 13
```
- **Pensamento pós-leitura:** `"Folha de setembro do ano passado embaixo do bloco novo... Risquei tudo até sexta-feira dia nove, e parei."`
- **Pensamento na releitura:** `"O último risco quase furou o papel no dia nove."`
- **Origem:** `Canal420GreyboxBuilder.cs`

---

### Documento 7 — Solicitação de Afastamento Médico (`gaveta_afastamento_opc`)
- **Dia / Local:** Casa Base (todos os dias) — Gaveta da Escrivaninha no Escritório (`Folha_Afastamento_Interagir`)
- **Estilo / Carimbo:** `CorporateMemo` → **`COMUNICAÇÃO INTERNA`**
- **Título:** `SOLICITAÇÃO DE AFASTAMENTO MÉDICO - DP`
- **Texto integral:**
```text
COLABORADOR: Cian Nabs | ÁREA: Contabilidade / Auditoria
DATA: 14/08/1995

MOTIVO INFORMADO PELO EMPREGADO:
Crises graves de insônia há mais de quatro meses, dores constantes de cabeça,
lapsos de memória durante conferências e tremores nas mãos ao fim do expediente.

ASSINATURA DO EMPREGADO: ___________________________
VISTO DA GERÊNCIA:        ___________________________
```
- **Pensamento pós-leitura:** `"Escrevi isso em agosto e larguei no fundo da gaveta. Se eu entregar pro Tavares, tô na rua."`
- **Pensamento na releitura:** `"Quem pede licença lá dentro perde a mesa."`
- **Origem:** `Canal420GreyboxBuilder.cs`

---

### Documento 8 — Recorte do Jornal da Baixada (`arquivo_metalico_opc`)
- **Dia / Local:** Casa Base (acessível no Dia 3 com `chave_arquivo_pessoal`) — Gaveta Alta do Arquivo Metálico no Escritório (`Recorte_Jornal_1994`)
- **Estilo / Carimbo:** `NewspaperClipping` → **`RECORTE DE JORNAL`**
- **Título:** `JORNAL DA BAIXADA - Domingo, 11 de setembro de 1994`
- **Texto integral:**
```text
MÃE E DUAS FILHAS SÃO ENCONTRADAS MORTAS NA RUA DAS ACÁCIAS

A Polícia Civil investiga a morte de M. N., de 32 anos, e de suas
duas filhas, de 6 e 8 anos, encontradas ontem à noite (10) dentro de
um sobrado no fim da Rua das Acácias.

Segundo o boletim de ocorrência, vizinhos ouviram gritos por volta das
22h30 e chamaram a PM. Ao arrombarem o quarto dos fundos, trancado pelo
lado de fora, os policiais já encontraram as três sem vida.

O marido, contador de 34 anos, permanecia em silêncio na sala quando a
viatura chegou. O IML recolheu os corpos na madrugada e o suspeito foi
encaminhado sob custódia para avaliação...

[A borda inferior do recorte está rasgada e borrada de umidade.]
```
- **Pensamento pós-leitura:** `"Conduzido sob custódia... A parte de baixo tá rasgada. Eu fui solto? Tô numa clínica? Ou nunca saí dessa sala naquela noite?"`
- **Pensamento na releitura:** `"O pedaço onde dizia o que aconteceu comigo foi arrancado."`
- **Origem:** `Canal420PropDetailBuilders.cs`

---

## 3. Prompts de Interação (`[E]`)

Todos os prompts exibidos na pílula central da mira por `InteractionSystem.cs` (que prefixa `[E] ` automaticamente, salvo overrides já formatados):

### 3.1 Padrões e Fallbacks do Sistema
| Componente | Condição | Prompt Exibido |
|---|---|---|
| `InteractionSystem.cs` | Prompt vazio / fallback | `[E] Examinar` |
| `InteractionSystem.cs` | Padrão para qualquer `IInteractable` | `[E] <prompt>` |
| `InspectableObject.cs` | Releitura de um documento já lido | `[E] Reler: <prompt>` |
| `DoorInteractable.cs` | Porta fechada (destrancada ou trancada sem chave) | `[E] Abrir porta` |
| `DoorInteractable.cs` | Porta trancada, jogador tem a chave | `[E] Destrancar porta` |
| `DoorInteractable.cs` | Porta aberta | `[E] Fechar porta` |

### 3.2 TV de Tubo e Sofá (`TVController.cs` e `SofaTVProxy.cs`)
| Estado / Objeto | Prompt Exibido |
|---|---|
| Dia 1 — TV ou Sofá (`Day1_OffWaitingToSit` / `SofaTVProxy`) | `[E] Sentar no sofá e ligar a TV` |
| Dia 1 — Sentado trocando entre Canais 04 / 09 / 17 *(override)* | `[E] Trocar de canal` |
| Dia 1 — Sentado no Canal 420 (1ª tentativa de sair) *(override)* | `[E] Mudar de canal` |
| Dia 1 — Sentado no Canal 420 (2ª tentativa de sair) *(override)* | `[E] Desligar a TV` |
| Dia 2 — TV desligada (`Day2_OffIdle`) | `[E] Tocar a tela da TV` |
| Dia 2 — Plantão ligado sozinho (`Day2_AutonomousNewsWaiting`) | `[E] Ver o que está passando` |
| Dia 3 — TV desligada no apagão (`Day3_SilentGlass`) | `[E] Olhar a TV` |

### 3.3 Objetos do Dia 1 (`Canal420GreyboxBuilder.cs`)
| ID / Objeto | Prompt |
|---|---|
| `lavar_rosto` (`Pia_Teste`) | `Lavar o rosto` |
| `checar_secretaria` (`Secretaria_Eletronica_D1`) | `Ouvir a secretária eletrônica` |
| `pegar_chave_escritorio` (`Pratinho_Chaves_Copa`) | `Pegar a chave no pratinho` |
| `pilhas_controle_task` (`Pilhas_Controle_D1`) | `Pegar as pilhas` |
| `olhar_papeis` (`Papeis_Teste`) | `Ler os papéis na mesa` *(releitura: `Reler: Ler os papéis na mesa`)* |
| `checar_calendario` (`Calendario_Dia1_Interagir`) | `Olhar o calendário` |
| `quadro_forca_opc` (`Quadro_Forca_Dia1`) | `Olhar os disjuntores` |
| `espelho_dia1_opc` (`Espelho_Dia1`) | `Olhar no espelho` |
| `quadros_familia_d1_opc` | `Olhar as fotografias do corredor` |
| `retrato_margaret_d1_opc` | `Olhar o retrato de Margaret` |
| `guarda_roupa_d1_opc` (`Casaco_Margaret_GuardaRoupa_D1`) | `Olhar o casaco cinza` |

### 3.4 Objetos do Dia 2 (`Canal420GreyboxBuilder.cs`)
| ID / Objeto | Prompt |
|---|---|
| `frascos_calmantes_dia2` (`Frascos_MesaCentro`) | `Olhar os frascos e a receita` *(releitura: `Reler: Olhar os frascos e a receita`)* |
| `pegar_fita_banheiro_dia2` (`Fita_Cassete_D2`) | `Pegar a fita na borda da pia` |
| `secretaria_fita_dia2` (sem fita) | `Olhar a secretária apitando` |
| `secretaria_fita_dia2` (com fita) | `Colocar a fita e ouvir a gravação` |
| `espelho_dia2_opc` (`Espelho_Dia2`) | `Olhar no espelho` |
| `calendario_d2_opc` | `Olhar o calendário` |
| `lanterna_vazia_d2_opc` | `Olhar a lanterna` |
| `quadro_forca_d2_opc` | `Olhar os disjuntores` |
| `casaco_margaret_d2_opc` | `Olhar o casaco cinza` |

### 3.5 Objetos e Puzzles do Dia 3 (`Canal420GreyboxBuilder.cs`, `FallingPictureFrame.cs`, `SafeBoxPuzzle.cs`)
| ID / Objeto | Prompt |
|---|---|
| `pegar_lanterna_dia3` (`Lanterna_Dia3`) | `Pegar a lanterna` |
| `estimulante_frasco_dia3_opc` (`Frasco_Estimulante_Dia3`) | `Olhar o frasco manipulado` *(releitura: `Reler: Olhar o frasco manipulado`)* |
| `engolir_capsula_dia3` (`Capsulas_Estimulante_Dia3`) | `Engolir cápsula a seco` |
| `espelho_dia3_opc` (`Espelho_Dia3`) | `Olhar no espelho` |
| `pegar_fusivel_dia3` (`Fusivel_Ceramico_Pickup_D3`) | `Pegar o fusível` |
| `restaurar_energia_dia3` (sem fusível) | `Olhar o fusível queimado` |
| `restaurar_energia_dia3` (com fusível) | `Trocar o fusível queimado` |
| `chave_arquivo_pessoal_dia3` (`Guarda_Roupa_Casacos_D3`) | `Vasculhar o bolso do casaco` |
| `agenda_margaret_dia3_opc` (`Agenda_Margaret_D3`) | `Ler a folha rasgada na cabeceira` *(releitura: `Reler: ...`)* |
| `bilhete_margaret_dia3_opc` (`Bilhete_Margaret_Dia3`) | `Abrir o bilhete dobrado` *(releitura: `Reler: ...`)* |
| `calendario_d3_opc` | `Olhar o calendário` |
| `calendario_folha_anterior_opc` | `Olhar a folha de baixo do calendário` *(releitura: `Reler: ...`)* |
| `secretaria_d3_opc` | `Olhar a secretária` |
| `FallingPictureFrame` (`Quadro_Margaret_Parede`) | `Ajeitar o retrato` |
| `SafeBoxPuzzle` (`Cofre_Parede_1009` — sem energia) | `Olhar o cofre` |
| `SafeBoxPuzzle` (`Cofre_Parede_1009` — com energia) | `Digitar senha no cofre` |

### 3.6 Objetos Permanentes da Casa Base (`Canal420GreyboxBuilder.cs`, `DayManager.cs`, `Canal420PropDetailBuilders.cs`)
| ID / Objeto | Prompt |
|---|---|
| `Porta_Rua_Interagir` (Dia 1 / Dia 3 pós-cofre) | `Olhar a porta da rua` |
| `Porta_Rua_Interagir` (Dia 2) | `Testar a porta da rua` |
| `Porta_Rua_Interagir` (Dia 3 antes do cofre) | `Forçar a porta da rua` |
| `compartimento_rack_opc` | `Olhar dentro do rack` |
| `estante_sala_opc` | `Olhar a estante` |
| `poltrona_opc` | `Olhar a poltrona` |
| `geladeira_bilhetes_opc` | `Ler os papéis na geladeira` |
| `armario_pia_cozinha_opc` | `Olhar embaixo da pia` |
| `gaveta_talheres_opc` | `Olhar os talheres` |
| `fogao_opc` | `Olhar o fogão` |
| `mesa_jantar_opc` | `Olhar a mesa da copa` |
| `gabinete_banheiro_opc` | `Olhar embaixo da pia` |
| `armario_remedios_opc` | `Olhar o armário de remédios` |
| `chuveiro_opc` | `Olhar o box` |
| `lado_cama_margaret_opc` | `Olhar o outro lado da cama` |
| `gaveta_criado_cian_opc` | `Mexer na gaveta` |
| `despertador_quarto_opc` | `Olhar o despertador` |
| `guarda_roupa_ternos_opc` | `Olhar os ternos` |
| `comoda_quarto_opc` | `Olhar a gaveta da cômoda` |
| `cabideiro_opc` | `Olhar o cabideiro` |
| `estante_escritorio_opc` | `Olhar as pastas na estante` |
| `gaveta_afastamento_opc` | `Ler o formulário na gaveta` *(releitura: `Reler: ...`)* |
| `arquivo_gaveta_media_opc` (`Canal420PropDetailBuilders.cs`) | `Folhear as pastas` |
| `arquivo_metalico_opc` (`Canal420PropDetailBuilders.cs`) | `Ler o recorte de jornal guardado` *(releitura: `Reler: ...`)* |

### 3.7 Contêineres Animados — Gavetas, Armários e Caixas (`AnimatedContainerInteractable`)
| ID do Contêiner | Prompt Fechado | Prompt Aberto | Prompt Especial (Tranca / Chave / Dia 2) | Origem |
|---|---|---|---|---|
| `portas_rack_tv` | `Abrir portas do rack` | `Fechar portas do rack` | — | `GreyboxBuilder` |
| `armario_pia_cozinha` | `Abrir o armário da pia` | `Fechar o armário da pia` | — | `GreyboxBuilder` |
| `gaveta_utilitaria_cozinha` | `Abrir a primeira gaveta da cozinha` | `Fechar a primeira gaveta` | — | `GreyboxBuilder` |
| `gaveta_talheres_cozinha` | `Abrir a gaveta de talheres` | `Fechar a gaveta de talheres` | — | `GreyboxBuilder` |
| `quadro_forca_porta` | `Abrir o quadro de força` | `Fechar o quadro de força` | — | `GreyboxBuilder` |
| `gabinete_pia_banheiro` | `Abrir o gabinete da pia` | `Fechar o gabinete da pia` | — | `GreyboxBuilder` |
| `armario_remedios_banheiro` | `Abrir o armário de remédios` | `Fechar o armário de remédios` | — | `GreyboxBuilder` |
| `gaveta_criado_cian` | `Abrir a gaveta do criado-mudo` | `Fechar a gaveta do criado-mudo` | — | `GreyboxBuilder` |
| `gaveta_margaret_trancada` | *(trancada permanentemente)* | — | `Tentar abrir a gaveta` | `GreyboxBuilder` |
| `guarda_roupa_cian_ternos` | `Abrir as portas dos ternos` | `Fechar as portas dos ternos` | — | `GreyboxBuilder` |
| `guarda_roupa_cian` | `Abrir o guarda-roupa` | `Fechar o guarda-roupa` | *Dia 2 entreaberto:* `Fechar a porta do guarda-roupa` | `GreyboxBuilder` / `DayManager` |
| `gaveta_comoda_superior` | `Abrir a gaveta de cima da cômoda` | `Fechar a gaveta de cima` | — | `GreyboxBuilder` |
| `gaveta_comoda_inferior` | `Abrir a gaveta do meio da cômoda` | `Fechar a gaveta do meio` | — | `GreyboxBuilder` |
| `gaveta_mesa_escritorio` | `Abrir a gaveta da escrivaninha` | `Fechar a gaveta da escrivaninha` | — | `GreyboxBuilder` |
| `caixa_ferramentas_d3` | `Abrir a caixa de ferramentas` | `Fechar a caixa de ferramentas` | — | `GreyboxBuilder` |
| `arquivo_gaveta_media` | `Abrir a gaveta do meio do arquivo` | `Fechar a gaveta do meio` | — | `PropDetailBuilders` |
| `arquivo_gaveta_alta` | `Abrir a gaveta alta do arquivo` | `Fechar a gaveta alta do arquivo` | *Sem chave:* `Tentar abrir a gaveta alta do arquivo`<br>*Com chave:* `Destrancar e abrir a gaveta alta` | `PropDetailBuilders` |

---

## 4. Comandos e Textos do HUD de Tarefas e Outras Interfaces (UI)

### 4.1 HUD de Tarefas (`ObjectiveHUD`)

#### Estrutura Fixa do Painel (`ObjectiveHUD.cs`)
- **Cabeçalho por dia:** `DIA 1` / `DIA 2` / `DIA 3` *(no Dia 4 o painel é ocultado)*
- **Dica de tecla (painel expandido):** `[TAB]`
- **Texto no modo recolhido:** `[TAB] Tarefa`
- **Formatação da tarefa ativa (1 por vez):** `- <texto da tarefa>`

#### Fila Completa de Tarefas por Dia (`DayManager.cs` e `TVController.cs`)

**Dia 1:**
| Ordem | ID da Tarefa | Texto no HUD | Visibilidade Inicial / Observações |
|---|---|---|---|
| 1 | `lavar_rosto` | `Lavar o rosto no banheiro` | Visível *(o `InspectableObject` registra `hudLabel`: `Lavar o rosto na pia do banheiro`)* |
| 2 | `checar_secretaria` | `Ouvir a secretária eletrônica` | Visível *(o `InspectableObject` registra `hudLabel`: `Ouvir a secretária eletrônica no corredor`)* |
| 3 | `pegar_chave_escritorio` | `Achar a chave do escritório na cozinha` | Oculta — revelada ao tentar a porta trancada do escritório |
| 4 | `destrancar_escritorio_dia1` | `Abrir o escritório` | Visível |
| 5 | `olhar_papeis` | `Conferir os papéis na mesa` | Visível |
| 6 | `checar_calendario` | `Confirmar a data no calendário` | Visível |
| 7 | `pilhas_controle_task` | `Pegar pilhas na cozinha` | Oculta — revelada ao tentar ligar a TV sem pilhas |
| 8 | `ligar_tv_dia1` | `Sentar na sala e ligar a TV` | Visível |

**Dia 2:**
| Ordem | ID da Tarefa | Texto no HUD | Atualizações Dinâmicas / Visibilidade |
|---|---|---|---|
| 1 | `frascos_calmantes_dia2` | `Ver os frascos na mesa da sala` | Visível |
| 2 | `pegar_fita_banheiro_dia2` | `Achar a fita da secretária` | Oculta — revelada ao checar a secretária sem fita |
| 3 | `secretaria_fita_dia2` | `Checar a secretária apitando no corredor` | Visível → ao pegar a fita muda para: **`Ouvir a fita na secretária eletrônica`** |
| 4 | `vulto_corredor` | `Ir até o corredor` | Oculta (registrada em segundo plano) |
| 5 | `quadros_familia_alterados` | `Seguir pelo corredor leste` | Oculta (registrada em segundo plano) |
| 6 | `sussurros_porta_trancada` | `Verificar o barulho no fim do corredor` | Oculta — revelada e promovida após ouvir a fita |
| 7 | `assistir_plantao_dia2` | `Ver o que ligou na sala` | Adicionada por `TVController.ActivateDay2AutonomousNews` quando a TV liga sozinha |

**Dia 3:**
| Ordem | ID da Tarefa | Texto no HUD | Atualizações Dinâmicas / Visibilidade |
|---|---|---|---|
| 1 | `pegar_lanterna_dia3` | `Pegar a lanterna na gaveta da cozinha` | Visível |
| 2 | `engolir_capsula_dia3` | `Tomar as pílulas na mesa da sala` | Oculta — revelada ao pegar a lanterna |
| 3 | `restaurar_energia_dia3` | `Checar o quadro de força na cozinha` | Oculta (revelada após tomar a pílula) → ao checar quadro ou pegar fusível muda para: **`Trocar o fusível no quadro de força`** |
| 4 | `pegar_fusivel_dia3` | `Procurar um fusível no banheiro` | Oculta — revelada ao checar o quadro de força sem fusível |
| 5 | `retrato_margaret_dia3` | `Examinar o retrato de Margaret no corredor` | Oculta — revelada após restaurar a energia do corredor |
| 6 | `cofre_1009_dia3` | `Abrir o cofre atrás do retrato` | Oculta — revelada quando o retrato de Margaret cai |
| 7 | `destrancar_porta_dia3` | `Abrir a porta no fim do corredor` | Oculta — revelada ao destrancar o cofre `1009` |
| 8 | `entrar_quarto_dia3` | `Entrar no quarto` | Oculta — revelada ao destrancar a porta do fundo |

**Dia 4:**
- *(HUD de tarefas oculto e limpo.)*

---

### 4.2 HUD da Lanterna (`FlashlightController`)
- **Pílula exibida ao equipar a lanterna no Dia 3:** `[F] Ligar/Desligar`

---

### 4.3 Interface do Leitor de Documentos (`InteractionSystem`)
- **Carimbos superiores por estilo (`DocumentStyle`):**
  - `HandwrittenNote` → `ANOTAÇÃO PESSOAL`
  - `CorporateMemo` → `COMUNICAÇÃO INTERNA`
  - `MedicalPrescription` → `RECEITUÁRIO DE CONTROLE ESPECIAL`
  - `NewspaperClipping` → `RECORTE DE JORNAL`
- **Botão / comando de fechamento:** `Guardar  [E / Botão Direito]`

---

### 4.4 Interface do Cofre Numérico (`SafeBoxPuzzle`)
- **Cabeçalho do painel:** `COFRE CR-90`
- **Visor inicial:** `- - - -`
- **Botões do teclado:** `1`, `2`, `3`, `4`, `5`, `6`, `7`, `8`, `9`, `LIMPAR`, `0`, `SAIR`
- **Dica padrão no rodapé:** `MOD. CR-90  |  [0-9] Digitar   [Backspace] Apagar   [E / ESC] Voltar`
- **Feedback de acerto (`1009`):** Visor `A B E R T O` | Rodapé `Trava liberada.`
- **Feedback de erro genérico:** Visor `E R R O` | Rodapé `Código incorreto.`
- **Feedback de erro ao digitar ano (`1994`, `1995` ou `1992`):** Visor `E R R O` | Rodapé `Não é o ano... São quatro números. Dia e mês.`

---

### 4.5 Menu de Opções (`GameSettingsUI`)
- **Botão no HUD (canto superior direito):** `[ESC] Opções`
- **Título da janela:** `OPÇÕES` | Botão fechar: `✕`
- **Controles e descrições:**
  - `ÁUDIO` — valor percentual `0%` a `100%` (padrão `85%`)
  - `BRILHO` — valor percentual `0%` a `100%` (padrão `50%`)
  - `GRÁFICOS` — opções: `Baixo`, `Médio`, `Alto`, `Ultra`
  - `RESOLUÇÃO` — opções: `2560×1664`, `2560×1440`, `1920×1080`, `Nativa`
  - `MODO DE TELA` — subtítulo: `Tela cheia sem bordas ou modo janela` | estados: `TELA CHEIA` / `JANELA`
  - `SOMBRAS CLARAS` — subtítulo: `Clareia cantos escuros sem luz direta` | estados: `LIGADO` / `DESLIGADO`
  - `VSYNC` — subtítulo: `Sincroniza quadros ao monitor (evita cortes)` | estados: `LIGADO` / `DESLIGADO`
- **Botões inferiores:** `PADRÃO` | `RETOMAR`
- **Rodapé de controles:** `WASD Mover  |  E / Clique Interagir  |  TAB Tarefas  |  ESC Retomar`

---

## 5. Textos Escritos em Props, Rótulos, Papéis 3D e Tela da TV (`PropDetails` / `PrintDetails` / `CRT`)

Todos os textos desenhados em texturas procedurais geradas por **`Canal420PrintDetailsGenerator.cs`**, aplicados aos modelos construídos em **`Canal420PropDetailBuilders.cs`**, mais os textos em tempo real desenhados na tela da TV em **`ProceduralCRTFeed.cs`**.

### 5.1 Calendários de Parede (`Canal420PrintDetailsGenerator.cs`)
| Material / Textura | Onde Aparece | Texto Impresso |
|---|---|---|
| `M_Print_Calendario_Out1995` (`Tex_Calendario_Out1995`) | Parede do Escritório (folha superior) | `OUTUBRO` / `1995` / `D S T Q Q S S` / dias `1` a `31` *(1–28 riscados com X, 29 circulado)* |
| `M_Print_Calendario_Set1994` (`Tex_Calendario_Set1994`) | Parede do Escritório (folha inferior revelada no Dia 3) | `SETEMBRO / 1994` / `D S T Q Q S S` / dias `01` a `30` *(01–09 riscados com X, 10 em diante em branco)* / rodapé `ULTIMA FOLHA GUARDADA` |

### 5.2 Medicamentos, Frascos, Cartelas e Caixas (`Canal420PrintDetailsGenerator.cs`)
| Material / Textura | Onde Aparece | Texto Impresso |
|---|---|---|
| `M_Print_Rotulo_TarjaPreta` (`Tex_Rotulo_TarjaPreta`) | Frascos de calmante na Mesa de Centro (Dia 2) e Armário do Banheiro | `FARMACIA CENTRAL` / `CLONAZEPAM 2MG` / `30 COMPRIMIDOS` / `USO ADULTO - RECEITA B1` / `TARJA PRETA` / `VENDA SOB PRESCRICAO` |
| `M_Print_Cartela_Sedativo` (`Tex_Cartela_Sedativo`) | Cartelas vazias (Mesa de Centro Dia 2, Criado-Mudo de Cian) | `CLONAZEPAM 2MG` / `LOTE 094/B   VAL 96` |
| `M_Print_Rotulo_Manipulado_D3` (`Tex_Rotulo_Manipulado_D3`) | Frasco manipulado derrubado na Mesa de Centro (Dia 3) | `LAB. MANIPULACAO` / `FENPROPOREX 25MG` / `+ CAFEINA 200MG` / `30 CAPSULAS - FORMULA` / `PAC: CIAN NABS  N 4182` / `USO RESTRITO` / `NAO EXCEDER 1 CAPS/DIA` |
| `M_Print_Cartela_Estimulante_D3` (`Tex_Cartela_Estimulante_D3`) | Cartela de cápsulas estimulantes (Dia 3) | `FENPROPOREX 25MG` / `CAFEINA 200MG  L.418` |
| `M_Print_Caixa_Antiacido` (`Tex_Caixa_Antiacido`) | Armário de Remédios do Banheiro | `ANTIACIDO` / `ALIVIO GASTRICO` / `10 COMPRIMIDOS` / `USO ORAL` |

### 5.3 Áudio, Vídeo, Pilhas e Relógios (`Canal420PrintDetailsGenerator.cs`)
| Material / Textura | Onde Aparece | Texto Impresso |
|---|---|---|
| `M_Print_Fita_Cassete` (`Tex_Fita_Cassete_Etiqueta`) | Fita cassete na pia do banheiro (Dia 2) | `MICROCASSETE  C-60` / `SEC. - SET/94` / `GRAVACAO AUTOMATICA` / `LADO A   REC 10/09` |
| `M_Print_Lombada_VHS_0` (`Tex_Lombada_VHS_0`) | Fita VHS no Rack da TV | `VHS` / `NATAL 93 - PRAIA` |
| `M_Print_Lombada_VHS_1` (`Tex_Lombada_VHS_1`) | Fita VHS no Rack da TV | `VHS` / `ANIV. MENINAS 94` |
| `M_Print_Lombada_VHS_2` (`Tex_Lombada_VHS_2`) | Fita VHS no Rack da TV | `VHS` / `GRAVACAO TV / 94` |
| `M_Print_Pilha_AA` (`Tex_Pilha_AA`) | Pilhas AA na gaveta da cozinha (Dia 1) | `+` / `1.5V AA` / `ALCALINA` / `-` |
| `M_Print_Mostrador_Relogio` (`Tex_Mostrador_Relogio_Analogico`) | Despertador do Quarto / Relógio da Estante | Números `1` a `12` / `QUARTZ` / `1994` |
| `M_Print_Clock_Digit_*` | Dígitos LED 7-segmentos (relógios digitais / aparelhos) | Dígitos `2`, `3`, `4`, `0` e `:` |

### 5.4 Embalagens Domésticas — Cozinha e Banheiro (`Canal420PrintDetailsGenerator.cs`)
| Material / Textura | Onde Aparece | Texto Impresso |
|---|---|---|
| `M_Print_Rotulo_Detergente` | Embaixo / sobre a pia da cozinha | `DETERGENTE` / `NEUTRO 500ML` / `IND. BRASILEIRA` |
| `M_Print_Rotulo_AguaSanitaria` | Gabinete do banheiro / cozinha | `AGUA SANITARIA` / `CLORO ATIVO 2%` / `IND. BRASILEIRA` |
| `M_Print_Rotulo_Lata_Cafe` | Bancada / prateleira da cozinha | `CAFE TORRADO` / `TRADICIONAL 500G` / `IND. BRASILEIRA` |
| `M_Print_Rotulo_Pote_Acucar` | Bancada / prateleira da cozinha | `ACUCAR` / `REFINADO ESPECIAL` / `IND. BRASILEIRA` |
| `M_Print_Caixa_Fosforos` | Primeira gaveta da cozinha | `FOSFOROS` / `40 UNIDADES` |
| `M_Print_Rotulo_Shampoo` | Box do chuveiro (banheiro) | `SHAMPOO SUAVE` / `USO DIARIO 350ML` / `IND. BRASILEIRA` |

### 5.5 Papéis, Bilhetes, Jornal, Envelopes e Desenhos 3D (`Canal420PrintDetailsGenerator.cs`)
| Material / Textura | Onde Aparece | Texto Impresso na Face 3D |
|---|---|---|
| `M_Print_Memorando_Tavares` | Escrivaninha do Escritório (Dia 1) | `COMUNICACAO INTERNA 048/95` / `PARA: CIAN NABS (CONTABIL)` / `DE:   A. TAVARES (GERENCIA)` / carimbo `URGENTE` / `SEGUNDA 08H00 NA MINHA MESA.` |
| `M_Print_Agenda_Margaret` | Criado-mudo do Quarto (Dia 3) | `SAB  10/SET` / `- MENINAS NA DONA LUCIA 10H` / `- TERNO CINZA LAVANDERIA` / `- FARMACIA: XAROPE` / `- LIGAR P/ MINHA MAE` / `CONVERSAR COM O CIAN HOJE.` |
| `M_Print_Bilhete_Margaret` | Escrivaninha do Escritório (Dia 3) | `CIAN,` / *(linhas cursivas simuladas)* / `MARGARET` |
| `M_Print_Lista_Geladeira` | Porta da Geladeira | `LISTA DA SEMANA` / `- LEITE` / `- MACA` / `- SABONETE INF.` / `- PAO DE FORMA` / `- CAFE` |
| `M_Print_Aviso_Conta` | Porta da Geladeira | `AVISO DE DEBITO` / `CONTA LUZ / AGUA` / carimbo `VENCIDA` |
| `M_Print_Jornal_Baixada_1994` | Gaveta Alta do Arquivo Metálico (Escritório) | `JORNAL DA BAIXADA` / `DOM, 11 DE SETEMBRO DE 1994` / `N.614` / `TRAGEDIA NA RUA DAS ACACIAS` / `POLICIA ISOLOU RESIDENCIA NA NOITE DE SABADO (10/09)` / legenda `FACHADA ISOLADA - R. ACACIAS` / subtítulos `VIZINHOS RELATAM SILENCIO`, `O MARIDO FOI CONDUZIDO NA MADRUGADA PARA...`, `INQUERITO EM SEGREDO` |
| `M_Print_Envelope_Carta` | Aparador, Mesa da Copa, Rack da TV, Capacho | Selo `95` / `CIAN NABS` / `RUA DAS ACACIAS, 420` / carimbo `VENCIDO` |
| `M_Print_Desenho_Familia` | Parede / mesa do Quarto das Filhas | `MINHA FAMILIA` *(o segundo desenho, `Tex_Desenho_Casa`, é apenas ilustração sem texto)* |

### 5.6 Interações e Documentos Definidos em `Canal420PropDetailBuilders.cs`
Além de construir as malhas detalhadas que recebem os rótulos acima, `Canal420PropDetailBuilders.cs` define diretamente os textos interativos do **Arquivo Metálico do Escritório** (`BuildOfficeFileCabinet`):
- **Gaveta do Meio (`arquivo_gaveta_media` / `arquivo_gaveta_media_opc`):**
  - Prompts: `Abrir a gaveta do meio do arquivo` / `Fechar a gaveta do meio` / `Folhear as pastas`
  - Falas: `"Declarações de 92 e 93. Coisa antiga do escritório, tudo em ordem."` | *Rep:* `"Só arquivo antigo."`
- **Gaveta Alta Trancada (`arquivo_gaveta_alta`):**
  - Prompts: `Tentar abrir a gaveta alta do arquivo` / `Destrancar e abrir a gaveta alta` / `Abrir a gaveta alta do arquivo` / `Fechar a gaveta alta do arquivo`
  - Falas: `"Não abre. A fechadura tá toda riscada, parece que tentei forçar com alguma coisa."` (trancada) / `"A chavinha do bolso dela serviu certinho."` (destrancando)
- **Recorte de Jornal 1994 (`Recorte_Jornal_1994` / `arquivo_metalico_opc`):**
  - Prompt: `Ler o recorte de jornal guardado`
  - Documento completo listado em [Documento 8 — Recorte do Jornal da Baixada](#documento-8--recorte-do-jornal-da-baixada-arquivo_metalico_opc).

### 5.7 Textos na Tela da TV CRT — OSD e Tickers (`ProceduralCRTFeed.cs`)
| Modo / Canal | Carimbo OSD Superior | Textos e Tickers na Tela |
|---|---|---|
| **Canal 04 (`BroadcastChannel04`)** | `CH 04` | `AO VIVO` / `JORNAL` / `NOITE` / ticker: `BAIXADA - CORTES NO SETOR E BALANCOS DA SEMANA   SANTOS 19C` |
| **Canal 09 (`BroadcastChannel09`)** | `CH 09` | `BAIXADA 19C  CHUVA E NEVOEIRO` |
| **Canal 17 (`BroadcastChannel17`)** | `CH 17` | `SO HOJE` / ticker: `LIGUE JA 0800-1995   ULTIMAS UNIDADES` / tela de relógio: `HORA CERTA` |
| **Plantão 1994 / Bleed (`Canal420Ghost` & Plantão Dia 2)** | `-- --` *(o número `420` nunca aparece no OSD)* | `PLANTAO` / `10/09/94  23:42` / `R. ACACIAS` |
