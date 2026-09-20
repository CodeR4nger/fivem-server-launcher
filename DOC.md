Estoy desarrollando FiveMServerLauncher, un launcher de Windows para FiveM escrito en C#/.NET 10 con WPF, utilizando una arquitectura desacoplada y TDD con xUnit.

Quiero que continúes el desarrollo desde el estado actual del proyecto, respetando las decisiones de diseño que ya están tomadas. No quiero que reconstruyas ni cambies decisiones ya establecidas salvo que exista una razón técnica concreta y la discutamos primero.

# Objetivo del proyecto
El objetivo es crear un launcher sencillo y rápido para Windows que permita:
- Entrar directamente a un servidor de FiveM.
- Abrir FiveM o FiveM Enhanced manualmente.
- Seleccionar el cliente preferido.
- Preparar automáticamente los requisitos específicos de un servidor.
- Detectar y lanzar aplicaciones externas cuando sean necesarias.
- Obtener información del servidor desde CFX.
- Aplicar las opciones de ejecución que el servidor requiera.
- Tener un modo de desarrollo con opciones adicionales.
- Actualizarse automáticamente en el futuro.
- Mantener la lógica completamente separada de la UI.
- No requerir permisos de administrador.
- La filosofía de UX es: si todo está preparado, no molestar al usuario. Si algo requiere intervención o espera, mostrar una pantalla de carga/progreso explicativa.

# Arquitectura actual
El proyecto está organizado alrededor de responsabilidades independientes.

La configuración ya tiene una separación entre:
```
Configuration
├── LauncherSettings
├── ConfigurationRepository
├── ISettingsStorage
├── InMemorySettingsStorage
└── FileSettingsStorage
```
``InMemorySettingsStorage`` existe en el proyecto de tests para evitar depender del filesystem durante las pruebas.

``FileSettingsStorage`` es la implementación real y persiste mediante JSON.

La persistencia utiliza ``System.Text.Json`` y ``JsonStringEnumConverter``.

La suite de tests utiliza ``xUnit`` y actualmente está completamente verde.

El último estado conocido es:
```shell
Resumen de pruebas: total: 15; con errores: 0; correcto: 15; omitido: 0; duración: 1,2 s
Compilación realizado correctamente en 3,4s
```
Por lo tanto, no rompas los tests existentes y continúa aplicando TDD.

## LauncherSettings actual
Después de revisar el diseño inicial, LauncherSettings fue simplificado.
Actualmente debe representar únicamente preferencias globales del launcher:
```
public class LauncherSettings
{
    public GameClient PreferredClient { get; set; } = GameClient.FiveM;
    public bool AutoLaunch { get; set; } = false;
}
```
Los enums actuales son:
```
public enum GameClient
{
    FiveM,
    FiveMEnhanced
}
```
La razón para eliminar Platform es que FiveM/Rockstar gestionan la plataforma del juego y no queremos que el launcher dependa de Steam/Epic para autenticar o arrancar FiveM.

La razón para eliminar ServerPort es que el puerto pertenece al servidor/perfil y no a la configuración global.

### Rockstar / Steam / Epic / Discord
Una decisión importante del diseño:

**RSC no será gestionado por el launcher.**

FiveM necesita Rockstar Social Club para iniciar sesión y FiveM ya se encarga de ello.

Steam y Discord son diferentes:
- No son necesariamente necesarios para arrancar FiveM.
- Algunos servidores utilizan scripts propios que requieren Steam o Discord.
- Si esos requisitos no están preparados, el usuario puede entrar a FiveM y ser rechazado posteriormente.
- Queremos detectar/preparar esos requisitos antes de lanzar/conectar FiveM.
Por eso Steam y Discord serán requisitos configurables por ``ServerProfile``.

Tampoco asumir que basta con que el proceso exista para considerar que el servicio está completamente preparado: inicialmente podemos comprobar procesos, pero la arquitectura debe permitir posteriormente implementar estados más precisos como "iniciado", "listo" o "autenticado".

## ServerProfile
Queremos separar completamente la configuración global del launcher de la configuración específica de cada servidor.

El concepto actual es:
```
ServerProfile
├── Name
├── Address
├── Requirements
└── FiveMLaunchOptions
```
### Name
Nombre amigable del servidor.

### Address
Debe ser flexible.

Queremos poder trabajar con:
```
CFX ID
URL de CFX
IP:puerto
dominio:puerto
```
Ejemplos conceptuales:
```
8e8xxv
cfx.re/join/xxxxx
149.56.120.52:30320
play.example.com:30120
```
No queremos acoplar el launcher a un único formato.

Cuando sea posible, una dirección CFX debe poder resolverse mediante la API de CFX y obtener información del servidor.

# API de CFX
Hemos investigado la API utilizada por CFX.

