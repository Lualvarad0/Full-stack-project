/**
 * Validación del número de tarjeta en el navegador. El número completo nunca se envía: solo el BIN
 * (6 primeros dígitos, que el servidor verifica con el procesador de pagos) y los 4 últimos.
 */

export type MarcaTarjeta = 'Visa' | 'Mastercard' | 'American Express' | 'Diners Club' | 'Discover';

export function soloDigitos(valor: string): string {
  return valor.replace(/\D/g, '');
}

/** Dígito de control (algoritmo de Luhn): detecta errores de tipeo y números inventados. */
export function luhnValido(numero: string): boolean {
  const digitos = soloDigitos(numero);
  if (digitos.length < 13 || digitos.length > 19) return false;
  let suma = 0;
  for (let i = 0; i < digitos.length; i++) {
    let d = Number(digitos[digitos.length - 1 - i]);
    if (i % 2 === 1) {
      d *= 2;
      if (d > 9) d -= 9;
    }
    suma += d;
  }
  return suma % 10 === 0;
}

/** Marca por los primeros dígitos (rangos públicos de cada red); el banco lo confirma el servidor. */
export function marcaPorNumero(numero: string): MarcaTarjeta | null {
  const d = soloDigitos(numero);
  if (/^4/.test(d)) return 'Visa';
  if (/^(5[1-5]|222[1-9]|22[3-9]\d|2[3-6]\d{2}|27[01]\d|2720)/.test(d)) return 'Mastercard';
  if (/^3[47]/.test(d)) return 'American Express';
  if (/^(30[0-5]|309|36|3[89])/.test(d)) return 'Diners Club';
  if (/^(6011|65|64[4-9])/.test(d)) return 'Discover';
  return null;
}

/** "4111 1111 1111 1111" (American Express: 4-6-5). */
export function formatearNumero(valor: string): string {
  const d = soloDigitos(valor).slice(0, 19);
  const grupos = marcaPorNumero(d) === 'American Express' ? [4, 6, 5] : [4, 4, 4, 4, 3];
  const partes: string[] = [];
  let i = 0;
  for (const largo of grupos) {
    if (i >= d.length) break;
    partes.push(d.slice(i, i + largo));
    i += largo;
  }
  return partes.join(' ');
}

/** Lo único que viaja al servidor. */
export function partesPublicas(numero: string): { bin: string; ultimos4: string } {
  const d = soloDigitos(numero);
  return { bin: d.slice(0, 6), ultimos4: d.slice(-4) };
}
