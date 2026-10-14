import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Espacios, tabulaciones o saltos de línea: no se aceptan en una contraseña nueva. */
export const tieneEspacios = (valor: string) => /\s/.test(valor);

export const claveCuenta: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const valor = String(control.value ?? '');
  if (tieneEspacios(valor)) return { claveCuenta: true, claveEspacios: true };
  return valor.length >= 8 && new TextEncoder().encode(valor).length <= 72 ? null : { claveCuenta: true };
};
export const clavesIguales: ValidatorFn = (form: AbstractControl): ValidationErrors | null =>
  form.get('password')?.value === form.get('confirmacion')?.value ? null : { clavesIguales: true };
