import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { LucideArrowLeft, LucideLoader, LucidePlus, LucideSend, LucideTrash2, LucideTriangleAlert } from '@lucide/angular';
import { FORMAS_PAGO, TARIFAS_IVA, TIPOS_IDENTIFICACION } from '@core/constants';
import { LineaEditable } from '@core/models';
import { AvisosService, ComprobantesService, FacturacionService } from '@core/services';
import { cedulaValida, mensajeDeError, redondear, rucValido, soloDigitos } from '@core/utils';
import { MensajeError, Segmentado, Titulo } from '@shared/components';
import { DineroPipe } from '@shared/pipes';

/** Formulario de factura: cliente, productos o servicios, forma de pago y totales en vivo. */
@Component({
  selector: 'app-nueva-factura',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MensajeError, RouterLink, Titulo, Segmentado, DineroPipe, LucideArrowLeft, LucidePlus, LucideTrash2, LucideSend, LucideLoader, LucideTriangleAlert],
  template: `
    <a routerLink="/facturacion" class="btn btn-ghost -ml-3">
      <svg lucideArrowLeft class="size-4" aria-hidden="true"></svg> Volver a facturas
    </a>
    <app-titulo class="mt-2 block" linea1="Nueva" linea2="factura" />

    @if (pendientes().length) {
      <div class="aviso-atencion mt-6">
        <svg lucideTriangleAlert class="mt-0.5 size-5 shrink-0 text-warning" aria-hidden="true"></svg>
        <p>
          Para emitir completa: {{ pendientes().join(', ') }}.
          <a routerLink="/configuracion" fragment="facturacion" class="ml-1 font-medium text-accent hover:underline">Ir a Configuración</a>
        </p>
      </div>
    }

    <form class="mt-6 grid gap-6 lg:grid-cols-[1fr_20rem]" (submit)="$event.preventDefault(); emitir()" novalidate>
      <div class="min-w-0 space-y-6">
        <!-- Cliente -->
        <section class="card p-5 sm:p-6" aria-labelledby="titulo-cliente">
          <h2 id="titulo-cliente" class="font-semibold">Cliente</h2>
          <app-segmentado class="mt-4 block" [opciones]="tipos" [valor]="tipo()" (valorChange)="cambiarTipo($event)" etiqueta="Tipo de identificación" />

          @if (tipo() === '07') {
            <p class="mt-4 text-sm text-muted-fg">Se emite a CONSUMIDOR FINAL (9999999999999). El SRI permite hasta $50 por factura.</p>
          } @else {
            <div class="mt-4 grid gap-4 sm:grid-cols-2">
              <div>
                <label class="field-label" for="identificacion">{{ etiquetaIdentificacion() }}</label>
                <input id="identificacion" class="input font-mono" list="clientes-conocidos" autocomplete="off"
                  [attr.inputmode]="tipo() === '06' ? 'text' : 'numeric'" [attr.aria-invalid]="identificacionInvalida()"
                  [value]="identificacion()" (input)="escribirIdentificacion($any($event.target).value)" />
                <datalist id="clientes-conocidos">
                  @for (c of clientes(); track c.identificacion) {
                    <option [value]="c.identificacion">{{ c.razonSocial }}</option>
                  }
                </datalist>
                @if (identificacionInvalida()) {
                  <p class="field-error">{{ tipo() === '04' ? 'El RUC no es válido.' : 'La cédula no es válida.' }}</p>
                }
              </div>
              <div>
                <label class="field-label" for="razon">Nombre o razón social</label>
                <input id="razon" class="input" autocomplete="off" [value]="razonSocial()" (input)="razonSocial.set($any($event.target).value)" />
              </div>
              <div>
                <label class="field-label" for="correo">Correo <span class="font-normal text-muted-fg">(para enviarle la factura)</span></label>
                <input id="correo" type="email" class="input" autocomplete="off" [value]="correo()" (input)="correo.set($any($event.target).value)" />
              </div>
              <div>
                <label class="field-label" for="direccion">Dirección <span class="font-normal text-muted-fg">(opcional)</span></label>
                <input id="direccion" class="input" autocomplete="off" [value]="direccion()" (input)="direccion.set($any($event.target).value)" />
              </div>
            </div>
          }
        </section>

        <!-- Detalle -->
        <section class="card p-5 sm:p-6" aria-labelledby="titulo-detalle">
          <h2 id="titulo-detalle" class="font-semibold">Productos o servicios</h2>
          <div class="mt-4 space-y-4">
            @for (l of lineas(); track l.clave; let i = $index) {
              <fieldset class="rounded-xl border border-border p-4">
                <legend class="sr-only">Ítem {{ i + 1 }}</legend>
                <div class="grid gap-3 sm:grid-cols-6">
                  <div class="sm:col-span-4">
                    <label class="field-label" [attr.for]="'desc-' + l.clave">Descripción</label>
                    <input [id]="'desc-' + l.clave" class="input" [value]="l.descripcion" (input)="editar(l.clave, { descripcion: $any($event.target).value })" />
                  </div>
                  <div class="sm:col-span-2">
                    <label class="field-label" [attr.for]="'cod-' + l.clave">Código <span class="font-normal text-muted-fg">(opcional)</span></label>
                    <input [id]="'cod-' + l.clave" class="input font-mono" maxlength="25" [value]="l.codigo" (input)="editar(l.clave, { codigo: $any($event.target).value })" />
                  </div>
                  <div>
                    <label class="field-label" [attr.for]="'cant-' + l.clave">Cantidad</label>
                    <input [id]="'cant-' + l.clave" type="number" min="0" step="any" class="input" [value]="l.cantidad" (input)="editar(l.clave, { cantidad: numero($any($event.target).value) })" />
                  </div>
                  <div class="sm:col-span-2">
                    <label class="field-label" [attr.for]="'precio-' + l.clave">Precio unitario</label>
                    <input [id]="'precio-' + l.clave" type="number" min="0" step="0.01" class="input" [value]="l.precioUnitario" (input)="editar(l.clave, { precioUnitario: numero($any($event.target).value) })" />
                  </div>
                  <div>
                    <label class="field-label" [attr.for]="'desc-v-' + l.clave">Descuento</label>
                    <input [id]="'desc-v-' + l.clave" type="number" min="0" step="0.01" class="input" [value]="l.descuento" (input)="editar(l.clave, { descuento: numero($any($event.target).value) })" />
                  </div>
                  <div class="sm:col-span-2">
                    <label class="field-label" [attr.for]="'iva-' + l.clave">IVA</label>
                    <select [id]="'iva-' + l.clave" class="input" (change)="editar(l.clave, { tarifa: $any($event.target).value })">
                      @for (t of tarifas; track t.valor) {
                        <option [value]="t.valor" [selected]="t.valor === l.tarifa">{{ t.etiqueta }}</option>
                      }
                    </select>
                  </div>
                </div>
                <div class="mt-3 flex items-center justify-between text-sm">
                  <span class="text-muted-fg">Subtotal <span class="money font-medium text-fg">{{ subtotalLinea(l) | dinero }}</span></span>
                  @if (lineas().length > 1) {
                    <button type="button" class="btn btn-ghost min-h-8 px-2 text-danger" (click)="quitar(l.clave)" [attr.aria-label]="'Quitar ítem ' + (i + 1)">
                      <svg lucideTrash2 class="size-4" aria-hidden="true"></svg> Quitar
                    </button>
                  }
                </div>
              </fieldset>
            }
          </div>
          <div class="mt-4 flex flex-wrap items-center gap-3">
            <button type="button" class="btn btn-secondary" (click)="agregar()">
              <svg lucidePlus class="size-4" aria-hidden="true"></svg> Agregar ítem
            </button>
            @if (servicios().length) {
              <select class="input w-auto min-w-56" aria-label="Agregar desde mis servicios" (change)="agregarServicio($any($event.target).value); $any($event.target).value = ''">
                <option value="">Agregar desde mis servicios…</option>
                @for (s of servicios(); track s.id) { <option [value]="s.id">{{ s.descripcion }} · {{ s.precioUnitario | dinero }}</option> }
              </select>
            } @else {
              <a routerLink="/facturacion/servicios" class="text-sm font-medium text-accent hover:underline">Guarda tus servicios para agregarlos con un clic</a>
            }
          </div>
        </section>
      </div>

      <!-- Resumen -->
      <aside class="card h-fit p-5 lg:sticky lg:top-24" aria-labelledby="titulo-totales">
        <h2 id="titulo-totales" class="font-semibold">Totales</h2>
        <dl class="mt-4 space-y-2 text-sm">
          <div class="flex justify-between"><dt class="text-muted-fg">Subtotal sin impuestos</dt><dd class="money">{{ totales().subtotal | dinero }}</dd></div>
          @if (totales().descuento > 0) {
            <div class="flex justify-between"><dt class="text-muted-fg">Descuentos</dt><dd class="money">−{{ totales().descuento | dinero }}</dd></div>
          }
          @for (t of totales().porTarifa; track t.etiqueta) {
            <div class="flex justify-between"><dt class="text-muted-fg">{{ t.etiqueta }} sobre {{ t.base | dinero }}</dt><dd class="money">{{ t.iva | dinero }}</dd></div>
          }
          <div class="flex justify-between border-t border-border pt-3 text-base font-semibold"><dt>Total</dt><dd class="money">{{ totales().total | dinero }}</dd></div>
        </dl>

        <label class="field-label mt-5" for="pago">Forma de pago</label>
        <select id="pago" class="input" (change)="formaPago.set($any($event.target).value)">
          @for (f of formasPago; track f.valor) {
            <option [value]="f.valor" [selected]="f.valor === formaPago()">{{ f.etiqueta }}</option>
          }
        </select>

        <app-mensaje-error class="mt-4 block" [mensaje]="error()" />

        <button type="submit" class="btn btn-primary mt-5 min-h-11 w-full" [disabled]="emitiendo() || pendientes().length > 0">
          @if (emitiendo()) {
            <svg lucideLoader class="size-4 animate-spin" aria-hidden="true"></svg> Firmando y enviando al SRI…
          } @else {
            <svg lucideSend class="size-4" aria-hidden="true"></svg> Firmar y emitir
          }
        </button>
        @if (emitiendo()) {
          <p class="mt-2 text-center text-xs text-muted-fg" role="status">El SRI puede tardar unos segundos en autorizarla.</p>
        }
      </aside>
    </form>
  `,
})
export class NuevaFactura {
  private readonly facturacion = inject(FacturacionService);
  private readonly avisos = inject(AvisosService);
  private readonly router = inject(Router);

