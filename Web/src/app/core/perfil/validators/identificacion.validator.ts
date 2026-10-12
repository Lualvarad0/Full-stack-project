/** Valida la cédula ecuatoriana (módulo 10), igual que el backend, para avisar mientras se escribe. */
export function cedulaValida(cedula: string): boolean {
  if (!/^\d{10}$/.test(cedula)) return false;
  const provincia = Number(cedula.slice(0, 2));
  if (!((provincia >= 1 && provincia <= 24) || provincia === 30) || Number(cedula[2]) >= 6)
    return false;
  let suma = 0;
  for (let i = 0; i < 9; i++) {
    const producto = Number(cedula[i]) * (i % 2 === 0 ? 2 : 1);
    suma += producto > 9 ? producto - 9 : producto;
  }
  return (10 - (suma % 10)) % 10 === Number(cedula[9]);
}

export function rucValido(ruc: string): boolean {
  if (!/^\d{13}$/.test(ruc)) return false;
  const provincia = Number(ruc.slice(0, 2));
  if (!((provincia >= 1 && provincia <= 24) || provincia === 30)) return false;
  const tercero = Number(ruc[2]);
  if (tercero < 6) return cedulaValida(ruc.slice(0, 10)) && ruc.slice(10) !== '000';
  if (tercero === 6) return ruc.slice(9) !== '0000';
  if (tercero === 9) return ruc.slice(10) !== '000';
  return false;
}
