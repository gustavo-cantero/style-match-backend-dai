# Proyecto: StyleMatch

## Descripción

**StyleMatch** es una aplicación móvil desarrollada en el marco de la materia _Desarrollo de Aplicaciones I_ de la **Universidad Argentina de la Empresa (UADE)**.  
Su propósito es brindar a los usuarios una forma innovadora de gestionar y combinar sus prendas de ropa mediante el uso de la cámara del dispositivo, logrando que el proceso de elegir qué ponerse sea más simple, organizado y creativo.

Con **StyleMatch** podés:

- Capturar fotos de tus prendas para construir tu propio guardarropa digital.
- Generar combinaciones únicas de outfits, potenciando tu creatividad y estilo personal.
- Guardar tus outfits favoritos para un acceso rápido.
- Organizar tus combinaciones dentro de categorías específicas según la ocasión o preferencia.

De esta manera, la aplicación no solo busca resolver el problema cotidiano de “qué me pongo”, sino también ofrecer una experiencia atractiva, accesible y personalizada, pensada para mejorar la vida diaria de los usuarios y acompañarlos en su expresión de estilo.

## Arquitectura

```mermaid
graph TB
    subgraph Client["📱 Cliente Móvil"]
        Mobile["React Native App<br/>(Expo)"]
        SecureStore["Secure Store<br/>(JWT Token)"]
    end

    subgraph API["☁️ Backend API"]
        Controllers["Controllers<br/>(Auth, Favourite, Garment)"]
        Services["Services<br/>(Outfit, ImageResizer)"]
        Auth["Auth Module"]
    end

    subgraph Storage["💾 Persistencia"]
        DB[("SQL Server")]
        Files["File System"]
    end

    subgraph External["🌐 Servicios Externos"]
        OpenAI["OpenAI API"]
        SMTP["SMTP Server"]
        Google["Google Auth"]
    end

    Mobile -->|HTTPS| Controllers
    Mobile --> SecureStore

    Controllers --> Auth
    Controllers --> Services

    Auth --> DB
    Services --> DB
    Services --> Files

    Services --> OpenAI
    Controllers --> SMTP
    Controllers --> Google

    style Client fill:#e1f5ff,stroke:#0277bd
    style API fill:#fff4e1,stroke:#ff6f00
    style Storage fill:#e8f5e9,stroke:#2e7d32
    style External fill:#fce4ec,stroke:#c2185b
```

## DER (Diagrama Entidad-Relación)

![DER](./diagramas/der.png)

## Diagrama de paquetes

```mermaid
graph TB
    %% --- FRONTEND ---
    subgraph Frontend["📱 Frontend - React Native (Expo)"]
        direction TB

        subgraph AppPkg["app/ (Screens)"]
            Screens["Login, Tabs, Create"]
        end

        subgraph HooksPkg["hooks/"]
            Hooks["useLogin, useCamera"]
        end

        subgraph ComponentsPkg["components/"]
            UI["UI & Outfit Components"]
        end

        subgraph ServicesPkg["services/"]
            APIServices["Auth, Garment, Outfit"]
        end

        subgraph SharedPkg["Shared/"]
            Interfaces["interfaces/"]
            Config["config/"]
        end

        %% Dependencias Internas Frontend
        AppPkg --> HooksPkg
        AppPkg --> ComponentsPkg
        ComponentsPkg --> ServicesPkg
        HooksPkg --> ServicesPkg
        ServicesPkg --> SharedPkg
    end

    %% --- BACKEND ---
    subgraph Backend["⚙️ Backend - ASP.NET Core"]
        direction TB

        subgraph ControllersPkg["Controllers/"]
            API_Endpoints["Auth, Garment, Favourite"]
        end

        subgraph BusinessPkg["Business Logic/"]
            ServicesBE["OutfitService"]
            Helpers["ImageResizer, Auth"]
        end

        subgraph ModelsPkg["Models/"]
            DTOs["DTOs & ViewModels"]
        end

        subgraph DataPkg["Data Access/"]
            Entities["Entities (User, Garment)"]
            DAL["DataHelper"]
        end

        %% Dependencias Internas Backend
        ControllersPkg --> BusinessPkg
        ControllersPkg --> ModelsPkg
        BusinessPkg --> DataPkg
        DataPkg --> ModelsPkg
    end

    %% --- INFRAESTRUCTURA (Local) ---
    subgraph Infrastructure["💾 Infraestructura Local"]
        DB[("SQL Server")]
        FS["File System"]
    end

    %% --- EXTERNOS (Cloud/Third Party) ---
    subgraph External["🌐 Servicios Externos"]
        OpenAI["OpenAI API"]
        SMTP["SMTP / Google"]
    end

    %% --- RELACIONES ENTRE CAPAS ---

    %% Frontend -> Backend
    ServicesPkg -.->|HTTPS / JSON| ControllersPkg

    %% Backend -> Infra Local
    DataPkg --> DB
    BusinessPkg --> FS

    %% Backend -> Servicios Externos
    BusinessPkg -.->|API Call| OpenAI
    ControllersPkg -.->|Net| SMTP

    %% Estilos
    style Frontend fill:#e3f2fd,stroke:#1565c0
    style Backend fill:#fff3e0,stroke:#e65100

    %% Estilo Infra (Verde)
    style Infrastructure fill:#e8f5e9,stroke:#2e7d32

    %% Estilo Externos (Rosado/Púrpura)
    style External fill:#fce4ec,stroke:#ad1457

    %% Estilos de nodos específicos (Paquetes blancos)
    classDef pack fill:#ffffff,stroke:#666,stroke-width:1px;
    class AppPkg,HooksPkg,ComponentsPkg,ServicesPkg,SharedPkg pack
    class ControllersPkg,BusinessPkg,ModelsPkg,DataPkg pack
```

