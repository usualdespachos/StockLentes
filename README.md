# StockLentes — etapas 1 y 2

ASP.NET Core Razor Pages / .NET 10 / EF Core 10 / SQLite.

## Ejecutar en desarrollo

```powershell
dotnet tool restore
dotnet restore
dotnet watch run --launch-profile http
```

Abrir http://localhost:5003. En desarrollo se aplican las migraciones y, únicamente si el catálogo está vacío, se crean 3 productos DEMO, 3 combinaciones y una óptica de demostración. No representan datos reales.

La base está en `App_Data/stocklentes-dev.db`, excluida de Git. Se puede configurar `ConnectionStrings:Stock` por variable de entorno o `appsettings.Local.json` (también excluido). No subir bases ni secretos.

## Modelo

- OpticalStores: ópticas.
- Orders: pedido, óptica, paciente, estado y número externo único por óptica.
- OrderLenses: receta solicitada por ojo/par, código y nombre originales.
- Products: catálogo, familia, control de stock y mínimo.
- LensFamilies + StockRules: dimensiones ESF/CIL/ADD/BASE configurables desde Productos → Nueva familia.
- Stock: existencia única por producto y combinación, saldo no negativo y versión de concurrencia.
- Movements: producto utilizado, graduación, referencias a receta/stock, datos de auditoría y copia de lo solicitado.

Graduaciones: enteros en centésimas. Existencias: enteros de medias unidades (1 = una lente = 0,5 pares). Vacío y cero son distintos. No se almacenan números de fecha como stock.

Las cargas y ajustes de inventario guardan existencia y movimiento en una transacción. Cada formulario lleva una clave única contra reenvíos; un formulario con versión obsoleta se rechaza. Un cambio de familia/control con existencias o historial también se rechaza.

Cada baja usa la identidad estable del detalle de pedido (OrderLensId único + IdempotencyKey lente-ID). Cambiar el producto utilizado no permite volver a descontar la misma lente. Confirmación, saldo, movimiento y finalización del pedido se guardan dentro de una transacción. Se verifica la versión de stock vista por el operario antes de descontar.

## Comprobaciones

```powershell
dotnet run --project tests/StockLentes.Checks
```

Usa una base temporal independiente: migración, semilla, exactitud de medias unidades, duplicados, cantidades inválidas, concurrencia y rollback.

## Próxima etapa

Pendiente de aprobación: siguiente etapa, importación de PDFs/stock e integraciones. El circuito manual de pedidos y bajas ya está disponible. No se conectó n8n, Drive ni Google Sheets.

Antes de uso compartido: autenticación/roles, respaldo y despliegue. El servidor actual es exclusivamente de desarrollo local.

Para PostgreSQL/SQL Server: mantener entidades/servicios, registrar el proveedor EF correspondiente y generar migraciones específicas por proveedor; no reutilizar sin revisar las migraciones SQLite. Auditar índices únicos con valores nulos y concurrencia en el proveedor elegido. En producción las migraciones se aplican explícitamente durante despliegue; no se cargan datos DEMO.

## Etapa 2: uso y decisiones

1. Pedidos → Nuevo pedido: óptica, número, paciente y fecha; comienza PENDIENTE.
2. Agregar todas las lentes con ojo, sección y par antes de confirmar la primera. La composición queda cerrada desde la primera baja para conservar la solicitud original.
3. Revisar producto utilizado y, cuando corresponda, base. Cambiar un desplegable recalcula la vista previa; no descuenta.
4. Si no hay coincidencia única, elegir manualmente una combinación del producto. Una base ambigua o configurada como manual requiere selección; vacío no equivale a cero.
5. Confirmar cada lente por separado: 0,5 pares. Un saldo cero bloquea esa lente. Un producto sin control genera un registro válido sin modificar stock.
6. El pedido pasa a TERMINADO únicamente cuando todas las lentes tienen movimiento confirmado.

Se preservan producto/código/receta/base solicitados. El movimiento conserva los utilizados, base sugerida, indicador de selección manual, motivo y una copia de pedido, óptica, paciente y receta. No hay edición retrospectiva ni borrado de pedidos confirmados en esta etapa.

Migración: `20260917124044_OrderRequestedBaseAndDate` agrega fecha del pedido y BASE solicitada; conserva los datos existentes. Sin cambios en CSS, colores o navegación. Los nuevos formularios reutilizan los componentes actuales.

### Pruebas de interfaz realizadas (solo datos DEMO)

- DEMO-E2-AUTO: OD 5 → 4,5; OI 4,5 → 4. Dos movimientos; pendiente hasta OI.
- DEMO-E2-BASE: base solicitada/sugerida 6,25; usada 6,50. Solo base 6,50 baja 2 → 1,5. Base 6,25 conserva 3.
- DEMO-E2-MANUAL: ESF solicitada 12 sin coincidencia; selección manual ESF 1/CIL 0, saldo 4 → 3,5. Solicitud original intacta.
- DEMO-E2-LIBRE: solicitado DEMO-100, utilizado DEMO-LIBRE. Baja registrada, existencias intactas.
- DEMO-E2-CERO: ESF 2/CIL -1 con saldo 0, botón bloqueado, pedido pendiente, sin movimiento.
- Reenvío: segunda pestaña con formulario anterior de DEMO-E2-BASE devuelve “ya estaba confirmada”; sin movimiento ni descuento adicional.

Se preparó el saldo DEMO de 4,5 a 5 mediante ajuste auditado y se agregó únicamente una combinación alternativa DEMO (ADD 2 / BASE 6,50, 2 pares). No se cargaron datos reales.

El ejecutable de comprobaciones también valida rollback, conservación de originales, producto sin control, stock cero y reenvío con producto cambiado en una base temporal independiente. Para auditar los cinco pedidos de interfaz sin escribir:

```powershell
dotnet run --project tests/StockLentes.Checks -p:BuildProjectReferences=false -- --audit-demo App_Data/stocklentes-dev.db
```