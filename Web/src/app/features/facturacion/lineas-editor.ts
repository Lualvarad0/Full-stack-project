import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucidePlus, LucideTrash2 } from '@lucide/angular';
import { TARIFAS_IVA, LineaEditable, ServicioFacturable } from '@core/facturacion';
import { DineroPipe } from '@shared/pipes';
import { nuevaLinea, subtotalLinea } from './calculos-factura';
@Component({
  selector: 'app-lineas-factura',
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DineroPipe, LucidePlus, LucideTrash2],
  templateUrl: './lineas-editor.html',
})
export class LineasFacturaEditor {
  readonly lineas = model.required<LineaEditable[]>();
  readonly servicios = input<ServicioFacturable[]>([]);
  protected readonly tarifas = TARIFAS_IVA;
  protected readonly subtotalLinea = subtotalLinea;
  protected agregar(): void {
    this.lineas.update((l) => [...l, this.crearLinea()]);
  }

  /** Agrega un servicio guardado; si la única línea está vacía, la reemplaza. */
  protected agregarServicio(id: string): void {
    const s = this.servicios().find((x) => x.id === Number(id));
    if (!s) return;
    const linea: LineaEditable = {
      ...this.crearLinea(),
      codigo: s.codigo ?? '',
      descripcion: s.descripcion,
      precioUnitario: s.precioUnitario,
      tarifa: s.tarifa,
    };
    this.lineas.update((l) =>
      l.length === 1 && !l[0].descripcion.trim() && !l[0].precioUnitario ? [linea] : [...l, linea],
    );
  }

  protected quitar(clave: number): void {
    this.lineas.update((l) => l.filter((x) => x.clave !== clave));
  }

  protected editar(clave: number, cambios: Partial<LineaEditable>): void {
    this.lineas.update((l) => l.map((x) => (x.clave === clave ? { ...x, ...cambios } : x)));
  }

  protected numero(texto: string): number {
    const n = Number(texto);
    return Number.isFinite(n) && n >= 0 ? n : 0;
  }

  private crearLinea(): LineaEditable {
    return nuevaLinea(Math.max(0, ...this.lineas().map((l) => l.clave)) + 1);
  }
}
