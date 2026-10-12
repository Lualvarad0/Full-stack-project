import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  Asiento,
  CuentaContable,
  LineaAsiento,
  LibrosService,
} from '@core/contabilidad/services/libros.service';
import { hoyEcuador } from '@core/contabilidad/services/tesoreria.service';
import { AvisosService } from '@core/ui';
import { DineroPipe } from '@shared/pipes';
@Component({
  selector: 'app-editor-asiento',
  imports: [FormsModule, DineroPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './editor-asiento.html',
})
export class EditorAsiento {
  readonly cuentas = input.required<CuentaContable[]>();
  readonly asiento = input<Asiento | null>(null);
  readonly editable = input(false);
  readonly cambiado = output<void>();
  private readonly api = inject(LibrosService);
  private readonly avisos = inject(AvisosService);
  protected readonly lineas = signal<LineaAsiento[]>([]);
  protected readonly ocupado = signal(false);
  protected fecha = hoyEcuador();
  protected concepto = '';
  protected readonly diferencia = computed(
    () => Math.round(this.lineas().reduce((s, l) => s + l.debe - l.haber, 0) * 100) / 100,
  );
  constructor() {
    effect(() => {
      const a = this.asiento();
      this.fecha = a?.fecha ?? hoyEcuador();
      this.concepto = a?.concepto ?? '';
      this.lineas.set(
        a
          ? a.lineas.map((l) => ({ ...l }))
          : [
              { cuentaId: 0, debe: 0, haber: 0 },
              { cuentaId: 0, debe: 0, haber: 0 },
            ],
      );
    });
  }
  protected campo(i: number, campo: 'cuentaId' | 'debe' | 'haber', valor: number): void {
    this.lineas.update((ls) => ls.map((l, j) => (j === i ? { ...l, [campo]: Number(valor) } : l)));
  }
  protected agregar(): void {
    if (this.lineas().length < 200)
      this.lineas.update((ls) => [...ls, { cuentaId: 0, debe: 0, haber: 0 }]);
  }
  protected quitar(i: number): void {
    if (this.lineas().length > 2) this.lineas.update((ls) => ls.filter((_, j) => i !== j));
  }
  protected guardar(): void {
    if (this.ocupado() || !this.editable()) return;
    if (
      !this.fecha ||
      !this.concepto.trim() ||
      this.lineas().some((l) => !l.cuentaId || l.debe > 0 === l.haber > 0)
    ) {
      this.avisos.error('Completa la fecha, concepto y un importe por línea.');
      return;
    }
    this.ocupado.set(true);
    this.api
      .guardar(
        { fecha: this.fecha, concepto: this.concepto, lineas: this.lineas() },
        this.asiento()?.id,
      )
      .subscribe({
        next: () => {
          this.ocupado.set(false);
          this.cambiado.emit();
          this.avisos.exito('Borrador guardado; revisa antes de contabilizar.');
        },
        error: (e) => {
          this.ocupado.set(false);
          this.avisos.error(e);
        },
      });
  }
}
