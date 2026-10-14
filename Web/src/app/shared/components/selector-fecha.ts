import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  ElementRef,
  forwardRef,
  inject,
  Injector,
  input,
  model,
  signal,
  viewChild,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import {
  LucideCalendarDays,
  LucideChevronDown,
  LucideChevronLeft,
  LucideChevronRight,
} from '@lucide/angular';
import { capitalizar, fechaIso, MESES } from '@core/utils';

const DIAS_SEMANA = ['L', 'M', 'X', 'J', 'V', 'S', 'D'];
const DIAS_SEMANA_LARGOS = ['lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábado', 'domingo'];

/** "2026-10-14" → Date local (sin desfase de zona horaria); null si no es una fecha válida. */
function leerIso(iso: string | null | undefined): Date | null {
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(iso ?? '');
  if (!m) return null;
  const fecha = new Date(+m[1], +m[2] - 1, +m[3]);
  return fecha.getMonth() === +m[2] - 1 ? fecha : null;
}

/**
 * Coloca el panel (en la capa superior, con popover) debajo del botón, o encima si no cabe,
 * sin salirse de la pantalla. Funciona igual dentro de modales con scroll.
 */
function ubicarPanel(boton: HTMLElement, panel: HTMLElement): void {
  const r = boton.getBoundingClientRect();
  const ancho = panel.offsetWidth;
  const alto = panel.offsetHeight;
  const margen = 8;
  const left = Math.min(Math.max(r.left, margen), window.innerWidth - ancho - margen);
  let top = r.bottom + 6;
  if (top + alto > window.innerHeight - margen) {
    const arriba = r.top - alto - 6;
    // Si no cabe ni abajo ni arriba (pantallas bajas), se pega al borde inferior visible.
    top = arriba >= margen ? arriba : window.innerHeight - alto - margen;
  }
  panel.style.left = `${Math.max(left, margen)}px`;
  panel.style.top = `${Math.max(top, margen)}px`;
}

/** Abre y cierra un panel popover anclado a su botón, siguiéndolo al hacer scroll. */
class PanelFlotante {
  private escuchas: AbortController | null = null;
  private abierto = false;

  constructor(
    private readonly boton: () => HTMLElement,
    private readonly panel: () => HTMLElement,
  ) {}

  abrir(): void {
    const panel = this.panel();
    if (this.abierto) return;
    panel.showPopover?.();
    this.abierto = true;
    ubicarPanel(this.boton(), panel);
    this.escuchas = new AbortController();
    const reubicar = () => ubicarPanel(this.boton(), panel);
    window.addEventListener('resize', reubicar, { signal: this.escuchas.signal });
    window.addEventListener('scroll', reubicar, { signal: this.escuchas.signal, capture: true, passive: true });
  }

  cerrar(): void {
    if (this.abierto) this.panel().hidePopover?.();
    this.soltar();
  }

  /** Deja de seguir el scroll (también al cerrarse solo, con Escape o clic fuera). */
  soltar(): void {
    this.abierto = false;
    this.escuchas?.abort();
    this.escuchas = null;
  }
}

const BOTON_CAMPO =
  'input flex cursor-pointer items-center gap-2 text-left disabled:cursor-not-allowed disabled:opacity-60';
const PANEL =
  'fixed inset-auto m-0 rounded-2xl border border-border bg-card p-3 text-fg shadow-xl';

type Vista = 'dias' | 'meses' | 'anios';

/**
 * Selector de fecha con calendario del mes. Usa el mismo diseño en toda la app.
 * Trabaja con fechas ISO ("2026-10-14"); '' es "sin fecha".
 * Formularios: <app-selector-fecha formControlName="fecha" idCampo="fecha" />
 * Señales: <app-selector-fecha [(valor)]="fecha" />
 */
