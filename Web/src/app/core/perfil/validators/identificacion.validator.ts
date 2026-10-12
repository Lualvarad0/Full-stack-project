import { AbstractControl, ValidationErrors } from '@angular/forms';

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

/** Persona (por ejemplo, una carga familiar): cédula válida, o pasaporte si trae letras. */
export function cedulaOPasaporteValido(id: string): boolean {
  if (/^\d+$/.test(id)) return cedulaValida(id);
  return /^[A-Za-z0-9]{5,20}$/.test(id);
}

/** RUC de persona natural (tercer dígito 0–5): sus 10 primeros dígitos deben ser la cédula. */
export function rucCoincideConCedula(ruc: string, cedula: string): boolean {
  if (ruc.length !== 13 || cedula.length !== 10 || Number(ruc[2]) >= 6) return true;
  return ruc.startsWith(cedula);
}

/** Validador de formularios reactivos para {@link cedulaOPasaporteValido}; vacío lo deja a `required`. */
export function cedulaOPasaporteValidator(control: AbstractControl<string>): ValidationErrors | null {
  const id = (control.value ?? '').trim();
  return !id || cedulaOPasaporteValido(id) ? null : { identificacion: true };
}
