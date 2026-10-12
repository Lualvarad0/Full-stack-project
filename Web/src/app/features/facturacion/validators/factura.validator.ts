import { LineaEditable } from '@core/facturacion';
import { redondear } from '@core/utils';
import { calcularTotales } from '../calculos-factura';
import { etiquetaIdentificacion, identificacionInvalida } from './cliente.validator';
export interface BorradorValidacionFactura {
  tipo: string;
  identificacion: string;
  razonSocial: string;
  lineas: LineaEditable[];
}

/** Validación final previa a la emisión; no hace peticiones ni modifica el borrador. */
export function validarFactura(borrador: BorradorValidacionFactura): string | null {
  const { tipo, identificacion, razonSocial, lineas } = borrador;
  const etiqueta = etiquetaIdentificacion(tipo).toLowerCase();
  if (tipo !== '07') {
    if (!identificacion) return `Escribe el ${etiqueta} del cliente.`;
    if (
      identificacionInvalida(tipo, identificacion) ||
      (tipo === '05' && identificacion.length !== 10) ||
      (tipo === '04' && identificacion.length !== 13)
    )
      return `Revisa el ${etiqueta} del cliente.`;
    if (razonSocial.trim().length < 2) return 'Escribe el nombre o razón social del cliente.';
  }
  if (lineas.some((l) => !l.descripcion.trim())) return 'Cada ítem necesita una descripción.';
  if (lineas.some((l) => l.cantidad <= 0))
    return 'La cantidad de cada ítem debe ser mayor que cero.';
  if (lineas.some((l) => (l.descuento || 0) > redondear(l.cantidad * l.precioUnitario)))
    return 'Un descuento es mayor que el valor de su ítem.';
  const total = calcularTotales(lineas).total;
  if (total <= 0) return 'El total de la factura debe ser mayor que cero.';
  if (tipo === '07' && total > 50)
    return 'A consumidor final solo puedes facturar hasta $50. Pide la cédula o RUC del cliente.';
  return null;
}