@Component({
  selector: 'app-selector-fecha',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideCalendarDays, LucideChevronLeft, LucideChevronRight, LucideChevronDown],
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => SelectorFecha), multi: true },
  ],
  host: { class: 'block' },
  template: `
    <button
      #boton
      type="button"
      [id]="idCampo()"
      [class]="botonClase()"
      aria-haspopup="dialog"
      [attr.aria-expanded]="abierto()"
      [attr.aria-invalid]="invalido() || null"
      [attr.aria-describedby]="describedby()"
      [attr.aria-label]="etiqueta() ? etiqueta() + ': ' + (texto() || placeholder()) : null"
      [disabled]="deshabilitado()"
      (click)="alternar()"
    >
      <svg lucideCalendarDays class="size-4 shrink-0 text-muted-fg" aria-hidden="true"></svg>
      <span class="min-w-0 flex-1 truncate" [class.text-muted-fg]="!texto()">
        {{ texto() || placeholder() }}
      </span>
    </button>

    <div
      #panel
      popover="auto"
      role="dialog"
      aria-label="Elegir fecha"
      [class]="panelClase"
      class="w-[min(18.5rem,calc(100vw-1rem))]"
      (toggle)="alCambiarPanel($any($event).newState)"
    >
      <div class="flex items-center justify-between gap-1">
        @switch (vista()) {
          @case ('dias') {
            <button type="button" class="encabezado-calendario" (click)="vista.set('meses')"
              aria-label="Elegir mes y año">
              {{ nombreMes(mesVisible()) }} {{ anioVisible() }}
              <svg lucideChevronDown class="size-4 text-muted-fg" aria-hidden="true"></svg>
            </button>
          }
          @case ('meses') {
            <button type="button" class="encabezado-calendario" (click)="abrirAnios()" aria-label="Elegir año">
              {{ anioVisible() }}
              <svg lucideChevronDown class="size-4 text-muted-fg" aria-hidden="true"></svg>
            </button>
          }
          @case ('anios') {
            <p class="px-2 text-sm font-semibold">Elige el año</p>
          }
        }
        @if (vista() !== 'anios') {
          <div class="flex">
            <button type="button" class="flecha-calendario" (click)="mover(-1)"
              [attr.aria-label]="vista() === 'dias' ? 'Mes anterior' : 'Año anterior'">
              <svg lucideChevronLeft class="size-4" aria-hidden="true"></svg>
            </button>
            <button type="button" class="flecha-calendario" (click)="mover(1)"
              [attr.aria-label]="vista() === 'dias' ? 'Mes siguiente' : 'Año siguiente'">
              <svg lucideChevronRight class="size-4" aria-hidden="true"></svg>
            </button>
          </div>
        }
      </div>

      @switch (vista()) {
        @case ('dias') {
          <div class="mt-2 grid grid-cols-7 text-center text-[11px] font-medium text-muted-fg" aria-hidden="true">
            @for (d of diasSemana; track $index) {
              <span class="py-1">{{ d }}</span>
            }
          </div>
          <div class="grid grid-cols-7 gap-0.5" (keydown)="teclaDia($event)">
            @for (c of celdas(); track c.iso) {
              <button
                type="button"
                class="dia-calendario"
                [class.dia-fuera]="c.fuera"
                [class.dia-hoy]="c.hoy"
                [class.dia-elegido]="c.iso === valor()"
                [attr.data-dia]="c.iso"
                [attr.tabindex]="c.iso === foco() ? 0 : -1"
                [attr.aria-label]="c.etiqueta"
                [attr.aria-pressed]="c.iso === valor()"
                [attr.aria-current]="c.hoy ? 'date' : null"
                [disabled]="c.deshabilitado"
                (click)="elegir(c.iso)"
              >
                {{ c.dia }}
              </button>
            }
          </div>
          <div class="mt-2 flex items-center justify-between border-t border-border pt-2">
            <button type="button" class="btn btn-ghost min-h-8 px-2 text-sm" [disabled]="!hoyPermitido()"
              (click)="elegir(hoy)">
              Hoy
            </button>
            @if (borrable() && valor()) {
              <button type="button" class="btn btn-ghost min-h-8 px-2 text-sm text-muted-fg" (click)="elegir('')">
                Borrar
              </button>
            }
          </div>
        }
        @case ('meses') {
          <div class="mt-3 grid grid-cols-3 gap-1.5">
            @for (m of meses; track $index) {
              <button
                type="button"
                class="celda-calendario"
                [class.dia-elegido]="m.indice === mesElegido() && anioVisible() === anioElegido()"
                [disabled]="!mesPermitido(m.indice)"
                (click)="irAMes(m.indice)"
              >
                {{ m.corto }}
              </button>
            }
          </div>
        }
        @case ('anios') {
          <div #listaAnios class="mt-3 grid max-h-64 grid-cols-4 gap-1.5 overflow-y-auto pr-1">
            @for (a of anios(); track a) {
              <button
                type="button"
                class="celda-calendario"
                [class.dia-elegido]="a === anioVisible()"
                [attr.data-anio]="a"
                (click)="irAAnio(a)"
              >
                {{ a }}
              </button>
            }
          </div>
        }
      }
    </div>
  `,
})
export class SelectorFecha implements ControlValueAccessor {
  /** Fecha ISO elegida; '' si no hay. */
  readonly valor = model('');
  /** Id del botón (para el <label for>). */
  readonly idCampo = input<string | null>(null);
  readonly min = input<string | null>(null);
  readonly max = input<string | null>(null);
  readonly placeholder = input('Elige una fecha');
  /** Nombre accesible cuando no hay <label for> (por ejemplo, en filtros compactos). */
  readonly etiqueta = input<string | null>(null);
  readonly invalido = input(false);
  readonly describedby = input<string | null>(null);
  /** Botón más bajo, para tablas y filas. */
  readonly compacto = input(false);
  /** Muestra "Borrar" para dejar el campo vacío (fechas opcionales). */
  readonly borrable = input(false);
  /** Sin fecha elegida, empieza en la lista de años (fechas de nacimiento). */
  readonly empezarEnAnios = input(false);

