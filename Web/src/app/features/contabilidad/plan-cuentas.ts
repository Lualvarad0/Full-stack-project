import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CuentaContable, LibrosService } from '@core/contabilidad/services/libros.service';
import { AvisosService } from '@core/ui';
@Component({
  selector: 'app-plan-cuentas',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: ` <section class="card p-5 space-y-4">
    <h2 class="font-semibold">Tu plan de cuentas</h2>
    @if (editable()) {
      <button class="btn btn-secondary" type="button" [disabled]="ocupado" (click)="base()">
        Añadir cuentas básicas
      </button>
      <form [formGroup]="form" (ngSubmit)="crear()" class="grid gap-3 md:grid-cols-4">
        <label
          >Código<input class="input" formControlName="codigo" placeholder="1.1.06" maxlength="30"
        /></label>
        <label>Nombre<input class="input" formControlName="nombre" maxlength="150" /></label>
        <label
          >Tipo<select class="input" formControlName="tipo">
            @for (tipo of tipos; track tipo) {
              <option [value]="tipo">{{ tipo }}</option>
            }
          </select></label
        >
        <button class="btn btn-primary self-end" [disabled]="form.invalid || ocupado">
          Añadir cuenta
        </button>
      </form>
    }
    <div class="overflow-x-auto">
      <table class="tabla w-full">
        <thead>
          <tr>
            <th>Código</th>
            <th>Nombre</th>
            <th>Tipo</th>
          </tr>
        </thead>
        <tbody>
          @for (c of cuentas(); track c.id) {
            <tr>
              <td>{{ c.codigo }}</td>
              <td>{{ c.nombre }}</td>
              <td>{{ c.tipo }}</td>
            </tr>
          } @empty {
            <tr>
              <td colspan="3">Inicializa tu plan de cuentas para comenzar.</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  </section>`,
})
export class PlanCuentas {
  readonly cuentas = input.required<CuentaContable[]>();
  readonly editable = input(false);
  readonly cambiado = output<void>();
  private readonly api = inject(LibrosService);
  private readonly avisos = inject(AvisosService);
  protected ocupado = false;
  protected readonly tipos = ['Activo', 'Pasivo', 'Patrimonio', 'Ingreso', 'Gasto'];
  protected readonly form = inject(NonNullableFormBuilder).group({
    codigo: [
      '',
      [Validators.required, Validators.pattern(/^[0-9]+(\.[0-9]+)*$/), Validators.maxLength(30)],
    ],
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    tipo: ['Activo'],
  });
  protected base(): void {
    if (this.ocupado || !this.editable()) return;
    this.ocupado = true;
    this.api.inicializar().subscribe({ next: () => this.fin(), error: (e) => this.error(e) });
  }
  protected crear(): void {
    if (this.ocupado || !this.editable() || this.form.invalid) return;
    this.ocupado = true;
    this.api.cuenta(this.form.getRawValue()).subscribe({
      next: () => {
        this.form.reset();
        this.fin();
      },
      error: (e) => this.error(e),
    });
  }
  private fin(): void {
    this.ocupado = false;
    this.cambiado.emit();
  }
  private error(e: unknown): void {
    this.ocupado = false;
    this.avisos.error(e);
  }
}
