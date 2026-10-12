import { AbstractControl, ValidationErrors } from '@angular/forms';
/** 'yyyy-MM-dd' de hoy menos `anios` años (fecha local). */
export function haceAnios(anios: number): string {
  const d = new Date();
  d.setFullYear(d.getFullYear() - anios);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

export function mayorDeEdad(control: AbstractControl<string>): ValidationErrors | null {
  const valor = control.value;
  return !valor || (valor <= haceAnios(18) && valor >= '1900-01-01') ? null : { mayorDeEdad: true };
}
