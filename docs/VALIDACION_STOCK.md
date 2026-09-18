# Stock — validación 18/09/2026

Se conservaron el modelo de medias unidades, las fórmulas de identificación por familia y el cierre transaccional de pedidos. No se modificó OrderService en esta etapa ni se importaron datos externos.

## Cambios

- InventoryService: entrada aditiva ENTRADA, con movimiento obligatorio, motivo, cantidad positiva en pasos de 0,5, versión e idempotencia. Ajustar mantiene el saldo final y registra su variación. Nueva combinación mantiene CARGA INICIAL.
- Pages/Stock: acciones separadas para ingreso y ajuste; tablas por producto con sólo las dimensiones de su familia; búsqueda por código/nombre y filtros ESF/CIL que aceptan coma o punto.
- Pages/Movimientos y MovementDisplay: tipos legibles, combinación utilizada, anterior/variación/resultante, referencia al pedido y receta original del snapshot; filtros por producto/orden y tipo.
- StockModuleChecks y Program de comprobaciones: pruebas aisladas del ciclo de inventario con código 100.

El modelo de StockMovement ya contenía los campos necesarios. No se agregó una migración en esta etapa. La estética general y Productos se conservaron.

## Pruebas

- dotnet build: cero errores y advertencias.
- Todas las comprobaciones existentes y nuevas: aprobadas.
- Base temporal con código 100: carga 8, entrada +5 → 13, ajuste -3 → 10, pedido de dos lentes → 9,5 → 9; exactamente dos bajas de 0,5.
- Entrada repetida: sin nueva suma ni movimiento. Versión obsoleta y cantidades cero/negativas/fraccionarias inválidas: bloqueadas.
- Graduación solicitada CIL -0,50 y utilizada -0,25: original conservado; baja sobre utilizada.
- Repetición de terminar: cero nuevas bajas. Insuficiencia conjunta, rollback y producto sin control: siguen aprobados por las pruebas existentes.
- Navegador en localhost:5004 con base separada stock-module-ui-1809.db: alta de producto 100 y combinación ESF -1,00/CIL -0,25 con 8 pares; entrada 5 → 13; ajuste final 12; movimientos verificados +8, +5 y -1 con saldos correctos.
- Filtro CIL con coma y ESF negativo: encuentra la fila exacta; no muestra ADD/BASE para Orgánico Blanco.

Las pruebas no escribieron stock ni movimientos en stocklentes-dev.db. La prueba operativa del código 100 en la base principal sigue pendiente de la siguiente etapa. No hubo commit ni push.
