import { TARIFAS_IVA, LineaEditable } from '@core/facturacion';
import { redondear } from '@core/utils';
export function subtotalLinea(l: LineaEditable): number {
  return Math.max(0, redondear(redondear(l.cantidad * l.precioUnitario) - (l.descuento || 0)));
}

export function calcularTotales(lineas: LineaEditable[]) {
  const porTarifa = TARIFAS_IVA.map((t) => {
    const base = redondear(
      lineas.filter((l) => l.tarifa === t.valor).reduce((s, l) => s + subtotalLinea(l), 0),
    );
    return {
      etiqueta: t.etiqueta,
      base,
      iva: redondear((base * t.porcentaje) / 100),
      usada: lineas.some((l) => l.tarifa === t.valor),
    };
  }).filter((t) => t.usada);
  const subtotal = redondear(porTarifa.reduce((s, t) => s + t.base, 0));
  const iva = redondear(porTarifa.reduce((s, t) => s + t.iva, 0));
  const descuento = redondear(lineas.reduce((s, l) => s + (l.descuento || 0), 0));
  return { porTarifa, subtotal, descuento, iva, total: redondear(subtotal + iva) };
}

export function nuevaLinea(clave: number): LineaEditable {
  return {
    clave,
    codigo: '',
    descripcion: '',
    cantidad: 1,
    precioUnitario: 0,
    descuento: 0,
    tarifa: 'Iva15',
  };
}