Endpoints relevantes:
```
https://frontend.cfx-services.net/api/servers
https://frontend.cfx-services.net/api/servers/single/
https://frontend.cfx-services.net/api/servers/streamRedir/
https://frontend.cfx-services.net/api/servers/icon/
https://gss.cfx-services.net/v1/public/featured-servers
```
También existen endpoints públicos de estado:
```
https://citizenfx.statuspage.io/api/v2/status.json
https://citizenfx.statuspage.io/api/v2/components.json
https://citizenfx.statuspage.io/api/v2/incidents/unresolved.json
```
Un ``/single/{CFX_ID}`` puede devolver información como:
```
EndPoint
Data {
    hostname
    clients
    sv_maxclients
    server
    vars
    resources
    requestSteamTicket
    connectEndPoints
    ...}
```
Dentro de ``vars`` puede aparecer información muy importante:
```
sv_defaultGameBuild
sv_enforceGameBuild
sv_pureLevel
sv_poolSizesIncrease
sv_enforceSteamAuth
sv_enhancedHostSupport
requestSteamTicket
```
Por ejemplo, un servidor real que investigamos devuelve:
```
sv_defaultGameBuild = 3258
sv_enforceGameBuild = 3258
sv_pureLevel = 1
```
y:
```
sv_poolSizesIncrease = "{...}"
```

No queremos duplicar manualmente en ServerProfile información que el servidor ya publica.

# FiveMLaunchOptions
El concepto ``FiveMLaunchOptions`` debe representar las opciones necesarias para preparar/lanzar FiveM.

Entre las opciones investigadas están:
```
Game Build
Pure Mode
PoolSizesIncrease
```
Además:
``-cl2``

para la segunda instancia.

Y la conexión directa:

``fivem://connect/<server>``

o:

``fivem://connect/<server>?<params>``

Argumentos de FiveM que hemos identificado:
```
-b<build>
-pure_<nivel>
-cl2
fivem://connect/<servidor>
fivem://connect/<servidor>?<params>
```

# CitizenFX.ini
También investigamos cómo FiveM persiste parte de esta configuración.

El archivo está junto al ejecutable de FiveM y puede contener:
```ini
[Game]
IVPath=D:\SteamLibrary\steamapps\common\Grand Theft Auto V
SavedBuildNumber=3258
PoolSizesIncrease={"AnimStore":20480,...}
ReplaceExecutable=0
UpdateChannel=beta
DefaultBuild=3258
```
Esto es importante para el diseño.

No necesariamente debemos pasar todo mediante argumentos de línea de comandos.

Algunas opciones pueden requerir modificar el entorno/configuración de FiveM.

En particular:
```
PoolSizesIncrease
Game Build
```
pueden estar relacionadas con ``CitizenFX.ini``.

Debemos investigar antes de implementar una estrategia definitiva.


# Dev Mode
Queremos un Dev Mode separado de la experiencia normal.

Debe permitir opciones que no deberían formar parte de la configuración normal del usuario.

Entre ellas:

- Segundo cliente FiveM (-cl2)
- Configurar manualmente Game Build
- Configurar manualmente Pure Mode

Una precisión importante:
El ``Game Build`` y ``Pure Mode`` configurables en Dev Mode son para abrir FiveM directamente, no para forzar esos valores al conectarse a un servidor.

Cuando se conecta a un servidor, deben prevalecer las necesidades/configuración del servidor.

# Pool Sizes
Hemos confirmado que algunos servidores publican:

sv_poolSizesIncrease

con un JSON de pools, por ejemplo:
```
{
  "AnimStore": 20480,
  "AttachmentExtension": 430,
  "CMoveObject": 600,
  "CWeaponComponentInfo": 2048,
  "EntityDescPool": 20480
}
```
No queremos tratar este JSON como una configuración arbitraria del launcher.

Debe existir una representación adecuada en el dominio y/o una estrategia específica para aplicarlo a FiveM.

La implementación debe investigarse antes de decidir si se modifica CitizenFX.ini,

# Cliente FiveM / Enhanced
El launcher soporta:
- FiveM
- FiveM Enhanced

La preferencia global es:
- PreferredClient

La UI principal tendrá:
- Entrar al servidor como acción principal.
- Un botón secundario con dropdown para:
- Abrir FiveM
- Abrir FiveM Enhanced
Conceptualmente:
```
┌───────────────────────────────┐
│     ENTRAR AL SERVIDOR        │
└───────────────────────────────┘

┌───────────────────────────────┐
│       ABRIR FIVEM          ▼  │
└───────────────────────────────┘
```
El dropdown permite seleccionar el cliente.

# UX
La filosofía principal es: No molestar si todo está listo.

Si Steam y Discord ya están preparados:
```
JUGAR
 ↓
Steam ✓
Discord ✓
FiveM ✓
 ↓
Conectar
```
sin mostrar ventanas innecesarias.

