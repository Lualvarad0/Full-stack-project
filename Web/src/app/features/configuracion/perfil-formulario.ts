import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { LucideLoader } from '@lucide/angular';
import {
  REGIMENES,
  RegimenTributario,
  PerfilService,
  calcularTope,
  cedulaValida,
  rucValido,
} from '@core/perfil';
import { AvisosService } from '@core/ui';
import { PeriodoService } from '@core/comprobantes';
import { mensajeDeError } from '@core/http';
import { soloDigitos } from '@core/utils';
import { MensajeError } from '@shared/components';
import { DineroPipe } from '@shared/pipes';
@Component({
  selector: 'app-perfil-formulario',
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MensajeError, ReactiveFormsModule, DineroPipe, LucideLoader],
  templateUrl: './perfil-formulario.html',
})
export class PerfilFormulario {
  protected readonly periodo = inject(PeriodoService);
  protected readonly perfilService = inject(PerfilService);
  private readonly avisos = inject(AvisosService);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly regimenes = REGIMENES;
  protected readonly opcionesCargas = [0, 1, 2, 3, 4, 5];

  protected readonly guardando = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.group({
    ruc: [''],
    cedula: [''],
    razonSocial: [''],
    regimen: ['' as RegimenTributario | ''],
    cargasFamiliares: [0],
    enfermedadCatastrofica: [false],
  });

  private readonly valores = toSignal(this.form.valueChanges, {
    initialValue: this.form.getRawValue(),
  });

  protected readonly rucInvalido = computed(() => {
    const ruc = soloDigitos(this.valores().ruc ?? '');
    return ruc.length > 0 && !rucValido(ruc);
  });
  protected readonly cedulaInvalida = computed(() => {
    const cedula = soloDigitos(this.valores().cedula ?? '');
    return cedula.length > 0 && !cedulaValida(cedula);
  });

  protected readonly tope = computed(() =>
    calcularTope(
      this.periodo.anio(),
      Number(this.valores().cargasFamiliares ?? 0),
      !!this.valores().enfermedadCatastrofica,
    ),
  );

  constructor() {
    // Rellena el formulario cuando llega el perfil (y tras guardarlo).
    effect(() => {
      const p = this.perfilService.perfil();
      if (!p) return;
      this.form.reset({
        ruc: p.ruc ?? '',
        cedula: p.cedula ?? '',
        razonSocial: p.razonSocial ?? '',
        regimen: p.regimen ?? '',
        cargasFamiliares: p.cargasFamiliares,
        enfermedadCatastrofica: p.enfermedadCatastrofica,
      });
    });
  }

  /** Al escribir un RUC de persona natural, propone la cédula (sus 10 primeros dígitos) si falta o es inválida. */
  protected completarCedula(): void {
    const ruc = soloDigitos(this.form.controls.ruc.value);
    const cedula = soloDigitos(this.form.controls.cedula.value);
    if (ruc.length === 13 && Number(ruc[2]) < 6 && (!cedula || !cedulaValida(cedula))) {
      this.form.controls.cedula.setValue(ruc.slice(0, 10));
    }
  }

  protected guardar(): void {
    this.error.set(null);
    if (this.rucInvalido() || this.cedulaInvalida()) {
      this.error.set('Revisa la cédula y el RUC antes de guardar.');
      return;
    }
    const v = this.form.getRawValue();
    const rucAnterior = this.perfilService.perfil()?.ruc ?? null;

    this.guardando.set(true);
    this.perfilService
      .guardar({
        ruc: soloDigitos(v.ruc) || null,
        cedula: soloDigitos(v.cedula) || null,
        razonSocial: v.razonSocial.trim() || null,
        regimen: v.regimen || null,
        cargasFamiliares: Number(v.cargasFamiliares),
        enfermedadCatastrofica: v.enfermedadCatastrofica,
      })
      .subscribe({
        next: (r) => {
          this.guardando.set(false);
          const cambioRuc = r.perfil.ruc !== rucAnterior && r.comprobantesEmitidos >= 0;
          this.avisos.exito(
            cambioRuc
              ? `Perfil guardado. ${r.comprobantesEmitidos} ${r.comprobantesEmitidos === 1 ? 'comprobante quedó' : 'comprobantes quedaron'} como ingreso y ${r.comprobantesRecibidos} como gasto.`
              : 'Perfil guardado.',
          );
        },
        error: (e) => {
          this.guardando.set(false);
          this.error.set(mensajeDeError(e));
        },
      });
  }
}
