# Auditoria Técnica — I/O Lockdown

> Data: 2026-05-05 | Versão analisada: 1.1.9 | Modelo: Claude Sonnet 4.6

---

## Sumário Executivo

O projeto é tecnicamente sólido para um utilitário de segurança Windows. A arquitetura tem boas fundações (interface `IHardwareController`, separação entre Engine/Service/UI) e cobre bem o modelo de ameaças declarado. Os três problemas mais críticos para resolver são:

1. **Tamanho do build (~300 MB publicado / ~85 MB MSI)** — causado principalmente por testes misturados com código de produção e ausência de trimming/compressão.
2. **Cobertura de testes insuficiente** — 4 testes unitários cobrem apenas o caminho feliz da engine; não há testes de borda, integração ou regressão.
3. **Tratamento de erros silencioso** — blocos `catch { }` sem logging mascaram falhas reais em operações de segurança críticas.

---

## 1. Análise do Tamanho do Build

### Situação atual

| Artefato | Tamanho |
|---|---|
| `publish/io-lockdown.exe` (self-contained) | ~65–70 MB |
| `IOLockdown_Installer.msi` | ~85 MB |
| Código-fonte (`.cs`) | ~35 KB |

O runtime .NET 9.0 self-contained tem ~130 MB descomprimido. Com `PublishSingleFile=true` e `PublishReadyToRun=true`, o binário já está relativamente compacto — mas há três causas evitáveis de inchaço:

### Causa 1: Testes no mesmo projeto de produção

Os arquivos `LockdownEngineTests.cs`, `xunit`, `xunit.runner.visualstudio` e `Microsoft.NET.Test.Sdk` estão no mesmo `.csproj` da aplicação. Isso faz com que **todas as dependências de teste sejam incluídas no binário publicado**.

```xml
<!-- io-lockdown.csproj — estes pacotes não deveriam estar aqui -->
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.5.1" />
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
```

**Solução:** Criar um projeto separado `io-lockdown.Tests/io-lockdown.Tests.csproj` com referência ao projeto principal via `<ProjectReference>`. Redução estimada: **15–25 MB** no executável publicado.

### Causa 2: Dependência `Cake.Powershell` não utilizada

`Cake.Powershell` está listada como dependência mas **não é usada em nenhum arquivo `.cs`**. O PowerShell é invocado diretamente via `Process.Start("powershell.exe", ...)` em `WindowsHardwareController.cs:82`.

```xml
<!-- Remover — não é usada -->
<PackageReference Include="Cake.Powershell" Version="2.0.0" />
```

### Causa 3: ReadyToRun sem trimming

`PublishReadyToRun=true` aumenta o tamanho adicionando código pré-compilado (melhora startup, mas infla o binário). Sem `PublishTrimmed=true`, todo o runtime .NET é incluído, mesmo as partes não usadas.

**Opções de otimização para `build.ps1` e `build.yml`:**

```xml
<!-- Adicionar ao .csproj -->
<PublishTrimmed>true</PublishTrimmed>
<TrimMode>partial</TrimMode>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
```

> **Atenção:** trimming com WinForms requer testes cuidadosos. Usar `TrimMode=partial` como ponto de partida; `full` pode quebrar reflexão usada pelo WMI/COM.

### Estimativa de redução

| Ação | Redução estimada |
|---|---|
| Separar testes em projeto próprio | 15–25 MB |
| Remover `Cake.Powershell` | 2–5 MB |
| Adicionar `EnableCompressionInSingleFile` | 20–30 MB |
| Adicionar `PublishTrimmed=partial` | 30–50 MB |
| **Total potencial** | **~67–110 MB** → alvo de ~60–80 MB no EXE, MSI abaixo de 50 MB |

---

## 2. Segurança

### Boas práticas presentes

| Item | Status |
|---|---|
| UAC `requireAdministrator` no manifest | ✅ |
| Single-instance mutex | ✅ |
| Interface `IHardwareController` (testabilidade) | ✅ |
| Separação Engine/Service/UI/Hardware | ✅ |
| WMI com `WITHIN 2` (polling curto) | ✅ |
| Serviço com conta `LocalSystem` | ✅ (necessário; justificado) |
| `.gitignore` excluindo `lockdown.log` | ✅ |

### Problemas encontrados

#### S1 — Blocos `catch {}` silenciosos (Alta severidade)

