# Simulador de Trayectoria de Dron - Parcial 2

Este proyecto es la resolución del Segundo Parcial de la materia **Programación III**. Consiste en un simulador por consola que calcula la ruta de vuelo ininterrumpida de un dron militar sobre una grilla dinámica (N x N) utilizando el patrón de salto 2x1 (movimiento del caballo de ajedrez).

## Arquitectura

El proyecto está diseñado bajo una arquitectura de N-Capas (Clean Architecture) para asegurar código limpio, mantenible y escalable:

1. **SimuladorDron.Domain**: Contiene las entidades principales y contratos (interfaces) de persistencia (`ControlMaestro`, `DetalleLog`, `IPersistenciaDron`). No tiene dependencias de otros proyectos.
2. **SimuladorDron.Application**: Aloja la lógica de negocio pura. Su corazón es la clase `MotorVuelo`, encargada de calcular las parcelas alcanzables mediante un BFS y encontrar la ruta final utilizando **Backtracking recursivo** optimizado fuertemente con la **Heurística de Warnsdorff** y el desempate de *Arnd Roth*.
3. **SimuladorDron.Infrastructure**: Encargada del acceso a datos. Implementa la persistencia en PostgreSQL puramente mediante **ADO.NET síncrono** (sin Dapper ni Entity Framework). Emplea transacciones y cumple la regla estricta de no usar bucles `for/foreach`, aplicando también la lógica de **ofuscación de datos**.
4. **SimuladorDron.ConsoleApp**: Capa de presentación y punto de entrada. Maneja la interacción con el usuario por terminal, inicializa las cadenas de conexión desde el `appsettings.json` y orquesta los casos de uso.

## Requisitos Previos

- [.NET SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (para levantar la base de datos PostgreSQL)

## Configuración y Ejecución

### 1. Levantar la Base de Datos

Ejecute el siguiente comando para instanciar un contenedor de PostgreSQL con las credenciales correctas:

```bash
docker run --name dron_postgres -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=admin -e POSTGRES_DB=dron_db -p 5432:5432 -d postgres
```

> **Nota:** La aplicación se encarga automáticamente de inicializar el esquema DDL (`tb_master_control` y `tb_detalle_log`) en su primer uso.

### 2. Ejecutar la Aplicación

Diríjase a la carpeta del proyecto de consola y ejecútelo:

```bash
cd SimuladorDron.ConsoleApp
dotnet run
```

## Funcionamiento

Al iniciar, el programa solicitará:
- **N (Dimensión del terreno):** Tamaño de la cuadrícula (ej. 8 para un terreno de 8x8).
- **X, Y (Coordenadas de Despegue):** Celda de inicio elegida por el operador.

El flujo es el siguiente:
1. Evalúa el terreno y usa un grafo BFS para contar la cantidad de parcelas conectadas (alcanzables).
2. Procesa la ruta aplicando inteligencia artificial (heurísticas en grafos) para intentar tocar todas las parcelas alcanzables exactamente una vez.
3. Despliega el resultado en pantalla donde el valor numérico indica el orden cronológico de la visita.
4. Si la misión es exitosa, se activan los protocolos de encriptación (**ofuscación**) y se guarda en la base de datos transaccionalmente.
5. Inmediatamente, se realiza una prueba de ingeniería inversa: la BD devuelve los últimos 5 pasos, la consola los desofusca en tiempo real y los valida contra los datos originales.

## Consideraciones Matemáticas y Técnicas

- **Casos Imposibles Matemáticos:** Existen dimensiones iniciales (por ej. `7x7` desde `0,1`) en los que, por reglas de paridad y teoría de grafos, es imposible completar el recorrido sin dejar exactamente una parcela libre. La heurística utilizada es lo suficientemente potente para recorrer el enorme árbol exponencial, darse cuenta del fallo, y retornar un fallo elegante en lugar de colapsar la memoria de la terminal.