Si algo necesita intervención:
```
Preparando el juego...

Steam
██████████████░░░░

Iniciando Steam...
```
Después:
```
Steam ✓
Discord ✓
FiveM ✓
Conectando...
```
Queremos una pantalla de carga/splash ligera porque actualmente las operaciones son rápidas, pero en el futuro pueden incluir:
```
actualización del launcher,
consulta CFX,
detección,
preparación de FiveM,
validaciones,
otras operaciones.
Arquitectura deseada
No queremos poner lógica directamente en MainWindow.xaml.cs.
```
El flujo debe parecerse conceptualmente a:
```
UI
 ↓
Application / Manager
 ↓
Domain services
 ↓
Infrastructure
```
Queremos responsabilidades separadas para:
```
GameLauncher
ProcessManager
StartupManager / Checker
PathResolver
Server service
CFX API service
Settings repository
Settings storage
Detection
Updates
```
La arquitectura debe permitir testear la lógica sin lanzar procesos reales ni depender del filesystem real cuando no sea necesario.

# TDD
Estamos desarrollando mediante TDD.

La regla de trabajo es:
```
RED
 ↓
Implementación mínima
 ↓
GREEN
 ↓
Refactor
```
Actualmente los tests de configuración/storage están verdes.

Cuando propongas el siguiente paso:
- primero explica brevemente qué comportamiento vamos a introducir;
- indica qué test debemos crear;
- deja que el test falle;
- luego implementamos lo mínimo;
- ejecutamos dotnet test;
- corregimos hasta GREEN;
- después hacemos refactor si corresponde.
No avances múltiples capas de arquitectura de golpe.

Queremos construir incrementalmente.

# Estado conceptual del proyecto
Actualmente estamos dejando atrás el diseño inicial que trataba Steam/Epic como plataformas globales.

El diseño actualizado es:
```
LauncherSettings
    ↓
preferencias globales

ServerProfile
    ↓
configuración/requisitos del servidor

CFX API
    ↓
información dinámica del servidor

FiveM preparation
    ↓
aplicar lo que necesita el servidor

FiveM
    ↓
conexión
```
El launcher no debe asumir que el servidor necesita Steam, Discord, un Game Build específico, Pure Mode o Pool Sizes.

Debe determinarlo cuando sea posible a partir de la información del servidor.

# Diseño inicial de UI
La pantalla principal inicialmente debe ser minimalista:
```
┌─────────────────────────────────────────┐
│                                         │
│          FiveMServerLauncher             │
│                                         │
│                                         │
│       [ ENTRAR AL SERVIDOR ]            │
│                                         │
│       [ ABRIR FIVEM             ▼ ]     │
│                                         │
│       ☑ Abrir automáticamente           │
│          la próxima vez                  │
│                                         │
│                           ⚙ Ajustes     │
└─────────────────────────────────────────┘
```
**Colores:**
```
Background: #FFFFFF
Primary:    #000000
Accent:     #FF6A00
Text:       #111111
Secondary:  #777777
Error:      #D32F2F
Success:    #2E7D32
```
El diseño debe ser limpio, con mucho espacio vacío, botones grandes y animaciones discretas.

Más adelante puede evolucionar para mostrar:
- banner del servidor,
- estado online,
- jugadores,
- noticias,
- Discord,
- web,
- información CFX,
sin tener que modificar la arquitectura de negocio.

# Actualizaciones
El sistema de actualización del launcher debe estar desacoplado.

Conceptualmente:
```
IUpdateService
    CheckForUpdates()
    DownloadUpdate()
    InstallUpdate()
    RestartLauncher()
```
La actualización del launcher es independiente de:
```
FiveM update
Server update
Game update
```
No mezclar estos conceptos.

El actualizador eventualmente necesitará ser capaz de actualizar/reemplazar el launcher sin que el ejecutable principal intente reemplazarse mientras está en ejecución.

# Restricciones
- Windows.
- .NET 10.
- WPF.
- MVVM.
- No permisos de administrador.
- No acoplar lógica a la UI.
- Mantener tests rápidos.
- No depender de procesos reales en tests unitarios.
- No introducir abstracciones únicamente "por si acaso".
- Mantener las abstracciones que aporten testabilidad o separen responsabilidades reales.
- No volver a introducir Platform.
- ServerPort no pertenece a LauncherSettings.
- RSC no será gestionado por el launcher.
- Steam/Discord son requisitos potenciales del servidor, no requisitos universales de FiveM.
- No asumir que "proceso iniciado" equivale necesariamente a "servicio autenticado/listo".
- No forzar Game Build/Pure Mode del perfil del servidor cuando el usuario simplemente abre FiveM en Dev Mode.
- Priorizar información proporcionada por CFX sobre configuración manual duplicada.
# Próximo objetivo
A partir de este estado, quiero continuar diseñando e implementando el sistema de ServerProfile y su relación con la información de CFX, pero sin saltar directamente a la UI.

La siguiente fase debe centrarse en el dominio y en las pruebas:
```
ServerProfile
    ↓
Requirements
    ↓
FiveMLaunchOptions
    ↓
CFX server information
```
Antes de implementar, analiza qué modelos/abstracciones son realmente necesarios y cuáles serían sobreingeniería.

Mantén el enfoque TDD y avanza de forma incremental.

Si una decisión técnica depende de información actual de FiveM/CFX, investigarla antes de asumirla.