  protected readonly tipos = TIPOS_IDENTIFICACION;
  protected readonly tarifas = TARIFAS_IVA;
  protected readonly formasPago = FORMAS_PAGO;

  protected readonly servicios = toSignal(this.facturacion.servicios().pipe(catchError(() => of([]))), { initialValue: [] });
  protected readonly clientes = toSignal(inject(ComprobantesService).clientes().pipe(catchError(() => of([]))), { initialValue: [] });
  protected readonly pendientes = toSignal(
    this.facturacion.datos().pipe(map((d) => d.pendientes), catchError(() => of<string[]>([]))),
    { initialValue: [] as string[] },
  );

  protected readonly tipo = signal<string>('05');
  protected readonly identificacion = signal('');
  protected readonly razonSocial = signal('');
  protected readonly correo = signal('');
  protected readonly direccion = signal('');
  protected readonly formaPago = signal<string>('20');
  private siguienteClave = 1;
  protected readonly lineas = signal<LineaEditable[]>([this.nuevaLinea()]);
  protected readonly emitiendo = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly etiquetaIdentificacion = computed(() => ({ '04': 'RUC', '05': 'Cédula', '06': 'Pasaporte' })[this.tipo()] ?? 'Identificación');
  protected readonly identificacionInvalida = computed(() => {
    const id = this.identificacion();
    if (!id || this.tipo() === '06') return false;
    return this.tipo() === '04' ? id.length >= 13 && !rucValido(id) : id.length >= 10 && !cedulaValida(id);
  });

