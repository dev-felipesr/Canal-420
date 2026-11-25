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
  > "Já tá escuro... preciso lavar o rosto."
- **Pia do Banheiro (`lavar_rosto` — `Pia_Teste`):**
  - *1ª vez:* "Nem água fria tá tirando essa dor de cabeça."
  - *Repetição:* "Não tá adiantando nada."
- **Secretária Eletrônica (`checar_secretaria` — `Secretaria_Eletronica_D1`):**
  - *1ª vez:* `[BIP] "Cian, aqui é o Tavares. O lote de outubro voltou errado de novo. Não sei o que tá acontecendo com você, mas segunda às oito eu quero isso resolvido na minha sala."`
  - *Repetição:* "Hoje é domingo, não vou ficar ouvindo isso."
- **Porta do Escritório (`Porta_Escritorio_Pivot` — trancada sem chave):**
  > "Tá trancada. Eu não lembro de ter passado a chave."
- **Pratinho de Chaves na Copa (`pegar_chave_escritorio` — `Pratinho_Chaves_Copa`):**
  > "Ah, a chave do escritório tava aqui."
- **Porta do Escritório (`Porta_Escritorio_Pivot` — ao destrancar):**
  > "Tá quente aqui dentro."
- **Papéis na Escrivaninha (`olhar_papeis` — `Papeis_Teste` — após ler Documento #1):**
  - *Pós-leitura:* "De novo isso... eu tenho certeza que já tinha conferido."
  - *Repetição:* "Não tô conseguindo acompanhar esses números."
- **Calendário do Escritório (`checar_calendario` — `Calendario_Dia1_Interagir`):**
  - *1ª vez:* "Dia vinte e nove... passou outra semana e eu nem vi."
  - *Repetição:* "Amanhã já é segunda de novo."
- **Tentar sentar na TV antes de concluir as tarefas do Dia 1 (`DayManager.cs`):**
  > "Se eu sentar agora eu vou apagar. Melhor resolver o resto antes."
- **Tentar sentar na TV sem pilhas no controle (`DayManager.cs`):**
  > "O controle tá sem pilha... acho que tem na lanterna da cozinha."
- **Pilhas na Primeira Gaveta da Cozinha (`pilhas_controle_task` — `Pilhas_Controle_D1`):**
  > "Acho que essas ainda funcionam."

#### Sequência da TV de Tubo no Dia 1 (`TVController.cs`)
- **Canal 04 (Telejornal):**
  > `[CANAL 04]: "...só neste semestre já foram fechadas mais de quatro mil vagas em escritórios no estado..."`
- **Canal 09 (Previsão do Tempo):**
  > `[CANAL 09]: "...a neblina deve continuar fechada na Baixada até quarta-feira, com baixa visibilidade na serra..."`
- **Canal 17 (Televendas):**
  > `[CANAL 17]: "...café pronto em quatro minutos! Ligue agora, 0800..."`
- **Canal 420 — 1ª sintonização:**
  > "Que canal é esse?... Esse barulho tá me dando tontura."
- **Canal 420 — 2ª tentativa (trocar de canal):**
  > "Não tá mudando..."
- **Canal 420 — 3ª tentativa (desligar / apagão para o Dia 2):**
  > "Não desliga... não tô conseguindo levantar o braço."

#### Inspeções Opcionais do Dia 1 (`Canal420GreyboxBuilder.cs`)
- **Quadro de Força (`quadro_forca_opc` — `Quadro_Forca_Dia1`):**
  - *1ª vez:* "Tá tudo normal aqui."
  - *Repetição:* "Tudo ligado."
- **Espelho do Banheiro (`espelho_dia1_opc` — `Espelho_Dia1`):**
  - *1ª vez:* "Olha a minha cara... preciso dormir."
  - *Repetição:* "Melhor não ficar olhando."
- **Fotografias do Corredor (`quadros_familia_d1_opc`):**
  - *1ª vez:* "Faz tempo que eu passo aqui e nem olho pra isso."
  - *Repetição:* "Não quero ficar parado olhando."
- **Retrato de Margaret (`retrato_margaret_d1_opc`):**
  - *1ª vez:* "Tá cheio de poeira... faz tempo que ninguém limpa."
  - *Repetição:* "Precisa limpar esse vidro."
- **Casaco Cinza de Margaret no Guarda-Roupa (`guarda_roupa_d1_opc` — `Casaco_Margaret_GuardaRoupa_D1`):**
  - *1ª vez:* "As roupas dela ainda tão aqui... eu não mexo nesse lado."
  - *Repetição:* "Que cheiro forte..."

---

### 1.3 Dia 2 — O Efeito Depressor (Segunda-feira)

#### Despertar e Fluxo Obrigatório
- **Ao acordar no Sofá (`DayManager.cs`):**
  > "Que gosto ruim na boca... que horas são?"
- **Frascos e Receituário na Mesa de Centro (`frascos_calmantes_dia2` — `Frascos_MesaCentro` — após ler Documento #2):**
  - *Pós-leitura:* "Essa receita é do ano passado."
  - *Repetição:* "Os dois frascos tão vazios."
- **Secretária Eletrônica sem fita (`secretaria_fita_dia2` — `Secretaria_Eletronica_D2`):**
  > "Tá sem a fita, só nesse apito... onde é que eu deixei isso?"
- **Fita Cassete na Borda da Pia (`pegar_fita_banheiro_dia2` — `Fita_Cassete_D2`):**
  > "Tá toda molhada."
- **Secretária Eletrônica com a fita (`secretaria_fita_dia2` — `Secretaria_Eletronica_D2`):**
  - *1ª vez:* `[BIP] Sábado, 22h47. ... Tem alguém respirando na gravação.`
  - *Repetição:* "Quem deixou isso gravado?"

#### Eventos Psicológicos, Corredor e Espelho no Dia 2
- **Gatilho 1 — Vulto no Corredor (`vulto_corredor` — `Trigger_1_VultoCorredor`):**
  > "Ei, tem alguém aí?... Não, não tem ninguém. É esse remédio."
- **Gatilho 2 — Quadros Familiares Rachando (`quadros_familia_alterados` — `Trigger_2_QuadrosFamilia`):**
  > "Que barulho foi esse?... Os quadros quebraram."
- **Gatilho 3 — Sussurros atrás da Porta (`pre_sussurros_corredor` — `Trigger_3_SussurrosPorta`):**
  - *(Sem legenda — apenas áudio abafado de sussurros e canção de ninar.)*
- **Porta Trancada do Fundo no Dia 2 (`Porta_Quarto_Pivot` — configurado por `DayManager.cs`):**
  - *Antes de ouvir a fita da secretária:* "Tá trancada... não vai abrir."
  - *Após ouvir a fita da secretária:* "Não tem ninguém aí dentro..."
- **Aparição no Espelho do Banheiro (`MirrorHauntController.cs`):**
  - *Ao se virar após ver o vulto na porta pelo reflexo:* "Espera... tinha alguém na porta."
  - *Discrepância do estado da porta no reflexo (se a porta física está fechada):* "Ué... no espelho ela tava aberta."
  - *Discrepância do estado da porta no reflexo (se a porta física está aberta):* "No espelho a porta tava quase fechada..."
- **Guarda-Roupa Entreaberto Sozinho no Dia 2 (`AnimatedContainerInteractable.cs` / `DayManager.cs`):**
  > "Me lembro de ter fechado essa porta ontem."

#### Sequência da TV Autônoma no Dia 2 (`TVController.cs`)
- **Tocar na TV enquanto desligada (`Day2_OffIdle`):**
  > "Ainda tá quente... eu não lembro quando desliguei."
- **TV ligando sozinha na sala (`ActivateDay2AutonomousNews`):**
  - *Se o jogador já estiver na sala:* "Ligou sozinha?"
  - *Se o jogador estiver fora da sala:* "Tem alguém na sala?"
- **Plantão Especial de 1994 (Parte 1):**
  > `[PLANTÃO]: "...a Polícia Civil isolou agora à noite uma casa no fim da Rua das Acácias, depois que vizinhos ligaram pro 190 ouvindo gritos lá dentro..."`
- **Plantão Especial de 1994 (Parte 2):**
  > `[PLANTÃO]: "...a mulher e as duas crianças foram encontradas mortas no quarto dos fundos, trancado por fora. O marido continuava sentado na sala."`
- **Reação de Cian:**
  > "Mortas?... Dez de setembro de noventa e quatro. Minha cabeça tá doendo demais, não consigo entender isso... eu tô no inferno?"
- **Invasão do Canal 420 (transição para o Dia 3):**
  > "Ah, esse canal... Ele me faz sentir bem."

#### Inspeções Opcionais do Dia 2 (`Canal420GreyboxBuilder.cs`)
- **Espelho do Banheiro (`espelho_dia2_opc` — `Espelho_Dia2`):**
  - *Flash de dissociação:* `A PORTA DO FUNDO`
  - *1ª vez:* "Tô com a vista pesada... parece que nem acordei direito."
  - *Repetição:* "Não quero ficar me olhando."
- **Calendário do Escritório (`calendario_d2_opc`):**
  - *1ª vez:* "Hoje é segunda, nem liguei para avisar."
  - *Repetição:* "Aviso depois."
- **Lanterna Vazia na Gaveta da Cozinha (`lanterna_vazia_d2_opc`):**
  - *1ª vez:* "Tá sem pilha, eu tirei ontem."
  - *Repetição:* "Continua sem pilha."
- **Quadro de Força (`quadro_forca_d2_opc`):**
  - *1ª vez:* "Tá fazendo um barulho estranho aqui dentro."
  - *Repetição:* "Melhor não mexer nisso."
- **Casaco Cinza de Margaret (`casaco_margaret_d2_opc`):**
  - *1ª vez:* "A manga encostou no meu braço... tá gelada."
  - *Repetição:* "Deixa fechado."

---

### 1.4 Dia 3 — Apagão, Estimulantes e Revelação (Terça-feira, madrugada)

#### Despertar e Bloqueios Iniciais (`DayManager.cs`)
- **Ao acordar no escuro na Sala (`DayManager.cs`):**
  > "Que que aconteceu?... Acabou a luz. Preciso achar a lanterna."
- **Tentar mexer no Quadro de Força ou Retrato de Margaret sem a lanterna:**
  > "Não dá pra ver nada aqui. Preciso da lanterna."
- **Tentar mexer nas Cápsulas Estimulantes sem a lanterna:**
  > "Tá escuro demais, não consigo nem ver o que tô pegando."
- **Tentar mexer no Quadro de Força ou Retrato de Margaret antes de engolir a 1ª cápsula:**
  > "Minha vista tá escurecendo de novo... preciso tomar aquelas pílulas na sala antes que eu apague."
- **Tentar tomar cápsula fora de contexto:**
  > "Agora não."

#### Progressão Principal do Dia 3
- **Lanterna na Primeira Gaveta da Cozinha (`pegar_lanterna_dia3` — `Lanterna_Dia3`):**
  > "Espera... eu tinha tirado as pilhas dessa lanterna. Como é que ela tá acendendo?"
- **Frasco Manipulado na Mesa de Centro (`estimulante_frasco_dia3_opc` — `Frasco_Estimulante_Dia3` — após ler Documento #3):**
  - *Pós-leitura:* "A data tá raspada... nem lembro de ter comprado isso. Preciso tomar uma logo."
  - *Repetição:* "Fenproporex com cafeína."
- **Cápsulas Estimulantes (`engolir_capsula_dia3` — `Capsulas_Estimulante_Dia3`):**
  - *1ª cápsula:* "Que gosto horrível... pelo menos a tontura passou."
  - *2ª cápsula (última):* "Essa era a última."
  - *Se tentar repetir durante cooldown:* "Meu coração já tá disparado, melhor esperar um pouco."
- **Quadro de Força sem fusível (`restaurar_energia_dia3` — `Quadro_Forca_Dia3`):**
  > "O fusível do corredor queimou. Acho que tem outro na caixa de ferramentas."
- **Fusível na Caixa de Ferramentas do Banheiro (`pegar_fusivel_dia3` — `Fusivel_Ceramico_Pickup_D3`):**
  > "Tem um aqui no fundo. Ainda tá inteiro."
- **Quadro de Força com fusível (`restaurar_energia_dia3` — `Quadro_Forca_Dia3`):**
  - *Ao trocar:* "Voltou... mas só acendeu o corredor."
  - *Repetição:* "Já tá ligado."
- **Retrato de Margaret no Corredor Leste (`FallingPictureFrame.cs` — `Quadro_Margaret_Parede`):**
  > "O arame soltou. Eu não queria me lembrar desse cofre."
- **Cofre de Parede (`SafeBoxPuzzle.cs` — `Cofre_Parede_1009`):**
  - *Sem energia no corredor:* "Não acende... tá sem energia nessa parede."
  - *Ao digitar `1009` e abrir:* "A chave... é a da porta do fundo."
- **Chave Escondida no Casaco Cinza (`chave_arquivo_pessoal_dia3` — `Guarda_Roupa_Casacos_D3`):**
  > "Tem uma chave aqui no bolso dela... parece a da gaveta do meu arquivo."

#### Pistas Documentais e Inspeções Opcionais do Dia 3
- **Agenda Rasgada na Cabeceira (`agenda_margaret_dia3_opc` — após ler Documento #4):**
  - *Flash de dissociação:* `10/09 — NÃO ABRE`
  - *Pós-leitura:* "Ela escreveu isso no sábado de manhã..."
  - *Repetição:* "Ela só queria conversar."
- **Bilhete Dobrado no Escritório (`bilhete_margaret_dia3_opc` — após ler Documento #5):**
  - *Pós-leitura:* "Tava aqui debaixo das pastas esse tempo todo... eu nem cheguei a ler."
  - *Repetição:* "Eu deixei aqui sem abrir."
- **Folha de Baixo do Calendário (`calendario_folha_anterior_opc` — após ler Documento #6):**
  - *Pós-leitura:* "A folha de setembro do ano passado ainda tava aqui embaixo... nem lembrava disso."
  - *Repetição:* "Não tem nada marcado depois do dia nove."
- **Calendário do Escritório (`calendario_d3_opc`):**
  - *1ª vez:* "Um monte de dia riscado... eu já nem sei mais em que mês eu tô."
  - *Repetição:* "Outubro em cima de setembro."
- **Secretária Eletrônica (`secretaria_d3_opc`):**
  - *1ª vez:* "Eu puxei da tomada porque não aguentava mais esse apito."
  - *Repetição:* "Tá fora da tomada."
- **Evento Psicológico — LED Fantasma na Secretária (`PsychologicalHorrorDirector.cs`, após ler o recorte de jornal):**
  > "Mas... ela tá fora da tomada."
- **TV Desligada no Dia 3 (`TVController.cs` — `Day3_SilentGlass`):**
  > "Não tem luz na casa, mas o vidro ainda tá estalando."
- **Espelho do Banheiro (`espelho_dia3_opc` — `Espelho_Dia3`):**
  - *Flash de dissociação:* `VOCÊ OUVIU`
  - *1ª vez:* "Olha o tamanho da minha pupila... tem alguma coisa errada comigo."
  - *Repetição:* "Tá escuro demais pra ver direito."

#### Clímax — Destrancando a Porta do Quarto das Filhas (`DayManager.cs`)
- **Ao destrancar `Porta_Quarto_Pivot` com a chave do cofre:**
  > "..."
- **Se Cian hesitar na soleira por 5 segundos sem entrar (`RunMargaretDoorTrapSequenceRoutine`):**
  > "Não tem barulho nenhum lá dentro..."
- **Se Cian tentar recuar de volta pelo corredor (`z < 3.10`):**
  > "Não adianta tentar voltar agora."
- **Ao ver Margaret dentro do quarto (antes do ataque):**
  > "Margaret..."

---

### 1.5 Dia 4 — Epílogo / Prelúdio (Quarto das Filhas)

*(Cian está sentado dentro do quarto iluminado pela manhã; movimentação bloqueada, olhar restrito.)*

- **Fala 1 (abertura do Dia 4 — `DayManager.cs`):**
  > "A coberta ainda tá dobrada no pé da cama, do jeito que ela deixou."
- **Fala 2 (confissão — `DayManager.cs`):**
  > "Eu passei a chave por fora... tomei os remédios e deixei a TV alta até não ouvir mais nada."
- **Fala 3 (encerramento — `DayManager.cs`):**
  > "Nem sei mais que ano é hoje... acho que eu nunca saí daqui."

---

### 1.6 Objetos Permanentes da Casa Base, Portas e Contêineres

#### Objetos Dinâmicos por Dia (`DayManager.cs`)
- **Porta da Rua (`Porta_Rua_Interagir`):**
  - *Base (`Canal420GreyboxBuilder.cs`):* "Duas voltas na chave e a corrente passada. Não quero abrir pra ninguém." | *Rep:* "Um monte de folheto acumulado por baixo."
  - *Dia 1:* "Duas voltas na chave e a corrente. A rua tá vazia... não vou abrir isso." | *Rep:* "Cheio de folheto velho por baixo."
  - *Dia 2:* "A corrente tá travada... e eu nem tenho força pra mexer nisso agora." | *Rep:* "Não consigo nem pensar em sair na rua."
  - *Dia 3 (antes de abrir o cofre):* "Não adianta tentar abrir agora... não é lá fora que isso vai se resolver." | *Rep:* "Não vou sair por aqui."
  - *Dia 3 (após pegar a chave do cofre):* "Essa chave não é daqui. Eu sei qual porta ela abre." | *Rep:* "Não é essa porta."
- **Despertador do Quarto (`despertador_quarto_opc`):**
  - *Dia 1 (17h40):* "Cinco e quarenta. Já parece tarde da noite." | *Rep:* "Cinco e quarenta."
  - *Dia 2 (10h15):* "Já passa das dez... perdi a hora do escritório." | *Rep:* "Dez e quinze. E eu nem liguei pra avisar."
  - *Dia 3 (03h12):* "Três e doze... será que esse relógio parou?" | *Rep:* "Continua marcando três e doze."

#### Portas (`DoorInteractable.cs` e `Canal420GreyboxBuilder.cs`)
- **Fallback genérico de porta (`DoorInteractable.cs`):**
  - *Trancada:* "Tá trancada."
  - *Destrancada:* "Abriu."
- **Porta do Quarto das Filhas (`Porta_Quarto_Pivot` — padrão Dia 1 e Dia 3 antes da chave):**
  > "Tá trancada faz tempo. Não tenho nada pra fazer aí dentro."

#### Objetos Permanentes Opcionais da Casa (`Canal420GreyboxBuilder.cs` e `Canal420PropDetailBuilders.cs`)
- **Dentro do Rack da TV (`compartimento_rack_opc`):**
  - *1ª vez:* "Umas fitas velhas e um monte de conta vencida." | *Rep:* "Só poeira e papel."
- **Estante da Sala (`estante_sala_opc`):**
  - *1ª vez:* "Eu virei esse retrato pra parede... nem lembro quando foi." | *Rep:* "Deixa assim."
- **Poltrona da Sala (`poltrona_opc`):**
  - *1ª vez:* "A manta dela ainda tá dobrada aí. Faz tempo que ninguém senta." | *Rep:* "Cheiro de roupa guardada."
- **Geladeira — Bilhetes e Boletos (`geladeira_bilhetes_opc`):**
  - *1ª vez:* "Um monte de conta vencida... e a lista de mercado dela no ímã. Nunca tirei." | *Rep:* "A tinta já tá quase apagando."
- **Embaixo da Pia da Cozinha (`armario_pia_cozinha_opc`):**
  - *1ª vez:* "Só resto de detergente e jornal velho." | *Rep:* "Cheiro de madeira úmida."
- **Gaveta de Talheres (`gaveta_talheres_opc`):**
  - *1ª vez / Rep:* "Acabo usando sempre o mesmo garfo e a mesma caneca."
- **Fogão (`fogao_opc`):**
  - *1ª vez:* "Só tenho usado uma boca pra esquentar água do café." | *Rep:* "Tá frio."
- **Mesa de Jantar / Copa (`mesa_jantar_opc`):**
  - *1ª vez:* "Cobrança e sujeira." | *Rep:* "Já esteve cheia."
- **Gabinete do Banheiro (`gabinete_banheiro_opc`):**
  - *1ª vez:* "Pano sujo e água sanitária. Faz tempo que não limpo nada aqui." | *Rep:* "Não tem nada que eu precise."
- **Armário de Remédios (`armario_remedios_opc`):**
  - *1ª vez:* "Sem esses remédios eu não consigo dormir." | *Rep:* "Quase todas as caixas já acabaram."
- **Box / Chuveiro (`chuveiro_opc`):**
  - *1ª vez:* "Eu evito entrar aí... fico sentindo que tem alguém me olhando." | *Rep:* "O ralo tá seco."
- **Outro Lado da Cama (`lado_cama_margaret_opc`):**
  - *1ª vez:* "Mesmo sozinho, eu só consigo dormir na minha ponta." | *Rep:* "Não passo pro lado de lá."
- **Gaveta do Criado-Mudo de Cian (`gaveta_criado_cian_opc`):**
  - *1ª vez:* "Só cartela vazia e recibo velho." | *Rep:* "Cheio de resto de remédio."
- **Gaveta do Criado-Mudo de Margaret (`gaveta_margaret_trancada` — `AnimatedContainerInteractable`):**
  - *1ª vez:* "Tá trancada. Eu nunca tive a chave desse lado." | *Rep:* "Não abre."
- **Guarda-Roupa — Ternos (`guarda_roupa_ternos_opc`):**
  - *1ª vez:* "Cheiro de mofo... acabo usando sempre o mesmo terno." | *Rep:* "Sempre a mesma roupa."
- **Cômoda — Gaveta Superior (`comoda_quarto_opc`):**
  - *1ª vez:* "Só as mesmas três camisas de sempre." | *Rep:* "Não tem nada aqui."
- **Cômoda — Gaveta do Meio (`gaveta_comoda_inferior` — `AnimatedContainerInteractable`):**
  - *1ª abertura:* "Tá difícil de abrir... só tem cobertor velho aqui embaixo."
- **Cabideiro do Hall (`cabideiro_opc`):**
  - *1ª vez:* "Esse guarda-chuva tá aí desde o ano passado." | *Rep:* "Ninguém mexe nisso."
- **Estante do Escritório (`estante_escritorio_opc`):**
  - *1ª vez:* "Cinco anos de arquivo em ordem. Pelo menos os números fazem sentido." | *Rep:* "Aqui dentro as coisas ainda batem."
- **Formulário de Afastamento na Gaveta da Escrivaninha (`gaveta_afastamento_opc` — após ler Documento #7):**
  - *Pós-leitura:* "Preenchi isso em agosto e larguei aí. Se eu entregar pro Tavares, tô na rua."
  - *Repetição:* "Nunca tive coragem de assinar."
- **Arquivo Metálico — Gaveta do Meio (`arquivo_gaveta_media_opc` — definido em `Canal420PropDetailBuilders.cs`):**
  - *1ª vez:* "Papéis de 92 e 93. Tá tudo em ordem aqui." | *Rep:* "Só faltam as coisas de 94."
- **Arquivo Metálico — Gaveta Alta (`arquivo_gaveta_alta` — definido em `Canal420PropDetailBuilders.cs`):**
  - *Trancada sem chave:* "Não abre. A fechadura tá toda riscada."
  - *Ao destrancar com a chave do casaco:* "A chave do bolso dela serviu."
- **Recorte de Jornal de 1994 (`arquivo_metalico_opc` — definido em `Canal420PropDetailBuilders.cs` — após ler Documento #8):**
  - *Pós-leitura:* "O final foi arrancado."
  - *Repetição:* "..."

---

### 1.7 Flashes de Dissociação (Verdade Reprimida)

Textos exibidos em glitch rápido (`InteractionSystem.ShowDissociationGlitchSubtitle`) logo antes da legenda normal:

| Objeto / ID | Dia | Texto do Flash | Origem |
|---|---|---|---|
| **Espelho do Banheiro (`espelho_dia2_opc`)** | Dia 2 | `A PORTA DO FUNDO` | `Canal420GreyboxBuilder.cs` |
| **Espelho do Banheiro (`espelho_dia3_opc`)** | Dia 3 | `VOCÊ OUVIU` | `Canal420GreyboxBuilder.cs` |
| **Agenda de Margaret (`agenda_margaret_dia3_opc`)** | Dia 3 | `10/09 — NÃO ABRE` | `Canal420GreyboxBuilder.cs` |

---

## 2. Documentos Escritos (Leitor Modal)

Todos os **8 documentos** abertos em tela cheia via `InteractionSystem.ShowDocument`:

### Documento 1 — Memorando de Tavares (`olhar_papeis`)
- **Dia / Local:** Dia 1 — Escrivaninha do Escritório (`Papeis_Teste`)
- **Estilo / Carimbo:** `CorporateMemo` → **`COMUNICAÇÃO INTERNA`**
- **Título:** `COMUNICAÇÃO INTERNA Nº 048/95`
- **Texto integral:**
```text
PARA: Cian Nabs — Contabilidade
DE: A. Tavares — Gerência

Cian, os lotes 03, 07 e 11 de outubro voltaram da diretoria de novo.
Você lançou números repetidos do mês passado e errou as datas.

Segunda às 8h quero os três refeitos na minha mesa.
Se acontecer outra vez, vou passar isso pro DP.
```
- **Pensamento pós-leitura:** `"De novo isso... eu tenho certeza que já tinha conferido."`
- **Pensamento na releitura:** `"Não tô conseguindo acompanhar esses números."`
- **Origem:** `Canal420GreyboxBuilder.cs`

---

### Documento 2 — Receituário de Clonazepam (`frascos_calmantes_dia2`)
- **Dia / Local:** Dia 2 — Mesa de Centro da Sala (`Frascos_MesaCentro`)
- **Estilo / Carimbo:** `MedicalPrescription` → **`RECEITUÁRIO DE CONTROLE ESPECIAL`**
- **Título:** `NOTIFICAÇÃO DE RECEITA B1 — CONTROLE ESPECIAL`
- **Texto integral:**
```text
PACIENTE: Cian Nabs
MEDICAMENTO: Clonazepam 2 mg - 02 caixas (60 comprimidos)

POSOLOGIA: 01 comprimido à noite, antes de dormir.
Não repetir sem nova consulta.

DATA: 24/10/1994  |  FARMÁCIA CENTRAL — SANTOS/SP
```
- **Pensamento pós-leitura:** `"Essa receita é do ano passado."`
- **Pensamento na releitura:** `"Os dois frascos tão vazios."`
- **Origem:** `Canal420GreyboxBuilder.cs`

---

### Documento 3 — Rótulo da Fórmula Manipulada (`estimulante_frasco_dia3_opc`)
- **Dia / Local:** Dia 3 — Mesa de Centro da Sala (`Frasco_Estimulante_Dia3`)
- **Estilo / Carimbo:** `MedicalPrescription` → **`RECEITUÁRIO DE CONTROLE ESPECIAL`**
- **Título:** `FÓRMULA MANIPULADA — REGISTRO / FARMÁCIA CENTRAL`
- **Texto integral:**
```text
PACIENTE: Cian Nabs | PEDIDO Nº 4182
FÓRMULA: Fenproporex 25 mg + Cafeína 200 mg (30 cápsulas)

MODO DE USAR: 01 cápsula pela manhã.
ATENÇÃO: Não tomar com outros remédios ou café.
Pode causar falta de ar, tremor, insônia e confusão.

DATA: --/--/--
```
- **Pensamento pós-leitura:** `"A data tá raspada... nem lembro de ter comprado isso. Preciso tomar uma logo."`
- **Pensamento na releitura:** `"Fenproporex com cafeína."`
- **Origem:** `Canal420GreyboxBuilder.cs`

---

### Documento 4 — Agenda Rasgada de Margaret (`agenda_margaret_dia3_opc`)
- **Dia / Local:** Dia 3 — Criado-Mudo do Quarto do Casal (`Agenda_Margaret_D3`)
- **Estilo / Carimbo:** `HandwrittenNote` → **`ANOTAÇÃO PESSOAL`**
- **Título:** `AGENDA PESSOAL`
- **Texto integral:**
```text
SÁBADO — 10 DE SETEMBRO

- Deixar as meninas na Dona Lúcia às 10h
- Pegar o terno cinza do Cian na lavanderia antes do meio-dia
- Passar na farmácia (xarope e termômetro)
- Ligar pra minha mãe à tarde

Preciso conversar com o Cian hoje à noite.
```
- **Flash de dissociação:** `10/09 — NÃO ABRE`
- **Pensamento pós-leitura:** `"Ela escreveu isso no sábado de manhã..."`
- **Pensamento na releitura:** `"Ela só queria conversar."`
- **Origem:** `Canal420GreyboxBuilder.cs`

---

### Documento 5 — Bilhete Dobrado de Margaret (`bilhete_margaret_dia3_opc`)
- **Dia / Local:** Dia 3 — Escrivaninha do Escritório (`Bilhete_Margaret_Dia3`)
- **Estilo / Carimbo:** `HandwrittenNote` → **`ANOTAÇÃO PESSOAL`**
- **Título:** `BILHETE DE MARGARET`
- **Texto integral:**
```text
Cian,

Deixei seu prato tampado no forno, é só esquentar.
Esperei até onze e meia, mas as meninas já estavam caindo de sono.

Quando você terminar aí, fecha a porta devagar pra não acordar elas.
Amanhã a gente conversa direito, sem gritar na frente das meninas.

Margaret
```
- **Pensamento pós-leitura:** `"Tava aqui debaixo das pastas esse tempo todo... eu nem cheguei a ler."`
- **Pensamento na releitura:** `"Eu deixei aqui sem abrir."`
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
- **Pensamento pós-leitura:** `"A folha de setembro do ano passado ainda tava aqui embaixo... nem lembrava disso."`
- **Pensamento na releitura:** `"Não tem nada marcado depois do dia nove."`
- **Origem:** `Canal420GreyboxBuilder.cs`

---

### Documento 7 — Solicitação de Afastamento Médico (`gaveta_afastamento_opc`)
- **Dia / Local:** Casa Base (todos os dias) — Gaveta da Escrivaninha no Escritório (`Folha_Afastamento_Interagir`)
- **Estilo / Carimbo:** `CorporateMemo` → **`COMUNICAÇÃO INTERNA`**
- **Título:** `SOLICITAÇÃO DE AFASTAMENTO MÉDICO - DP`
- **Texto integral:**
```text
FUNCIONÁRIO: Cian Nabs | SETOR: Contabilidade
DATA: 14/08/1995

MOTIVO DO PEDIDO:
Não tenho conseguido dormir direito há meses. Tenho tido dores de cabeça fortes, esquecimentos durante o trabalho e dificuldade pra lembrar que dia é.

ASSINATURA DO FUNCIONÁRIO: ___________________________
VISTO DA GERÊNCIA: ___________________________
```
- **Pensamento pós-leitura:** `"Preenchi isso em agosto e larguei aí. Se eu entregar pro Tavares, tô na rua."`
- **Pensamento na releitura:** `"Nunca tive coragem de assinar."`
- **Origem:** `Canal420GreyboxBuilder.cs`

---

### Documento 8 — Recorte do Jornal da Baixada (`arquivo_metalico_opc`)
- **Dia / Local:** Casa Base (acessível no Dia 3 com `chave_arquivo_pessoal`) — Gaveta Alta do Arquivo Metálico no Escritório (`Recorte_Jornal_1994`)
- **Estilo / Carimbo:** `NewspaperClipping` → **`RECORTE DE JORNAL`**
- **Título:** `JORNAL DA BAIXADA — Domingo, 11 de setembro de 1994`
- **Texto integral:**
```text
MÃE E DUAS FILHAS SÃO ENCONTRADAS MORTAS NA RUA DAS ACÁCIAS

A Polícia Civil investiga a morte de M. N., de 32 anos, e das duas
filhas, de 6 e 8 anos, na noite de sábado (10), em uma casa no fim
da Rua das Acácias.

Vizinhos chamaram a PM por volta das 22h30 depois de ouvirem gritos
e barulho vindo de dentro. Quando os policiais arrombaram a porta do
quarto dos fundos, que estava trancada por fora, as três já estavam
mortas. A polícia ainda não informou a causa.

O marido, contador de 34 anos, estava sentado na sala com a TV alta.
Ele foi levado na madrugada para...

//////////////////////////////
```
- **Pensamento pós-leitura:** `"O final foi arrancado."`
- **Pensamento na releitura:** `"..."`
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
| `M_Print_Calendario_Set1994` (`Tex_Calendario_Set1994`) | Parede do Escritório (folha inferior revelada no Dia 3) | `SETEMBRO / 1994` / `D S T Q Q S S` / dias `01` a `30` *(01–09 riscados com X, 10 em diante em branco)* / rodapé `SANTOS - SP  1994` |

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
| `M_Print_Lombada_VHS_2` (`Tex_Lombada_VHS_2`) | Fita VHS no Rack da TV | `VHS` / `NAO APAGAR / 94` |
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
| `M_Print_Agenda_Margaret` | Criado-mudo do Quarto (Dia 3) | `SAB  10/SET` / `- MENINAS NA DONA LUCIA 10H` / `- TERNO CINZA LAVANDERIA` / `- FARMACIA: XAROPE` / `- LIGAR P/ MAE` / `CONVERSAR COM O CIAN HOJE.` |
| `M_Print_Bilhete_Margaret` | Escrivaninha do Escritório (Dia 3) | `CIAN,` / *(linhas cursivas simuladas)* / `MARGARET` |
| `M_Print_Lista_Geladeira` | Porta da Geladeira | `LISTA DA SEMANA` / `- LEITE` / `- MACA` / `- SABONETE INF.` / `- PAO DE FORMA` / `- CAFE` |
| `M_Print_Aviso_Conta` | Porta da Geladeira | `AVISO DE DEBITO` / `CONTA LUZ / AGUA` / carimbo `VENCIDA` |
| `M_Print_Jornal_Baixada_1994` | Gaveta Alta do Arquivo Metálico (Escritório) | `JORNAL DA BAIXADA` / `DOM, 11 DE SETEMBRO DE 1994` / `N.614` / `TRAGEDIA NA RUA DAS ACACIAS` / `POLICIA ISOLOU CASA NA NOITE DE SABADO (10/09)` / legenda `FACHADA ISOLADA - R. ACACIAS` / subtítulos `PORTA TRANCADA POR FORA`, `LEVADO NA MADRUGADA PARA...`, `CAUSA NAO INFORMADA` |
| `M_Print_Envelope_Carta` | Aparador, Mesa da Copa, Rack da TV, Capacho | Selo `95` / `CIAN NABS` / `RUA DAS ACACIAS, 420` / carimbo `VENCIDO` |
| `M_Print_Desenho_Familia` | Parede / mesa do Quarto das Filhas | `MINHA FAMILIA` *(o segundo desenho, `Tex_Desenho_Casa`, é apenas ilustração sem texto)* |

### 5.6 Interações e Documentos Definidos em `Canal420PropDetailBuilders.cs`
Além de construir as malhas detalhadas que recebem os rótulos acima, `Canal420PropDetailBuilders.cs` define diretamente os textos interativos do **Arquivo Metálico do Escritório** (`BuildOfficeFileCabinet`):
- **Gaveta do Meio (`arquivo_gaveta_media` / `arquivo_gaveta_media_opc`):**
  - Prompts: `Abrir a gaveta do meio do arquivo` / `Fechar a gaveta do meio` / `Folhear as pastas`
  - Falas: `"Papéis de 92 e 93. Tá tudo em ordem aqui."` | *Rep:* `"Só faltam as coisas de 94."`
- **Gaveta Alta Trancada (`arquivo_gaveta_alta`):**
  - Prompts: `Tentar abrir a gaveta alta do arquivo` / `Destrancar e abrir a gaveta alta` / `Abrir a gaveta alta do arquivo` / `Fechar a gaveta alta do arquivo`
  - Falas: `"Não abre. A fechadura tá toda riscada."` (trancada) / `"A chave do bolso dela serviu."` (destrancando)
- **Recorte de Jornal 1994 (`Recorte_Jornal_1994` / `arquivo_metalico_opc`):**
  - Prompt: `Ler o recorte de jornal guardado`
  - Documento completo listado em [Documento 8 — Recorte do Jornal da Baixada](#documento-8--recorte-do-jornal-da-baixada-arquivo_metalico_opc).

### 5.7 Textos na Tela da TV CRT — OSD e Tickers (`ProceduralCRTFeed.cs`)
| Modo / Canal | Carimbo OSD Superior | Textos e Tickers na Tela |
|---|---|---|
| **Canal 04 (`BroadcastChannel04`)** | `CH 04` | `AO VIVO` / `JORNAL` / `NOITE` / ticker: `BAIXADA - CORTES NO SETOR ADMINISTRATIVO   SANTOS 19C` |
| **Canal 09 (`BroadcastChannel09`)** | `CH 09` | `BAIXADA 19C  NEBLINA NA SERRA` |
| **Canal 17 (`BroadcastChannel17`)** | `CH 17` | `SO HOJE` / ticker: `LIGUE 0800-1995   ULTIMAS UNIDADES` / tela de relógio: `HORA CERTA` |
| **Plantão 1994 / Bleed (`Canal420Ghost` & Plantão Dia 2)** | `-- --` *(o número `420` nunca aparece no OSD)* | `PLANTAO` / `10/09/94  23:42` / `R. ACACIAS` |