  private readonly injector = inject(Injector);
  private readonly boton = viewChild.required<ElementRef<HTMLButtonElement>>('boton');
  private readonly panel = viewChild.required<ElementRef<HTMLElement>>('panel');
  private readonly listaAnios = viewChild<ElementRef<HTMLElement>>('listaAnios');
  private readonly flotante = new PanelFlotante(
    () => this.boton().nativeElement,
    () => this.panel().nativeElement,
  );

  protected readonly diasSemana = DIAS_SEMANA;
  protected readonly meses = MESES.map((m, indice) => ({ indice, corto: capitalizar(m.slice(0, 3)) }));
  protected readonly hoy = fechaIso(new Date());
  protected readonly panelClase = PANEL;
  protected readonly abierto = signal(false);
  protected readonly deshabilitado = signal(false);
  protected readonly vista = signal<Vista>('dias');
  /** Primer día del mes que se está mostrando. */
  private readonly visible = signal(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  /** Día con el foco del teclado (ISO). */
  protected readonly foco = signal(this.hoy);

  protected readonly anioVisible = computed(() => this.visible().getFullYear());
  protected readonly mesVisible = computed(() => this.visible().getMonth());
  protected readonly anioElegido = computed(() => leerIso(this.valor())?.getFullYear() ?? null);
  protected readonly mesElegido = computed(() => leerIso(this.valor())?.getMonth() ?? null);

  protected readonly botonClase = computed(
    () => `${BOTON_CAMPO} ${this.compacto() ? 'min-h-9 py-1' : ''} ${this.abierto() ? 'border-ring ring-3 ring-ring/15' : ''}`,
  );

  protected readonly texto = computed(() => {
    const fecha = leerIso(this.valor());
    if (!fecha) return '';
    return new Intl.DateTimeFormat('es-EC', {
      day: 'numeric',
      month: this.compacto() ? 'short' : 'long',
      year: 'numeric',
    }).format(fecha);
  });

  /** Seis semanas fijas (de lunes a domingo) para que el panel no cambie de alto. */
  protected readonly celdas = computed(() => {
    const anio = this.anioVisible();
    const mes = this.mesVisible();
    const desfase = (new Date(anio, mes, 1).getDay() + 6) % 7;
    return Array.from({ length: 42 }, (_, i) => {
      const fecha = new Date(anio, mes, 1 - desfase + i);
      const iso = fechaIso(fecha);
      return {
        iso,
        dia: fecha.getDate(),
        fuera: fecha.getMonth() !== mes,
        hoy: iso === this.hoy,
        deshabilitado: !this.permitido(iso),
        etiqueta: `${DIAS_SEMANA_LARGOS[(fecha.getDay() + 6) % 7]}, ${fecha.getDate()} de ${MESES[fecha.getMonth()]} de ${fecha.getFullYear()}`,
      };
    });
  });

  protected readonly hoyPermitido = computed(() => this.permitido(this.hoy));

  protected readonly anios = computed(() => {
    const actual = new Date().getFullYear();
    const desde = leerIso(this.min())?.getFullYear() ?? actual - 100;
    const hasta = leerIso(this.max())?.getFullYear() ?? actual + 10;
    return Array.from({ length: Math.max(hasta - desde + 1, 1) }, (_, i) => desde + i);
  });

  private alCambiar: (valor: string) => void = () => {};
  private alTocar: () => void = () => {};

  constructor() {
    inject(DestroyRef).onDestroy(() => this.flotante.soltar());
  }

  protected nombreMes(mes: number): string {
    return capitalizar(MESES[mes]);
  }

  protected alternar(): void {
    if (this.abierto()) {
      this.flotante.cerrar();
      return;
    }
    const elegida = leerIso(this.valor());
    const base = elegida ?? this.limitar(new Date());
    this.visible.set(new Date(base.getFullYear(), base.getMonth(), 1));
    this.foco.set(fechaIso(base));
    this.vista.set(!elegida && this.empezarEnAnios() ? 'anios' : 'dias');
    this.flotante.abrir();
    if (this.vista() === 'anios') this.mostrarAnioElegido();
    else this.enfocarDia();
  }

  protected alCambiarPanel(estado: string): void {
    const abierto = estado === 'open';
    this.abierto.set(abierto);
    if (!abierto) {
      this.flotante.soltar();
      this.alTocar();
    }
  }

  protected elegir(iso: string): void {
    this.valor.set(iso);
    this.alCambiar(iso);
    this.flotante.cerrar();
    this.boton().nativeElement.focus();
  }

  protected mover(paso: number): void {
    const v = this.visible();
    this.visible.set(
      this.vista() === 'dias'
        ? new Date(v.getFullYear(), v.getMonth() + paso, 1)
        : new Date(v.getFullYear() + paso, v.getMonth(), 1),
    );
  }

  protected abrirAnios(): void {
    this.vista.set('anios');
    this.mostrarAnioElegido();
  }

  protected irAAnio(anio: number): void {
    this.visible.set(new Date(anio, this.mesVisible(), 1));
    this.vista.set('meses');
  }

  protected irAMes(mes: number): void {
    this.visible.set(new Date(this.anioVisible(), mes, 1));
    this.vista.set('dias');
    const elegida = leerIso(this.valor());
    const mismoMes = elegida && elegida.getFullYear() === this.anioVisible() && elegida.getMonth() === mes;
    this.foco.set(mismoMes ? this.valor() : fechaIso(new Date(this.anioVisible(), mes, 1)));
    this.enfocarDia();
  }

  protected mesPermitido(mes: number): boolean {
    const anio = this.anioVisible();
    const min = this.min();
    const max = this.max();
    return (
      (!max || fechaIso(new Date(anio, mes, 1)) <= max) &&
      (!min || fechaIso(new Date(anio, mes + 1, 0)) >= min)
    );
  }

  /** Flechas, Inicio/Fin y RePág/AvPág mueven el foco por el calendario. */
  protected teclaDia(evento: KeyboardEvent): void {
    const actual = leerIso(this.foco());
    if (!actual) return;
    const lunes = (actual.getDay() + 6) % 7;
    const pasos: Record<string, () => Date> = {
      ArrowLeft: () => new Date(actual.getFullYear(), actual.getMonth(), actual.getDate() - 1),
      ArrowRight: () => new Date(actual.getFullYear(), actual.getMonth(), actual.getDate() + 1),
      ArrowUp: () => new Date(actual.getFullYear(), actual.getMonth(), actual.getDate() - 7),
      ArrowDown: () => new Date(actual.getFullYear(), actual.getMonth(), actual.getDate() + 7),
      Home: () => new Date(actual.getFullYear(), actual.getMonth(), actual.getDate() - lunes),
      End: () => new Date(actual.getFullYear(), actual.getMonth(), actual.getDate() + 6 - lunes),
      PageUp: () => new Date(actual.getFullYear() - (evento.shiftKey ? 1 : 0), actual.getMonth() - (evento.shiftKey ? 0 : 1), actual.getDate()),
      PageDown: () => new Date(actual.getFullYear() + (evento.shiftKey ? 1 : 0), actual.getMonth() + (evento.shiftKey ? 0 : 1), actual.getDate()),
    };
    const siguiente = pasos[evento.key]?.();
    if (!siguiente) return;
    evento.preventDefault();
    this.foco.set(fechaIso(siguiente));
    if (siguiente.getMonth() !== this.mesVisible() || siguiente.getFullYear() !== this.anioVisible())
      this.visible.set(new Date(siguiente.getFullYear(), siguiente.getMonth(), 1));
    this.enfocarDia();
  }

  writeValue(valor: string | null): void {
    this.valor.set(valor ?? '');
  }

  registerOnChange(fn: (valor: string) => void): void {
    this.alCambiar = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.alTocar = fn;
  }

  setDisabledState(deshabilitado: boolean): void {
    this.deshabilitado.set(deshabilitado);
  }

  private permitido(iso: string): boolean {
    const min = this.min();
    const max = this.max();
    return (!min || iso >= min) && (!max || iso <= max);
  }

  /** Hoy, o el límite más cercano si hoy queda fuera del rango permitido. */
  private limitar(fecha: Date): Date {
    const iso = fechaIso(fecha);
    const max = this.max();
    const min = this.min();
    if (max && iso > max) return leerIso(max) ?? fecha;
    if (min && iso < min) return leerIso(min) ?? fecha;
    return fecha;
  }

  private enfocarDia(): void {
    afterNextRender(
      () => {
        const dia = this.panel().nativeElement.querySelector<HTMLButtonElement>(`[data-dia="${this.foco()}"]`);
        dia?.focus();
        ubicarPanel(this.boton().nativeElement, this.panel().nativeElement);
      },
      { injector: this.injector },
    );
  }

  private mostrarAnioElegido(): void {
    afterNextRender(
      () => {
        const anio = this.listaAnios()?.nativeElement.querySelector<HTMLElement>(`[data-anio="${this.anioVisible()}"]`);
        anio?.scrollIntoView({ block: 'center' });
        anio?.focus({ preventScroll: true });
        ubicarPanel(this.boton().nativeElement, this.panel().nativeElement);
      },
      { injector: this.injector },
    );
  }
}

/**
 * Selector del día del mes (1 a 31) con el mismo diseño del calendario,
 * para vencimientos que se repiten cada mes.
 */
@Component({
  selector: 'app-selector-dia',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideCalendarDays],
  host: { class: 'block' },
  template: `
    <button
      #boton
      type="button"
      [id]="idCampo()"
      [class]="botonClase()"
      aria-haspopup="dialog"
      [attr.aria-expanded]="abierto()"
      (click)="abierto() ? flotante.cerrar() : abrir()"
    >
      <svg lucideCalendarDays class="size-4 shrink-0 text-muted-fg" aria-hidden="true"></svg>
      <span class="min-w-0 flex-1 truncate" [class.text-muted-fg]="!valor()">
        {{ valor() ? 'Día ' + valor() + ' de cada mes' : placeholder() }}
      </span>
    </button>

    <div
      #panel
      popover="auto"
      role="dialog"
      aria-label="Elegir día del mes"
      [class]="panelClase"
      class="w-[min(18.5rem,calc(100vw-1rem))]"
      (toggle)="alCambiarPanel($any($event).newState)"
    >
      <p class="px-1 text-sm font-semibold">¿Qué día del mes vence?</p>
      <div class="mt-2 grid grid-cols-7 gap-0.5">
        @for (d of dias; track d) {
          <button
            type="button"
            class="dia-calendario"
            [class.dia-elegido]="d === valor()"
            [attr.aria-pressed]="d === valor()"
            [attr.data-dia]="d"
            (click)="elegir(d)"
          >
            {{ d }}
          </button>
        }
      </div>
      @if (valor()) {
        <div class="mt-2 border-t border-border pt-2">
          <button type="button" class="btn btn-ghost min-h-8 px-2 text-sm text-muted-fg" (click)="elegir(null)">
            Sin día fijo
          </button>
        </div>
      }
    </div>
  `,
})
export class SelectorDia {
  readonly valor = model<number | null>(null);
  readonly idCampo = input<string | null>(null);
  readonly placeholder = input('Opcional');