  protected readonly totales = computed(() => {
    const lineas = this.lineas();
    const porTarifa = TARIFAS_IVA.map((t) => {
      const base = redondear(lineas.filter((l) => l.tarifa === t.valor).reduce((s, l) => s + this.subtotalLinea(l), 0));
      return { etiqueta: t.etiqueta, base, iva: redondear((base * t.porcentaje) / 100), usada: lineas.some((l) => l.tarifa === t.valor) };
    }).filter((t) => t.usada);
    const subtotal = redondear(porTarifa.reduce((s, t) => s + t.base, 0));
    const iva = redondear(porTarifa.reduce((s, t) => s + t.iva, 0));
    const descuento = redondear(lineas.reduce((s, l) => s + (l.descuento || 0), 0));
    return { porTarifa, subtotal, descuento, iva, total: redondear(subtotal + iva) };
  });

  protected subtotalLinea(l: LineaEditable): number {
    return Math.max(0, redondear(redondear(l.cantidad * l.precioUnitario) - (l.descuento || 0)));
  }

  protected cambiarTipo(tipo: string): void {
    this.tipo.set(tipo);
    this.error.set(null);
  }

  /** Si es un cliente al que ya facturaste, completa su nombre. */
  protected escribirIdentificacion(valor: string): void {
    const id = this.tipo() === '06' ? valor.trim() : soloDigitos(valor);
    this.identificacion.set(id);
    const conocido = this.clientes().find((c) => c.identificacion === id);
    if (conocido && !this.razonSocial()) this.razonSocial.set(conocido.razonSocial);
  }

