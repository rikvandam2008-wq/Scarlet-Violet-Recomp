<div align="center">

<img src="docs/icon/icon_1024.png" alt="Ícone do PokeV-Recomp" width="140">

# PokeV-Recomp (SV Recomp)

**Port nativo para PC de *Pokémon Violet*, feito por recompilação estática. Em desenvolvimento.**

[English](README.md) · **Português (Brasil)**

[Relatar um problema](../../issues) ·
[Releases](../../releases)

</div>

---

> [!WARNING]
> **Projeto em desenvolvimento: espere bugs e crashes.**
> O PokeV-Recomp ainda está em fase inicial. Mesmo nos testes feitos por mim e por amigos aparecem falhas gráficas,
> quedas de desempenho e travamentos, e você provavelmente vai encontrar outros. Peço paciência, e que nos ajude
> [relatando bugs e crashes](../../issues) (com o arquivo `logs/play.log`, quando possível): cada relato ajuda a
> melhorar o projeto.
>
> **O desenvolvimento vai andar mais devagar por um tempo**, porque meu foco principal agora é o
> [PokeL-ARecomp](https://github.com/fabioschmidt01/PokeL-ARecomp).

> [!NOTE]
> **Últimas atualizações (v0.0.23 a v0.0.28): grandes melhorias de desempenho e estabilidade, e várias correções de
> bugs.**
> - **v0.0.28, 12 pull requests da comunidade:** crash ao entrar em cidades corrigido, sem triângulos pretos /
>   esticados perto dos NPCs e na tempestade de areia e neblina, NPCs com sobrancelhas de perto, céu sem faixas,
>   animações da interface na velocidade certa a 60 fps, oclusão ambiente e luz indireta funcionando, zona morta dos
>   analógicos (lupa do mapa / viagem rápida), NVIDIA DLAA, ultrawide 32:9 e opções no F1 de quantas pessoas aparecem
>   e até onde os modelos mantêm o detalhe máximo (por [Atterax](https://github.com/Atterax) e
>   [didslima](https://github.com/didslima), #5 a #16).
> - **O cabelo não brilha mais em roxo / ciano** (achado e corrigido por [Atterax](https://github.com/Atterax),
>   [#4](../../pull/4)). Ainda faltam algumas salvaguardas nessa correção.
> - **Velocidade normal abaixo de 60 fps**: acabou a câmera lenta a 40-50 fps.
> - **Sem os pontinhos brancos** nas bordas dos triângulos em placas Intel e AMD.
> - **Linux**: pasta de dados própria do PokeV-Recomp, e arquivos de outros jogos são recusados em vez de compilados
>   por cima (por [didslima](https://github.com/didslima), [#2](../../pull/2)).
> - A versão suportada do jogo aparece corretamente como **4.0.0** (relatado por Atterax, [#3](../../issues/3)).

O **PokeV-Recomp** (SV Recomp) transforma o código de Nintendo Switch de *Pokémon Violet* em um programa nativo de
64 bits para Windows (e Linux, quase sem teste). O código de máquina ARM64 do jogo é traduzido antecipadamente para C e compilado para x86-64 no
seu próprio PC. Os serviços do sistema operacional do Switch, o sistema de arquivos, o áudio e os gráficos NVN são
reimplementados sobre o Vulkan.

**Nenhum emulador roda enquanto você joga.** O código do jogo executa de forma nativa no seu processador e na sua placa
de vídeo.

> [!IMPORTANT]
> **Você precisa da sua própria cópia do jogo.** O PokeV-Recomp não inclui código do jogo, ROM, asset, textura, fonte
> nem chave de criptografia, e nunca vai incluir. Você fornece os arquivos do jogo que possui; a recompilação acontece
> localmente na sua máquina.

---

## Estado atual

Este é um port inicial, em desenvolvimento. O jogo inicia, carrega um save e é jogável no mundo aberto, mas não está
terminado e só foi testado em uma máquina (Intel Arc B580, Xeon E5-2650 v4, Windows 10).

**Problemas conhecidos**

- A taxa de quadros é instável no mundo aberto (em torno de 40 fps na máquina de teste, com quedas), e há travadinhas
  na primeira vez que cada shader é compilado.
- Só o *Pokémon Violet* 4.0.0 é reconhecido. O *Pokémon Scarlet* ainda não é suportado.
- **O Linux quase não foi testado.** As releases incluem um AppImage compilado automaticamente a partir do mesmo
  código. Um contribuidor o abriu e conferiu a preparação no Bazzite (RTX 5070 Ti); ninguém relatou uma sessão de
  jogo completa ainda. Relatos são bem-vindos ([docs/linux.md](docs/linux.md) lista o
  que incluir).

Cada correção e o estado da sua validação ficam registrados em
[HISTORICO_CORRECOES_E_DESENVOLVIMENTO.md](HISTORICO_CORRECOES_E_DESENVOLVIMENTO.md).

---

## Recursos

A maior parte vem do runtime em que este projeto se baseia (veja os [Créditos](#créditos-e-agradecimentos)). Estão
disponíveis no menu F1; nem todos foram validados com o *Pokémon Violet* ainda.

### Desempenho
- **Alvo de 30 ou 60 fps** (opção). Veja acima, nos problemas conhecidos, como está hoje.
- **Escala de resolução interna**, com resolução dinâmica opcional.
- **Descompactação de texturas em várias threads**, ajustada ao número de threads do processador.

### Gráficos
- **Upscalers**: bilinear, AMD FSR 1, Intel XeSS; AMD FSR 2 (temporal) e anti-aliasing SMAA.
- **Opções de iluminação**: oclusão de ambiente (XeGTAO), luz indireta, reflexos na água, resolução e nitidez das
  sombras.
- **Saída em HDR** (opção).
- **Pacotes de texturas HD**: a opção *Originais / HD* do menu carrega texturas de substituição de `textures/hd/`.
- **Mods**: `mods/<nome>/romfs` em camadas sobre os arquivos do jogo, cada um ligado ou desligado na aba Mods da tela
  inicial. A tradução para português do Brasil da [PKG Traduções](https://pkgtraducoes.com.br/) é instalada assim (não
  vem incluída: baixe com eles). A aba Mods tem o link para baixar e os créditos.
- **Backup dos saves**: cada save também é copiado para `save/backups/` (os 10 mais novos, no máximo um a cada
  5 minutos).

### Controles
- Controles via SDL3 (Xbox, DualShock 4, DualSense, Switch Pro Controller, Steam Deck).
- Teclado e mouse, com câmera pelo mouse e teclas configuráveis.

---

## Versão suportada

| Jogo | Title ID | Versão | Build ID (`main`) | Estado |
|---|---|---|---|---|
| **Pokémon Violet** | `01008F6008C5E000` | **4.0.0** (update v786432) | `709BFD6611529864` | Suportado |
| **Pokémon Scarlet** | `0100A3D008C5C000` | 4.0.0 | - | Ainda não suportado |

---

## Requisitos

| Componente | Mínimo esperado (não testado) | Testado |
|---|---|---|
| **Sistema operacional** | Windows 10/11 64 bits, ou Linux x86-64 com glibc 2.35+ | Windows 10 |
| **Processador** | x86-64 com AVX2, 4 núcleos | Intel Xeon E5-2650 v4 (12 núcleos / 24 threads) |
| **Placa de vídeo** | Vulkan 1.3, 4 GB de memória de vídeo | Intel Arc B580 (12 GB) |
| **Memória** | 8 GB | 16 GB |
| **Armazenamento** | ~15 GB livres | SSD |

---

## Como jogar

1. Baixe o `PokeV-Recomp-windows-x64.zip` na página de [Releases](../../releases) (Linux:
   `PokeV-Recomp-linux-x86_64.AppImage`, sem teste - dê permissão de execução e abra; os passos abaixo são os mesmos).
2. Extraia em uma pasta com permissão de escrita (por exemplo `C:\Jogos\PokeV-Recomp`).
3. Tenha em mãos a sua própria cópia do jogo, de um destes jeitos:
   - o jogo extraído (pastas `exefs` e `romfs`) na versão **4.0.0** - sem precisar de keys; ou
   - o seu dump: o jogo base (`.xci` ou `.nsp`), a **atualização 4.0.0** (`.nsp`) e o `prod.keys` do seu próprio
     console.
4. Abra o `PokeV-Recomp.exe`.
5. Na tela inicial, escolha a pasta com os arquivos do jogo e clique em **Preparar o jogo**.
   A versão do jogo é conferida antes: só Pokémon Violet 4.0.0 é preparado (Scarlet, outras versões e arquivos de
   outros jogos são recusados, com o motivo na tela). No Linux, os arquivos do jogo, saves e configurações ficam em
   `~/.local/share/pokev-recomp` (veja [docs/linux.md](docs/linux.md)); o `prod.keys` também é encontrado em `~/.switch`.
6. A primeira configuração recompila o código do jogo no seu PC. Leva de 10 a 30 minutos, conforme o processador, e
   acontece uma vez (de novo só quando uma atualização muda o modo como o código é gerado).
7. Clique em **Jogar**.

---

## Controles

O movimento no teclado fica em **W / A / S / D** e a câmera segue o mouse. Todas as teclas e botões podem ser vistos e
alterados no menu F1.

| Tecla | Ação |
|---|---|
| F1 | Menu de configurações |
| F9 | Grava os passes de renderização do próximo quadro em `build/` (diagnóstico) |
| F10 | Painel de desempenho |
| F11 | Alternar tela cheia |
| F12 | Salvar captura de tela em `screenshots/` |

---

## Contribuidores

Obrigado a todos que relataram problemas e enviaram correções: sem eles esses bugs não teriam sido encontrados.

- **[Atterax](https://github.com/Atterax)**: achou e corrigiu o cabelo roxo / ciano - o tradutor de shaders ignorava o
  código de condição dos desvios, e a iluminação seguia o caminho errado no cabelo ([#4](../../pull/4)); avisou que a
  atualização suportada é a 4.0.0 do jogo, não a 3.0.1 ([#3](../../issues/3)); criou o `LARECOMP_CLOCK` para testes
  repetíveis. Na v0.0.28: sobrancelhas / cílios dos NPCs ([#5](../../pull/5)), zona morta dos analógicos
  ([#6](../../pull/6)), céu com faixas ([#7](../../pull/7)), crash ao entrar numa cidade ([#8](../../pull/8)),
  triângulos esticados na tempestade / neblina ([#9](../../pull/9)), triângulos pretos seguindo NPCs
  ([#10](../../pull/10)), animações da interface a 60 fps ([#11](../../pull/11)), NVIDIA DLAA ([#12](../../pull/12)),
  oclusão ambiente e luz indireta ([#13](../../pull/13)), F1 > Pessoas exibidas ([#15](../../pull/15)) e detalhe de
  pessoas / Pokémon ([#16](../../pull/16)).
- **[didslima](https://github.com/didslima)**: testou o AppImage no Bazzite, relatou que ele usava a pasta de dados do
  Legends: Arceus ([#1](../../issues/1)) e corrigiu: pasta de dados própria do PokeV-Recomp, só arquivos do Pokémon
  Violet aceitos, nomes do PokeV-Recomp no AppImage, com testes ([#2](../../pull/2)). Na v0.0.28: suporte a
  ultrawide 32:9 - câmera Hor+ e HUD, menus e arte da tela de título mantidos em 16:9 ([#14](../../pull/14)).

Relatos e pull requests são bem-vindos: veja o [CONTRIBUTING.md](CONTRIBUTING.md).

---

## Créditos e agradecimentos

- **Peiikai**: criador do [LAS Recomp](https://github.com/peiikai/LASRecomp) - o recompilador estático, a arquitetura
  do runtime e o tradutor de shaders Maxwell em que este projeto se baseia.
- **Hexveil404**: criador do [PokeZ-ARecomp](https://github.com/Hexveil404/PokeZ-ARecomp), que trouxe os serviços do
  sistema do Switch e o suporte à engine de Pokémon.
- **fabioschmidt01 & Antigravity**: arquitetura, multi-threading para Intel Xeon, ajustes para a Intel Arc B580,
  integração do Intel XeSS e os ports de Pokémon Violet e Arceus.
- **Projeto Ryujinx**: referência de comportamento para o kernel do Switch e a GPU NVN.
- **[PKG Traduções](https://pkgtraducoes.com.br/)**: o mod de tradução para português do Brasil (distribuído por eles).
- **Bibliotecas open-source**: SDL3, FFmpeg, Dear ImGui, Vulkan Memory Allocator, Shaderc, XeGTAO, LibHac, Intel XeSS SDK.

O código próprio do PokeV-Recomp está sob a [licença MIT](LICENSE); os componentes em `third_party/` mantêm as suas
próprias licenças.

---

## Aviso legal

O *PokeV-Recomp* é um projeto de fãs, de pesquisa e preservação. **Não** é endossado, afiliado nem patrocinado pela
Nintendo, Game Freak, Creatures Inc. ou The Pokémon Company. *Pokémon*, *Pokémon Scarlet* e *Pokémon Violet* são marcas
dos seus respectivos donos. Nenhum código da Nintendo, binário do jogo, textura, modelo ou fonte é distribuído neste
repositório nem nas suas releases.
