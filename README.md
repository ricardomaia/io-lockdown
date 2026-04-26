# I/O Lockdown

I/O Lockdown é uma ferramenta de segurança de endpoint para Windows, projetada para proteger estações de trabalho contra ataques de periféricos físicos e exfiltração de dados enquanto o sistema está bloqueado.

## Objetivo e Estratégia de Defesa

O software implementa uma política de **Fail-Close** para mitigar vetores de ataque como BadUSB (Rubber Ducky), Keyloggers de hardware e Rogue Network Adapters.

### 1. Bloqueio Lógico (Sessão Bloqueada)
Ao detectar que a sessão foi bloqueada (`Win+L`), o sistema executa:
- **Network Kill-Switch:** Desativa todos os adaptadores de rede (Ethernet/Wi-Fi) via WMI.
- **USB Storage Block:** Desativa o serviço `USBSTOR` no registro para impedir a montagem de pendrives.

### 2. Monitoramento Anti-Tampering (Hardware)
Enquanto bloqueado, o aplicativo monitora o barramento USB em busca de mudanças. Se houver qualquer alteração física (conexão de um novo dispositivo ou remoção de um existente):
- **Detecção de Violação:** O sistema identifica instantaneamente a mudança via eventos `WM_DEVICECHANGE`.
- **Lockdown Total:** Todos os controladores USB (Hubs e Root Controllers) são desativados via PowerShell (`Disable-PnpDevice`).
- **Persistência de Segurança:** Uma vez detectada a violação, o software **não reativa** as interfaces ao desbloquear a tela. O estado de "Violação Detectada" bloqueia a restauração automática.

## Consequências de uma Violação

Se uma violação ocorrer enquanto o PC estiver bloqueado:
1. As portas USB pararão de responder (incluindo seu teclado e mouse legítimos).
2. Ao desbloquear o Windows (usando métodos alternativos ou após a violação ter ocorrido), um alerta de erro crítico será exibido.
3. **Recuperação:** Será necessário reiniciar fisicamente o computador para que o Windows reinicie os controladores de hardware.

## Requisitos de Sistema

- **SO:** Windows 10/11
- **Privilégios:** Executar como Administrador (necessário para manipulação de hardware PnP e Registro).
- **Framework:** .NET 6.0

## Tecnologias Utilizadas

- **C# / .NET 6.0:** Lógica principal e interface.
- **WMI & PnP PowerShell:** Gestão de estado de dispositivos de baixo nível.
- **Win32 API (WndProc):** Captura de eventos de hardware em tempo real.

---
*Aviso: Use com cautela. A desconexão acidental do seu teclado durante o bloqueio resultará no desligamento das portas USB, exigindo reinício do sistema.*
