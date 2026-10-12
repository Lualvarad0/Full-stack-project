import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const claveCuenta: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const valor = String(control.value ?? '');
  return valor.length >= 8 && new TextEncoder().encode(valor).length <= 72 ? null : { claveCuenta: true };
};
export const clavesIguales: ValidatorFn = (form: AbstractControl): ValidationErrors | null =>
  form.get('password')?.value === form.get('confirmacion')?.value ? null : { clavesIguales: true };