  private readonly injector = inject(Injector);
  private readonly boton = viewChild.required<ElementRef<HTMLButtonElement>>('boton');
  private readonly panel = viewChild.required<ElementRef<HTMLElement>>('panel');
  protected readonly flotante = new PanelFlotante(
    () => this.boton().nativeElement,
    () => this.panel().nativeElement,
  );

  protected readonly dias = Array.from({ length: 31 }, (_, i) => i + 1);
  protected readonly panelClase = PANEL;
  protected readonly abierto = signal(false);
  protected readonly botonClase = computed(
    () => `${BOTON_CAMPO} ${this.abierto() ? 'border-ring ring-3 ring-ring/15' : ''}`,
  );

  constructor() {
    inject(DestroyRef).onDestroy(() => this.flotante.soltar());
  }

  protected abrir(): void {
    this.flotante.abrir();
    afterNextRender(
      () =>
        this.panel()
          .nativeElement.querySelector<HTMLButtonElement>(`[data-dia="${this.valor() ?? 1}"]`)
          ?.focus(),
      { injector: this.injector },
    );
  }

  protected alCambiarPanel(estado: string): void {
    this.abierto.set(estado === 'open');
    if (estado !== 'open') this.flotante.soltar();
  }

  protected elegir(dia: number | null): void {
    this.valor.set(dia);
    this.flotante.cerrar();
    this.boton().nativeElement.focus();
  }
}
