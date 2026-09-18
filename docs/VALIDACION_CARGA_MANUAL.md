# Carga manual de recetas — validación 18/09/2026

## Representación

Un envío de Cargar anteojo crea una lente física por cada ojo incluido. LEJOS, INTERMEDIA y CERCA se conservan como secciones de esa lente, no como lentes adicionales. Guardar y agregar otro crea otro número de anteojo. Un identificador de formulario evita duplicar el anteojo al reenviar.

Los campos siguen siendo texto para conservar entradas incompletas o ilegibles. Se guarda primero la receta completa y luego se interpreta. La interpretación no impide conservar el pedido. La receta original permanece en OriginalInputJson; las correcciones se guardan separadamente. Las graduaciones efectivamente utilizadas tienen campos propios y se usan al terminar, sin alterar la receta solicitada.

Se reutiliza FinishAsync: prevalidación conjunta, transacción, 0,5 pares por lente física, movimientos individuales, cierre y protección contra reintentos. No se restauró el selector de combinación de stock.

## Archivos

- Models/Domain.cs: grupo de anteojo, secciones y graduación utilizada.
- Data/StockDbContext.cs: relaciones e índices únicos.
- Services/LensEntry.cs: entrada completa de receta.
- Services/PrescriptionService.cs: guardado de anteojos, idempotencia y detección de distancias diferentes.
- Services/OrderService.cs: interpretación y selección utilizada; protección de detalles antiguos ambiguos.
- Pages/Pedidos/AgregarLente.cshtml y .cshtml.cs: formulario existente, guardado sin rechazo por datos faltantes y ambos destinos.
- Pages/Pedidos/CorregirLente.cshtml y .cshtml.cs: corrección de la misma lente sin agregar otra ni perder el original.
- Pages/Pedidos/Detalle.cshtml y .cshtml.cs: receta completa, corrección y excepción de graduación utilizada.
- Migrations/20260918115558_PhysicalLensesAndPrescriptionSections.cs, .Designer.cs y StockDbContextModelSnapshot.cs: migración aditiva, aplicada sin borrar la base.
- tests/StockLentes.Checks/PrescriptionChecks.cs y Program.cs: casos nuevos y auditoría de solo lectura.

No se modificó la hoja de estilos general. Sin commit ni push.

## Resultados

| Prueba | Resultado |
|---|---|
| dotnet build | Sin errores ni advertencias |
| Comprobaciones existentes de inventario y pedidos | Todas aprobadas |
| Receta de tres distancias para OD/OI | Dos lentes y seis secciones; sólo dos bajas al terminar |
| Guardado y reenvío del mismo formulario | Cero movimientos, saldo intacto, sin duplicar anteojo |
| Otro anteojo | Segundo número de par y dos lentes adicionales |
| Una sola lente | Un registro físico y baja de 0,5 |
| Datos desconocidos/ilegibles, incluso otra distancia | Guardados y señalados para revisión |
| Corrección | Original intacto y misma identidad de lente |
| Graduación utilizada distinta | Descuenta la utilizada y conserva la solicitada en el historial |
| Stock compartido | Secuencial 3 → 2,5 → 2 en base temporal |
| Reintento de terminar | Cero nuevas bajas |
| Stock cero, insuficiencia conjunta y fallo en segunda baja | Bloqueo/rollback completo; sin bajas parciales |
| Producto sin control | Registra utilización sin alterar existencias |
| Navegador: crear → receta → guardar y agregar otro → volver | Aprobado con DEMO-RECETA-1809 |
| Navegador: corregir receta y guardar graduación utilizada | Aprobado, sin descontar |

La auditoría de la base local después del recorrido confirmó: cuatro lentes (dos anteojos), doce secciones, estado PENDIENTE; los 21 movimientos previos siguen siendo exactamente los mismos, y todas las cantidades y versiones de stock permanecen idénticas. La entrada original «dato pendiente» permanece en la lente 27 tras corregirla a 2.00. Las pruebas de descuentos se ejecutaron en bases temporales, no sobre el stock local.

## Datos que necesitan revisión

El pedido local 89, Id 8, ya tenía dos detalles OD/par 1. No se fusionaron ni borraron automáticamente: se bloquea su cierre hasta aclarar si corresponden a una sola lente con distintas distancias o a anteojos diferentes. El registro original no fue modificado.

Si una familia que busca por ESF/CIL recibe varias graduaciones por distancia, debe indicarse cuál fue utilizada; no se elige arbitrariamente. No hace falta una decisión adicional para multifocales que se identifican por ADD/BASE conforme a su regla configurada.

La aplicación queda en localhost:5003 con dotnet watch. Se deshabilitó únicamente hot reload en ese proceso porque el compilador de recarga falló; watch conserva el reinicio al cambiar archivos.