Múltiplos pontos críticos de segurança engolim exceções sem registro:

```csharp
// WindowsHardwareController.cs:46 — falha ao capturar foto passa em silêncio
public async Task CapturePhoto() {
    try { ... } catch { }  // ← nenhuma evidência de que falhou
}

// WindowsHardwareController.cs:74 — desabilitar rede pode falhar silenciosamente
try { item.InvokeMethod(methodName, null); } catch { }

// LockdownEngine.cs:47 — inicialização inteira pode falhar sem aviso
try { ... } catch { }

// WindowsHardwareController.cs:87 — log nunca chega a lugar algum
catch { }
```

**Risco:** Uma violação pode ocorrer, `TriggerViolation` ser chamado, a câmera falhar, a rede não ser desabilitada — e o operador não saberá. O sistema aparece como funcional enquanto está comprometido.

**Correção mínima:** Substituir `catch { }` por `catch (Exception ex) { Log($"ERRO: {ex.Message}"); }` em todas as operações de segurança críticas. Idealmente, propagar exceções em `TriggerViolation` e `SetNetworkState`.

#### S2 — Injeção via PowerShell (Baixa severidade, mitigada)

```csharp
// WindowsHardwareController.cs:81
string script = $"Get-PnpDevice -Class 'USB' | {action} -Confirm:$false";
```

`action` é controlado internamente (hardcoded como `"Enable-PnpDevice"` ou `"Disable-PnpDevice"`), então não há vetor de injeção real hoje. Porém, se a lógica evoluir para aceitar input externo, isto se torna injeção de comando.

**Mitigação já existente:** valores são literais internos. **Ação recomendada:** adicionar comentário explicitando o invariante, ou usar array de argumentos com `ArgumentList` no lugar de string interpolada.

#### S3 — Loop Bluetooth sem cancelamento (Média severidade)

```csharp
// LockdownEngine.cs:185
Task.Run(async () => {
    while (true) {  // ← sem CancellationToken
        ...
        await Task.Delay(15000);
    }
});
```

A `Task` não é armazenada nem pode ser cancelada. Se o app fechar (ou `LockdownEngine` for destruído), a task continua rodando até o processo terminar — podendo interferir com estado de hardware. O `BluetoothClient` criado dentro da task também nunca é descartado (`IDisposable`).

**Correção:**
```csharp
private CancellationTokenSource? _bluetoothCts;

public void StartBluetoothMonitor(string targetAddress) {
    _bluetoothCts?.Cancel();
    _bluetoothCts = new CancellationTokenSource();
    var token = _bluetoothCts.Token;
    Task.Run(async () => {
        using var client = new BluetoothClient();
        while (!token.IsCancellationRequested) {
            ...
            await Task.Delay(15000, token);
        }
    }, token);
}
```

#### S4 — Permissões do arquivo de log (Média severidade)

```csharp
// LockdownEngine.cs:19
private string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lockdown.log");
```

O log é criado em `C:\Program Files\IOLockdown\lockdown.log` com permissões herdadas do diretório. Embora `Program Files` seja restrito a escrita para usuários não-admin, **a leitura é permitida a todos os usuários por padrão**. Logs de violação podem conter IDs de dispositivos, timestamps de atividade do usuário, etc.

**Recomendação:** Usar `%PROGRAMDATA%\IOLockdown\logs\` com ACL restrita a Administrators, ou registrar eventos no **Windows Event Log** (que tem controle de acesso granular).

#### S5 — Versão inconsistente entre csproj e Package.wxs

```xml
<!-- io-lockdown.csproj -->
<Version>1.1.9</Version>