  protected agregar(): void {
    this.lineas.update((l) => [...l, this.nuevaLinea()]);
  }

  /** Agrega un servicio guardado; si la única línea está vacía, la reemplaza. */
  protected agregarServicio(id: string): void {
    const s = this.servicios().find((x) => x.id === Number(id));
    if (!s) return;
    const linea: LineaEditable = { ...this.nuevaLinea(), codigo: s.codigo ?? '', descripcion: s.descripcion, precioUnitario: s.precioUnitario, tarifa: s.tarifa };
    this.lineas.update((l) => (l.length === 1 && !l[0].descripcion.trim() && !l[0].precioUnitario ? [linea] : [...l, linea]));
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

  protected emitir(): void {
    const problema = this.validar();
    this.error.set(problema);
    if (problema) return;

    const consumidorFinal = this.tipo() === '07';
    this.emitiendo.set(true);
    this.facturacion.emitir({
      tipoIdentificacion: this.tipo(),
      identificacion: consumidorFinal ? '9999999999999' : this.identificacion(),
      razonSocial: consumidorFinal ? 'CONSUMIDOR FINAL' : this.razonSocial().trim(),
      correo: consumidorFinal ? null : this.correo().trim() || null,
      direccion: consumidorFinal ? null : this.direccion().trim() || null,
      formaPago: this.formaPago(),
      detalles: this.lineas().map(({ clave, codigo, ...l }) => ({ ...l, codigo: codigo.trim() || null, descripcion: l.descripcion.trim() })),
    }).subscribe({
      next: (f) => {
        this.emitiendo.set(false);
        if (f.estado === 'Autorizada') this.avisos.exito(`Factura ${f.numero} autorizada por el SRI.`);
        void this.router.navigate(['/facturacion', f.id]);
      },
      error: (e) => {
        this.emitiendo.set(false);
        this.error.set(mensajeDeError(e));
      },
    });
  }

  private validar(): string | null {
    if (this.tipo() !== '07') {
      if (!this.identificacion()) return `Escribe el ${this.etiquetaIdentificacion().toLowerCase()} del cliente.`;
      if (this.identificacionInvalida() || (this.tipo() === '05' && this.identificacion().length !== 10) || (this.tipo() === '04' && this.identificacion().length !== 13))
        return `Revisa el ${this.etiquetaIdentificacion().toLowerCase()} del cliente.`;
      if (this.razonSocial().trim().length < 2) return 'Escribe el nombre o razón social del cliente.';
    }
    if (this.lineas().some((l) => !l.descripcion.trim())) return 'Cada ítem necesita una descripción.';
    if (this.lineas().some((l) => l.cantidad <= 0)) return 'La cantidad de cada ítem debe ser mayor que cero.';
    if (this.lineas().some((l) => (l.descuento || 0) > redondear(l.cantidad * l.precioUnitario))) return 'Un descuento es mayor que el valor de su ítem.';
    if (this.totales().total <= 0) return 'El total de la factura debe ser mayor que cero.';
    if (this.tipo() === '07' && this.totales().total > 50) return 'A consumidor final solo puedes facturar hasta $50. Pide la cédula o RUC del cliente.';
    return null;
  }

  private nuevaLinea(): LineaEditable {
    return { clave: this.siguienteClave++, codigo: '', descripcion: '', cantidad: 1, precioUnitario: 0, descuento: 0, tarifa: 'Iva15' };
  }
}
