export const redondear = (valor: number) => Math.round((valor + Number.EPSILON) * 100) / 100;

export const MONEDA = new Intl.NumberFormat('es-EC', { style: 'currency', currency: 'USD' });