<!-- Package.wxs -->
<Package ... Version="1.1.8" ...>
```

Não é um risco de segurança, mas pode causar problemas de upgrade silencioso via MSI se o `UpgradeCode` identificar versões incorretamente.

#### S6 — `_staticHardware` como campo estático

```csharp
// LockdownEngine.cs:24
private static IHardwareController _staticHardware = new WindowsHardwareController();
```

Esta instância estática é criada independentemente da injeção de dependência, quebra a testabilidade do método estático `LockWorkStation()` e não pode ser substituída por mock. Representa uma inconsistência arquitetural.

---

## 3. Qualidade do Código

### Problemas de design

| # | Local | Problema |
|---|---|---|
| D1 | `Form1.cs` | Classe com responsabilidades mistas: UI, gerenciamento de sessão, tratamento de mensagens Win32 e delegação ao engine. Candidata a extração de `SessionEventHandler`. |
| D2 | `LockdownEngine.cs:47` | Construtor faz trabalho pesado (`ResetSystemToSafeState`, `CaptureHardwareWhitelist`, `StartHardwareMonitors`) e engole erros. Falhas na inicialização são invisíveis. |
| D3 | `WindowsHardwareController.cs:82–86` | `WaitForExit()` sem timeout bloqueia a thread indefinidamente se o PowerShell travar. Usar `WaitForExit(timeoutMs)` com fallback. |
| D4 | `LockdownEngine.cs:24` | Campo `_staticHardware` estático quebra DI e testabilidade do método estático `LockWorkStation()`. |
| D5 | `Package.wxs:6` | Versão desatualizada (`1.1.8` vs `1.1.9` no csproj). Deve ser lida dinamicamente ou sincronizada no CI. |

### Números mágicos sem nome

```csharp
await Task.Delay(15000);    // 15 segundos — por quê 15?
if (failureCount >= 3) {    // 3 falhas — limiar configurável?
key.SetValue("Start", enable ? 3 : 4, ...);  // 3=SERVICE_DEMAND_START, 4=SERVICE_DISABLED
```

Extrair constantes nomeadas:
```csharp
private const int BluetoothPollIntervalMs = 15_000;
private const int BluetoothFailureThreshold = 3;
private const int UsbStorEnabledValue = 3;   // SERVICE_DEMAND_START
private const int UsbStorDisabledValue = 4;  // SERVICE_DISABLED
```

---

## 4. Análise dos Testes

### Estado atual

```
LockdownEngineTests.cs — 4 testes (xUnit)
MockHardwareController — implementação mock completa de IHardwareController
```

| Teste | O que verifica | Status |
|---|---|---|
| `ResetSystemToSafeState_ShouldEnableAllHardware` | Reset ativa USB/rede, limpa flag | ✅ |
| `TriggerViolation_ShouldDisableHardwareAndSetFlag` | Violação desativa hardware, soa alarme, tira foto | ✅ |
| `IsDeviceAuthorized_ShouldReturnTrueForKnownDevices` | Whitelist autoriza/rejeita por ID | ✅ |
| `SystemUnlock_ShouldRestoreHardware_WhenNoViolation` | Unlock sem violação restaura hardware | ⚠️ |

### Problemas nos testes existentes

**T1 — Teste de unlock não usa a API da engine:**

```csharp
// LockdownEngineTests.cs:85-96
// O teste manipula diretamente o mock, não chama engine.OnSessionUnlock() ou similar.
// Está testando o mock, não o comportamento da engine.
engine.IsLocked = true;
mock.SetNetworkState(false);   // ← manipulação direta do mock
...
engine.IsLocked = false;
if (!engine.ViolationDetected) engine.ResetSystemToSafeState(); // ← lógica inline no teste
```

O teste não verifica que a engine faz a restauração automaticamente no unlock — apenas que `ResetSystemToSafeState` funciona quando chamado manualmente. O fluxo real do `LockdownService.OnSessionChange` não é testado.

**T2 — Nenhum teste de idempotência:**

`TriggerViolation` tem guarda `if (_violationDetected) return;` — não há teste verificando que uma segunda violação não duplica ações.

**T3 — Nenhum teste de whitelist vazia:**

`IsDeviceAuthorized` retorna `true` para strings vazias/null (`if (string.IsNullOrEmpty(pnpDeviceId)) return true;`) — comportamento potencialmente perigoso que não é testado ou documentado.

**T4 — Nenhum teste dos monitores WMI:**

Os watchers de chegada/remoção de USB são o coração do produto. Nenhum teste verifica que eventos de chegada/remoção acionam as ações corretas.

### Testes ausentes (lacunas críticas)

| Teste necessário | Prioridade |
|---|---|
| `TriggerViolation_IsIdempotent` — segunda chamada não duplica ações | Alta |
| `IsDeviceAuthorized_NullOrEmpty_ReturnsBehavior` — documentar e testar o retorno `true` para null | Alta |
| `OnDeviceArrival_WhileLocked_TriggersViolation` | Alta |
| `OnDeviceArrival_UnknownDevice_WhileUnlocked_TriggersLock` | Alta |
| `OnDeviceRemoval_TrustedDevice_WhileUnlocked_PlaysAlarm` | Alta |
| `OnDeviceRemoval_WhileLocked_TriggersViolation` | Alta |
| `BluetoothMonitor_3ConsecutiveFailures_LocksWorkstation` | Média |
| `BluetoothMonitor_RecoveryResetsCounter` | Média |
| `CaptureHardwareWhitelist_PopulatesTrustedIds` | Média |
| `ResetSystemToSafeState_ClearsViolationFlag` | ✅ coberto |

### Estrutura recomendada para testes

```
io-lockdown.Tests/
  io-lockdown.Tests.csproj       ← projeto separado
  Unit/
    LockdownEngineTests.cs        ← mover testes existentes
    BluetoothMonitorTests.cs      ← novos
    DeviceAuthorizationTests.cs   ← novos
  Mocks/
    MockHardwareController.cs     ← mover mock existente
```

---

## 5. CI/CD

### Estado atual

- Build no push para `main` (ignora docs)
- Gera MSI e faz upload como artefato
- Não executa os testes (`dotnet test` ausente)

### Problemas

**CI1 — Testes não são executados no pipeline:**

```yaml
# build.yml — etapa ausente
- name: Run Tests
  run: dotnet test --no-build -c Release
```

Os 4 testes existentes nunca são executados em CI. Uma regressão passaria despercebida.

**CI2 — Versão do MSI não é sincronizada automaticamente:**

`Package.wxs` tem `Version="1.1.8"` enquanto o `.csproj` tem `1.1.9`. O CI não verifica nem sincroniza isso.

**CI3 — Sem release automática:**

O MSI é gerado como artefato de workflow mas não é publicado como GitHub Release. Usuários precisam buscar manualmente nas Actions.

---

## 6. Plano de Ação Priorizado

### Prioridade 1 — Impacto imediato no tamanho e qualidade

- [ ] **Separar testes em projeto próprio** (`io-lockdown.Tests/`) — reduz 15–25 MB do build e melhora organização
- [ ] **Remover `Cake.Powershell`** do `.csproj` — dependência não utilizada
- [ ] **Adicionar `EnableCompressionInSingleFile=true`** ao `.csproj` e scripts de build
- [ ] **Sincronizar versão** entre `io-lockdown.csproj` e `Package.wxs`

### Prioridade 2 — Segurança e confiabilidade

- [ ] **Substituir `catch { }` por logging** em todas as operações de segurança críticas (`TriggerViolation`, `SetNetworkState`, `CapturePhoto`)
- [ ] **Adicionar `CancellationToken` ao loop Bluetooth** e descartar `BluetoothClient`
- [ ] **Adicionar `WaitForExit(timeout)`** na chamada PowerShell de `SetUsbHardwareState`
- [ ] **Avaliar mover log para `%PROGRAMDATA%`** com ACL restrita

### Prioridade 3 — Cobertura de testes

- [ ] **Adicionar teste de idempotência** para `TriggerViolation`
- [ ] **Adicionar testes para eventos de chegada/remoção** de dispositivos
- [ ] **Documentar comportamento `null` em `IsDeviceAuthorized`** e adicionar teste
- [ ] **Adicionar `dotnet test` ao workflow de CI**

### Prioridade 4 — Otimização adicional de tamanho

- [ ] **Testar `PublishTrimmed=true` com `TrimMode=partial`** — potencial de 30–50 MB adicionais; requer testes extensivos com WinForms e WMI
- [ ] **Avaliar remoção de `PublishReadyToRun`** se startup time não for crítico (reduz ~10–15 MB)

---

## Apêndice: Linha de base do tamanho

Reproduzir com as flags atuais vs. otimizadas:

```powershell
# Atual
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:PublishReadyToRun=true -o ./publish

# Com compressão (Prioridade 1)
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:PublishReadyToRun=true `
  -p:EnableCompressionInSingleFile=true -o ./publish-compressed

# Com trimming (Prioridade 4 — testar cuidadosamente)
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:PublishReadyToRun=true `
  -p:EnableCompressionInSingleFile=true `
  -p:PublishTrimmed=true -p:TrimMode=partial -o ./publish-trimmed
```

Medir e comparar tamanhos antes de alterar o pipeline de produção.