## Diagrama de secuencia

```mermaid
sequenceDiagram
    actor User as Usuario
    participant Front as Frontend Mobile
    participant Back as Backend API
    participant DB as Base de Datos / Storage
    participant AI as OpenAI API

    Note over User,AI: ========== FLUJO DE LOGIN ==========

    User->>Front: Ingresa email y password
    Front->>Front: AuthService.loginOrFailWithCredentials()
    Front->>Back: POST /api/auth

    Back->>Back: AuthController.Login(data)
    Back->>Back: AuthService.LoginAsync()
    Back->>DB: UserDB.AuthAsync(email, pass)
    DB-->>Back: Usuario válido

    Back->>Back: AuthService.CreateTokenAsync()
    Back-->>Front: { token: "..." }

    Front->>Front: SecureStore.setItemAsync("authToken")
    Front-->>User: Login exitoso (Redirige a Tabs)

    Note over User,AI: ========== CREACIÓN DE OUTFIT ==========

    User->>Front: Selecciona prendas y Guarda
    Front->>Front: SecureStore.getItemAsync("authToken")

    Front->>Back: POST /api/Favourite<br/>Header: Bearer {token}

    Back->>Back: Middleware: Valida Firma JWT

    Back->>Back: FavouriteController.Add(data)
    Back->>Back: Valida datos y genera ID
    Back->>DB: FavouriteDB.SaveAsync()
    DB-->>Back: res = 2 (cambios detectados)

    alt res == 2 (Generar Nueva Imagen)
        Back->>Back: OutfitService.GenerateOutfitAsync()

        loop Para cada prenda
            Back->>DB: FileSystem.LeeImagen()
            DB-->>Back: Bytes imagen
        end

        Back->>Back: OutfitService.ComposeAtlas (SkiaSharp)

        Back->>AI: POST /v1/images/edits<br/>(Atlas + Prompt)
        AI-->>Back: JSON { b64_json: "..." }

        Back->>Back: Decodifica Base64 -> Bytes

        Back->>DB: FileSystem.GuardaImagenFull()
        Back->>DB: FileSystem.GuardaThumbnail()
    end

    Back-->>Front: Ok({ externalId })
    Front-->>User: Navega a /outfits y muestra resultado
```

## Tecnologías Utilizadas

- **Frontend – React Native:** Se eligió por ser un framework híbrido que permite el desarrollo multiplataforma con una única base de código. La mayoría del equipo posee experiencia previa en **React**, lo que agiliza la curva de aprendizaje y acelera el desarrollo.
- **Backend – .NET:** La elección de **.NET** se fundamenta en la experiencia previa del equipo en el ecosistema Microsoft, lo que facilita la implementación de buenas prácticas y optimiza los tiempos de desarrollo. Además, ofrece un excelente manejo de memoria y rendimiento, características clave para aplicaciones que requieren escalabilidad.
- **Base de Datos – SQL Server:** Se seleccionó **SQL Server** por tratarse de un sistema de base de datos relacional robusto, ampliamente utilizado en entornos académicos y empresariales. Su integración con el ecosistema Microsoft facilita la administración y asegura la confiabilidad e integridad de los datos.
- **Gestión de Proyecto – Jira:** Se utilizó **Jira** por ofrecer un servicio gratuito adecuado al alcance del proyecto. Su interfaz es intuitiva y permite un seguimiento ágil y claro de las tareas.
- **Control de Versiones – GitHub:** Se seleccionó **GitHub** por ser una plataforma ampliamente adoptada en la industria, con integración nativa a múltiples herramientas de CI/CD. Permite un control de versiones confiable, colaboración fluida entre los integrantes del equipo y trazabilidad completa del desarrollo.

## Plan de pruebas:

[Plan de Pruebas - DAI.pdf](./diagramas/Plan%20de%20Pruebas%20-%20DAI.pdf)

## Instalación y Ejecución

### Prerrequisitos

Antes de comenzar, asegurate de tener instalado:

- [Node.js](https://nodejs.org/) (versión recomendada: **LTS**)
- [npm](https://www.npmjs.com/) o [yarn](https://yarnpkg.com/) como gestor de paquetes
- [Expo Go](https://expo.dev/client) en tu dispositivo móvil (disponible en Google Play y App Store) para probar la app de forma rápida

### Pasos de instalación

1. Cloná el repositorio en tu máquina local:
   ```bash
   git clone https://github.com/nicopenaloza/style-match-dai.git
   cd style-match-dai
   ```
2. Instalá las dependencias del proyecto

```bash
  npm install
```

3. Iniciá el servidor de desarrollo

```bash
  npm start
```

4. Iniciá el emulador de Android (debes tener Android Studio instalado).

```bash
    a
```

En el caso de querer probar la aplicación con iOS podrá utilizar

```bash
    i
```

Y en el caso de querer probarla en la web, puede utilizar

```bash
    w
```

### Pasos de creación de APK.

1. Ejecute el siguiente comando en la raíz del proyecot.

```bash
  npx expo prebuild
```

1. Ingrese a /android.

```bash
  cd android
```

3. Genere el APK con

```bash
    ./gradlew assembleRelease
```
