# I/O Lockdown

I/O Lockdown é uma ferramenta de segurança de endpoint para Windows, projetada para proteger estações de trabalho contra ataques de periféricos físicos e exfiltração de dados enquanto o sistema está bloqueado.

## Objetivo e Estratégia de Defesa

O software implementa uma política de **Fail-Close** para mitigar vetores de ataque como BadUSB (Rubber Ducky), Keyloggers de hardware e Rogue Network Adapters.

### 1. Bloqueio Lógico (Sessão Bloqueada)
Ao detectar que a sessão foi bloqueada (`Win+L`), o sistema executa:
- **Network Kill-Switch:** Desativa todos os adaptadores de rede (Ethernet/Wi-Fi) via WMI.
- **USB Storage Block:** Desativa o serviço `USBSTOR` no registro para impedir a montagem de pendrives.
- **Whitelist de Hardware:** Captura o estado atual dos dispositivos de entrada confiáveis.

### 2. Monitoramento Anti-Tampering (Hardware)
Enquanto bloqueado, o aplicativo monitora o barramento USB. Se houver qualquer alteração física (conexão de um novo dispositivo ou remoção de um existente):
- **Detecção de Violação:** O sistema identifica a mudança via eventos `WM_DEVICECHANGE`.
- **Evidência Visual:** O sistema captura automaticamente uma foto do intruso usando a webcam disponível.
- **Lockdown Total:** Todos os controladores USB (Hubs e Root Controllers) são desativados via PowerShell (`Disable-PnpDevice`).
- **Persistência de Segurança:** O estado de "Violação Detectada" impede a reativação das interfaces ao desbloquear a tela.

### 3. Smart Lock (Proximidade Bluetooth)
O sistema pode ser configurado para monitorar a presença de um dispositivo Bluetooth pareado (ex: seu celular). Se o dispositivo sair do alcance, o Windows é bloqueado automaticamente.

## Modos de Operação

- **Interface de Auditoria:** Aplicação de bandeja que permite visualizar logs em tempo real.
- **Modo Serviço:** Pode ser executado como um serviço do Windows (`--service`) para proteção em nível de sistema sem necessidade de login de usuário.

## Consequências de uma Violação

Se uma violação ocorrer enquanto o PC estiver bloqueado:
1. As portas USB pararão de responder (incluindo teclado e mouse legítimos).
2. Uma foto da tentativa de intrusão será salva na pasta de Imagens.
3. Ao desbloquear o Windows, os logs mostrarão o horário exato e o motivo da violação.
4. **Recuperação:** É necessário reiniciar fisicamente o computador para que o Windows reinicie os controladores de hardware desativados.

## Requisitos de Sistema

- **SO:** Windows 10/11
- **Privilégios:** Executar como Administrador (necessário para manipulação de hardware PnP e Registro).
- **Framework:** .NET 9.0

## Tecnologias Utilizadas

- **C# / .NET 9.0:** Lógica principal e interface.
- **WMI & PnP PowerShell:** Gestão de estado de dispositivos de baixo nível.
- **Win32 API:** Captura de eventos de hardware e controle de sessão.
- **InTheHand.Net:** Integração Bluetooth para Smart Lock.
- **UWP MediaCapture:** Captura de fotos para evidência de violação.

---
*Aviso: Use com cautela. A desconexão acidental do seu teclado durante o bloqueio resultará no desligamento das portas USB, exigindo reinício do sistema.*
