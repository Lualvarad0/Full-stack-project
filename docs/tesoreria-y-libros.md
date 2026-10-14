# Tesorería y libros

Cada cuenta bancaria, movimiento, pago, cuenta contable y asiento pertenece a un `UsuarioId`. El contador accede únicamente mediante una autorización activa, con permiso de consulta o edición. Los saldos, exportaciones y referencias de documentos conservan ese mismo propietario.

## Cobros y pagos

Registra aplicaciones parciales, medio, referencia y vencimiento. Las notas de crédito/débito modifican el saldo de la factura; las retenciones revisadas y vinculadas reducen el dinero pendiente. Las devoluciones solo se registran cuando hay saldo a devolver. El sistema no efectúa cargos ni transferencias bancarias.

La anulación conserva el historial y exige justificación, período abierto y ausencia de conciliación o asiento activo. Los importes admiten dos decimales. Una operación se serializa por titular para impedir sobrepagos concurrentes.

## Bancos

Cuenta individual con saldo inicial, fecha inicial y número completo cifrado por Data Protection; la interfaz muestra los últimos cuatro caracteres. Importación CSV de movimientos con identificador, fecha, concepto, importe firmado y referencia. El CSV admite comillas; la importación es atómica y reconoce duplicados por cuenta/identificador.

Conciliación de un movimiento con uno o varios pagos, incluso aplicaciones parciales de cada pago. Exige mismo titular, sentido del dinero, cuenta y suma exacta. Desconciliar requiere explicación. Comisiones y movimientos sin factura se pueden revisar con motivo, sin inventar un comprobante de venta.

## Plan y asientos

La plantilla de cuentas se copia a la cuenta de cada usuario. Admite cuentas propias de activo, pasivo, patrimonio, ingreso o gasto; sus códigos no son compartidos. El editor guarda borradores con dos a doscientas líneas y una columna positiva por línea. Contabilizar exige debe igual a haber y cuentas propias activas. Un asiento contabilizado se conserva: para corregirlo se anula con justificación en un período abierto y se crea su reemplazo.

Facturas revisadas y pagos permiten preparar borradores. La propuesta necesita revisión de cuentas e impuestos antes de contabilizar. No identifica inventarios, activos, depreciaciones o costos especiales por el nombre de una factura: se registran mediante cuentas y asientos manuales.

El mayor incluye saldo anterior y movimientos contabilizados. El balance de comprobación acumula hasta la fecha elegida; situación financiera incluye activos, pasivos, patrimonio y resultado acumulado en la ecuación. Ingresos, gastos y resultado del informe corresponden al intervalo elegido. Los borradores y anulados quedan fuera.

El cierre detecta borradores y conciliaciones pendientes. Para titulares configurados como obligados a llevar contabilidad, también exige un asiento contabilizado para cada comprobante del negocio. El cierre bloquea cambios de asientos, pagos, movimientos y clasificaciones; la reapertura guarda su justificación y actor real.

Migraciones: `TesoreriaYConciliacion` y `LibrosContables`. Protege y respalda las claves de Data Protection junto con la base de datos: sin ellas no se recuperan números bancarios cifrados.
