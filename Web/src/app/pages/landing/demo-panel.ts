import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  ElementRef,
  inject,
  input,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import {
  LucideBell,
  LucideCircleCheck,
  LucideHandCoins,
  LucideInbox,
  LucideLayoutDashboard,
  LucideLoaderCircle,
  LucideMousePointer2,
  LucideReceipt,
  LucideSearch,
} from '@lucide/angular';
import { MONEDA } from '@core/utils';
import { CategoriaDemo, LlegadaDemo, MovimientoDemo, VistaDemo } from '@core/models';

const sinMovimiento = () => typeof matchMedia !== 'undefined' && matchMedia('(prefers-reduced-motion: reduce)').matches;

/** Muestra un monto que sube o baja hasta su nuevo valor (sin animación si se pide reducir movimiento). */
@Component({
  selector: 'app-contador',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `{{ texto() }}`,
})
export class Contador {
  readonly valor = input.required<number>();
  private readonly actual = signal(0);
  protected readonly texto = computed(() => MONEDA.format(this.actual()));

  constructor() {
    let cuadro = 0;
    effect(() => {
      const destino = this.valor();
      const desde = untracked(this.actual);
      cancelAnimationFrame(cuadro);
      if (sinMovimiento() || desde === destino) {
        this.actual.set(destino);
        return;
      }
      const inicio = performance.now();
      const paso = (ahora: number) => {
        const t = Math.min(1, (ahora - inicio) / 900);
        this.actual.set(desde + (destino - desde) * (1 - Math.pow(1 - t, 3)));
        if (t < 1) cuadro = requestAnimationFrame(paso);
      };
      cuadro = requestAnimationFrame(paso);
    });
    inject(DestroyRef).onDestroy(() => cancelAnimationFrame(cuadro));
  }
}

const CATEGORIAS: CategoriaDemo[] = ['Vivienda', 'Salud', 'Alimentación', 'Educación', 'Vestimenta'];

const PASOS = ['Correo recibido', 'XML leído', 'Verificado con el SRI', 'Registrado'];

/** Montos del mes anteriores a los movimientos visibles (datos ilustrativos). */
const BASE = {
  ingresos: 11503,
  gastos: 1890.35,
  deducibles: { Vivienda: 1350, Salud: 600, Alimentación: 520, Educación: 300, Vestimenta: 90 } as Record<CategoriaDemo, number>,
  clientes: { 'Constructora del Pacífico': 3200, 'Estudio Andrade & Asoc.': 2100, 'Comercial Ríos': 1450 } as Record<string, number>,
};

const TOPE = 5752.6;

const INICIALES: MovimientoDemo[] = [
  { id: 1, fecha: '09 oct', nombre: 'Constructora del Pacífico', monto: 977.5, ingreso: true, categoria: null },
  { id: 2, fecha: '08 oct', nombre: 'De Prati', monto: 91.89, ingreso: false, categoria: 'Vestimenta' },
  { id: 3, fecha: '05 oct', nombre: 'Inmobiliaria Los Ceibos', monto: 450, ingreso: false, categoria: 'Vivienda' },
  { id: 4, fecha: '03 oct', nombre: 'Fybeca', monto: 43.92, ingreso: false, categoria: 'Salud' },
];

const POR_LLEGAR: Omit<MovimientoDemo, 'id'>[] = [
  { fecha: '10 oct', nombre: 'Supermaxi', monto: 101.68, ingreso: false, categoria: 'Alimentación' },
  { fecha: '10 oct', nombre: 'Estudio Andrade & Asoc.', monto: 640, ingreso: true, categoria: null },
  { fecha: '10 oct', nombre: 'Farmacias Cruz Azul', monto: 23.4, ingreso: false, categoria: 'Salud' },
  { fecha: '10 oct', nombre: 'Unidad Educativa Delta', monto: 180, ingreso: false, categoria: 'Educación' },
  { fecha: '10 oct', nombre: 'Comercial Ríos', monto: 1215.3, ingreso: true, categoria: null },
  { fecha: '10 oct', nombre: 'Etafashion', monto: 64.9, ingreso: false, categoria: 'Vestimenta' },
];

const RECORRIDO: VistaDemo[] = ['buzon', 'resumen', 'gastos', 'ingresos', 'resumen'];

/**
 * Demo interactiva del panel para la landing: se puede navegar con clics, las facturas
 * "llegan" solas al buzón y, si nadie la toca, un cursor la recorre por su cuenta.
 * Todos los datos son ilustrativos.
 */
@Component({
  selector: 'app-demo-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Contador, LucideSearch, LucideBell, LucideCircleCheck, LucideLayoutDashboard, LucideReceipt, LucideHandCoins,
    LucideInbox, LucideMousePointer2, LucideLoaderCircle,
  ],
  host: { '(pointerdown)': 'interaccion()', '(keydown)': 'interaccion()' },
  template: `
    <div #marco class="relative flex overflow-hidden rounded-xl border border-border bg-card text-left text-[11px] select-none"
      role="group" aria-label="Demostración interactiva de Nudo con datos de ejemplo">
      <!-- Barra lateral -->
      <nav class="hidden w-44 shrink-0 flex-col gap-0.5 border-r border-border p-3 md:flex" aria-label="Secciones de la demo">
        <div class="mb-4 flex items-center gap-1.5 px-2 font-semibold">
          <span class="grid size-5 place-items-center rounded-md bg-ink text-[10px] text-ink-fg">N</span> Nudo
        </div>
        @for (s of secciones; track s.vista) {
          <button type="button" [attr.data-vista]="s.vista" (click)="ir(s.vista)" [attr.aria-pressed]="vista() === s.vista"
            class="flex cursor-pointer items-center gap-2 rounded-md px-2 py-1.5 text-left transition-colors"
            [class]="vista() === s.vista ? 'bg-muted font-medium text-fg' : 'text-muted-fg hover:bg-muted/60 hover:text-fg'">
            @switch (s.vista) {
              @case ('resumen') { <svg lucideLayoutDashboard class="size-3.5" aria-hidden="true"></svg> }
              @case ('gastos') { <svg lucideReceipt class="size-3.5" aria-hidden="true"></svg> }
              @case ('ingresos') { <svg lucideHandCoins class="size-3.5" aria-hidden="true"></svg> }
              @case ('buzon') { <svg lucideInbox class="size-3.5" aria-hidden="true"></svg> }
            }
            <span class="flex-1">{{ s.nombre }}</span>
            @if (s.vista === 'buzon' && nuevas() > 0) {
              <span class="rounded-full bg-brand px-1.5 text-[9px] font-medium text-brand-ink">{{ nuevas() }}</span>
            }
          </button>
        }
        <div class="mt-auto rounded-lg bg-muted/60 p-2.5">
          <p class="text-[10px] text-muted-fg">Tope deducible</p>
          <div class="mt-1.5 h-1.5 overflow-hidden rounded-full bg-border">
            <div class="h-full rounded-full bg-brand transition-[width] duration-700" [style.width.%]="avanceTope()"></div>
          </div>
          <p class="mt-1 text-[10px] font-medium">{{ avanceTope() }} % usado</p>
        </div>
      </nav>

      <div class="flex min-w-0 flex-1 flex-col">
        <!-- Barra superior -->
        <div class="flex items-center justify-between gap-3 border-b border-border px-4 py-2.5">
          <div class="flex w-full max-w-56 items-center gap-2 rounded-md border border-border px-2 py-1 text-muted-fg">
            <svg lucideSearch class="size-3" aria-hidden="true"></svg>
            <span class="flex-1 truncate">Buscar comprobante</span>
          </div>
          <div class="flex items-center gap-3">
            <button type="button" (click)="ir('buzon')" class="hidden cursor-pointer rounded-full bg-brand px-3 py-1 text-[10px] font-medium text-brand-ink transition-colors hover:bg-brand-hover sm:inline">
              Traer facturas
            </button>
            <span class="relative">
              <svg lucideBell class="size-3.5 text-muted-fg" aria-hidden="true"></svg>
              @if (nuevas() > 0) {
                <span class="absolute -top-0.5 -right-0.5 size-1.5 rounded-full bg-danger"></span>
              }
            </span>
            <span class="grid size-6 place-items-center rounded-full bg-primary-soft text-[9px] font-semibold text-primary">DA</span>
          </div>
        </div>

        <!-- Pestañas en pantallas pequeñas -->
        <div class="flex gap-1 overflow-x-auto border-b border-border px-3 py-2 md:hidden">
          @for (s of secciones; track s.vista) {
            <button type="button" [attr.data-vista]="s.vista" (click)="ir(s.vista)" [attr.aria-pressed]="vista() === s.vista"
              class="shrink-0 cursor-pointer rounded-full px-3 py-1 transition-colors"
              [class]="vista() === s.vista ? 'bg-ink text-ink-fg' : 'text-muted-fg hover:bg-muted'">{{ s.nombre }}</button>
          }
        </div>

        <!-- Contenido -->
        <div class="relative h-[400px] overflow-hidden bg-muted/30 p-4 md:h-[380px]">
          @switch (vista()) {
            @case ('resumen') {
              <div class="space-y-3 animate-aparecer [--desde:8px]">
                <div class="flex items-baseline justify-between">
                  <p class="text-sm font-semibold">Hola, David</p>
                  <p class="text-muted-fg">Octubre 2026</p>
                </div>
                <div class="grid grid-cols-3 gap-2">
                  @for (k of kpis(); track k.nombre) {
                    <div class="rounded-lg border border-border bg-card p-2.5">
                      <p class="truncate text-muted-fg">{{ k.nombre }}</p>
                      <p class="mt-0.5 text-sm font-semibold tracking-tight tabular-nums sm:text-base" [class]="k.clase"><app-contador [valor]="k.valor" /></p>
                    </div>
                  }
                </div>
                <div class="grid gap-3 sm:grid-cols-[1.3fr_1fr]">
                  <div class="rounded-lg border border-border bg-card p-3">
                    <p class="text-muted-fg">Ingresos, últimos 6 meses</p>
                    <svg viewBox="0 0 300 80" preserveAspectRatio="none" class="mt-2 h-16 w-full" aria-hidden="true">
                      <defs>
                        <linearGradient id="demo-area" x1="0" x2="0" y1="0" y2="1">
                          <stop offset="0%" stop-color="var(--brand)" stop-opacity="0.18" />
                          <stop offset="100%" stop-color="var(--brand)" stop-opacity="0" />
                        </linearGradient>
                      </defs>
                      <path class="animate-aparecer [animation-delay:600ms]" d="M0,62 C30,58 45,40 75,44 C105,48 120,30 150,28 C180,26 195,40 225,30 C255,20 270,12 300,8 L300,80 L0,80 Z" fill="url(#demo-area)" />
                      <path class="animate-trazar [stroke-dasharray:1]" pathLength="1" d="M0,62 C30,58 45,40 75,44 C105,48 120,30 150,28 C180,26 195,40 225,30 C255,20 270,12 300,8" fill="none" stroke="var(--brand)" stroke-width="2" vector-effect="non-scaling-stroke" />
                    </svg>
                  </div>
                  <div class="rounded-lg border border-border bg-card p-3">
                    <div class="flex justify-between"><p class="text-muted-fg">Tope deducible</p><p class="font-medium">{{ avanceTope() }} %</p></div>
                    <div class="mt-2 h-2 overflow-hidden rounded-full bg-muted">
                      <div class="h-full rounded-full bg-brand transition-[width] duration-700" [style.width.%]="avanceTope()"></div>
                    </div>
                    <p class="mt-2 text-muted-fg">Te faltan <span class="font-medium text-fg"><app-contador [valor]="faltaTope()" /></span> para la rebaja máxima.</p>
                  </div>
                </div>
                <div class="rounded-lg border border-border bg-card p-3">
                  <p class="mb-1 font-medium">Comprobantes recientes</p>
                  <table class="w-full">
                    <tbody>
                      @for (m of recientes(); track m.id) {
                        <tr class="border-t border-border first:border-t-0" [class.animate-resaltar]="m.id > 100">
                          <td class="w-12 py-1.5 text-muted-fg">{{ m.fecha }}</td>
                          <td class="truncate py-1.5">{{ m.nombre }}</td>
                          <td class="py-1.5 text-right font-medium tabular-nums" [class.text-success]="m.ingreso">{{ signo(m) }}</td>
                          <td class="hidden py-1.5 text-right sm:table-cell">
                            <span class="pildora bg-success-soft text-[10px] text-success">Verificado</span>
                          </td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
              </div>
            }

            @case ('gastos') {
              <div class="space-y-3 animate-aparecer [--desde:8px]">
                <div class="flex items-baseline justify-between">
                  <p class="text-sm font-semibold">Gastos de octubre</p>
                  <p class="font-semibold tabular-nums"><app-contador [valor]="totalGastos()" /></p>
                </div>
                <div class="flex flex-wrap gap-1.5">
                  @for (c of filtrosGasto; track c) {
                    <button type="button" (click)="filtro.set(c)" [attr.aria-pressed]="filtro() === c"
                      class="cursor-pointer rounded-full border px-2.5 py-0.5 transition-colors"
                      [class]="filtro() === c ? 'border-ink bg-ink text-ink-fg' : 'border-border bg-card hover:bg-muted'">{{ c }}</button>
                  }
                </div>
                <div class="grid gap-3 sm:grid-cols-[1fr_1.2fr]">
                  <div class="rounded-lg border border-border bg-card p-3">
                    <p class="mb-2 text-muted-fg">Deducibles por categoría</p>
                    @for (c of porCategoria(); track c.nombre) {
                      <button type="button" (click)="filtro.set(c.nombre)" class="block w-full cursor-pointer rounded py-1 text-left hover:bg-muted/60">
                        <span class="flex justify-between"><span>{{ c.nombre }}</span><span class="tabular-nums text-muted-fg">{{ c.texto }}</span></span>
                        <span class="mt-1 block h-1.5 overflow-hidden rounded-full bg-muted">
                          <span class="block h-full rounded-full transition-[width] duration-700"
                            [class]="filtro() === 'Todas' || filtro() === c.nombre ? 'bg-brand' : 'bg-brand/30'" [style.width.%]="c.porcentaje"></span>
                        </span>
                      </button>
                    }
                  </div>
                  <div class="rounded-lg border border-border bg-card p-3">
                    <p class="mb-1 text-muted-fg">Comprobantes</p>
                    @for (m of gastosFiltrados(); track m.id) {
                      <div class="flex items-center justify-between gap-2 border-t border-border py-1.5 first:border-t-0" [class.animate-resaltar]="m.id > 100">
                        <span class="min-w-0">
                          <span class="block truncate">{{ m.nombre }}</span>
                          <span class="text-[10px] text-muted-fg">{{ m.fecha }} · {{ m.categoria }}</span>
                        </span>
                        <span class="shrink-0 font-medium tabular-nums">{{ signo(m) }}</span>
                      </div>
                    } @empty {
                      <p class="py-6 text-center text-muted-fg">Sin gastos en esta categoría.</p>
                    }
                  </div>
                </div>
              </div>
            }

            @case ('ingresos') {
              <div class="space-y-3 animate-aparecer [--desde:8px]">
                <div class="flex items-baseline justify-between">
                  <p class="text-sm font-semibold">Ingresos y clientes</p>
                  <p class="font-semibold text-success tabular-nums"><app-contador [valor]="totalIngresos()" /></p>
                </div>
                <div class="grid grid-cols-2 gap-2">
                  <div class="rounded-lg border border-border bg-card p-2.5">
                    <p class="text-muted-fg">IVA cobrado</p>
                    <p class="mt-0.5 text-sm font-semibold tabular-nums"><app-contador [valor]="ivaCobrado()" /></p>
                  </div>
                  <div class="rounded-lg border border-border bg-card p-2.5">
                    <p class="text-muted-fg">IVA pagado en compras</p>
                    <p class="mt-0.5 text-sm font-semibold tabular-nums"><app-contador [valor]="ivaPagado()" /></p>
                  </div>
                </div>
                <div class="rounded-lg border border-border bg-card p-3">
                  <p class="mb-2 text-muted-fg">Lo que te compró cada cliente</p>
                  @for (c of clientes(); track c.nombre; let i = $index) {
                    <div class="py-1.5">
                      <div class="flex justify-between"><span>{{ c.nombre }}</span><span class="font-medium tabular-nums">{{ c.texto }}</span></div>
                      <div class="mt-1 h-1.5 overflow-hidden rounded-full bg-muted">
                        <div class="h-full rounded-full bg-success transition-[width] duration-700" [style.width.%]="c.porcentaje"></div>
                      </div>
                    </div>
                  }
                </div>
              </div>
            }

            @case ('buzon') {
              <div class="space-y-3 animate-aparecer [--desde:8px]">
                <div>
                  <p class="text-sm font-semibold">Tu buzón de facturas</p>
                  <p class="mt-0.5 text-muted-fg">Reenvía tus facturas a
                    <span class="rounded bg-card px-1.5 py-0.5 font-mono text-fg ring-1 ring-border">david7k2&#64;facturas.nudo.ec</span>
                  </p>
                </div>
                <div class="space-y-2">
                  @for (l of llegadas(); track l.id) {
                    <div class="rounded-lg border border-border bg-card p-3 animate-deslizar">
                      <div class="flex items-center justify-between gap-2">
                        <span class="min-w-0 truncate font-medium">{{ l.movimiento.nombre }}</span>
                        <span class="shrink-0 font-medium tabular-nums" [class.text-success]="l.movimiento.ingreso">{{ signo(l.movimiento) }}</span>
                      </div>
                      <ol class="mt-2 grid grid-cols-4 gap-1.5">
                        @for (p of pasos; track p; let i = $index) {
                          <li class="min-w-0">
                            <span class="block h-1 rounded-full transition-colors duration-500" [class]="i <= l.paso ? 'bg-success' : 'bg-muted'"></span>
                            <span class="mt-1 flex items-center gap-1 truncate text-[10px]" [class]="i <= l.paso ? 'text-fg' : 'text-muted-fg'">
                              @if (i === l.paso + 1) {
                                <svg lucideLoaderCircle class="size-2.5 shrink-0 animate-spin" aria-hidden="true"></svg>
                              }
                              {{ p }}
                            </span>
                          </li>
                        }
                      </ol>
                    </div>
                  } @empty {
                    <p class="rounded-lg border border-dashed border-border p-6 text-center text-muted-fg">Esperando el próximo correo…</p>
                  }
                </div>
              </div>
            }
          }

          <!-- Aviso de factura nueva -->
          @for (a of aviso(); track a.id) {
            <div class="absolute right-3 bottom-3 flex max-w-[240px] items-center gap-2 rounded-lg bg-ink px-3 py-2 text-ink-fg shadow-lg animate-deslizar" aria-hidden="true">
              <svg lucideCircleCheck class="size-4 shrink-0 text-success" aria-hidden="true"></svg>
              <span class="min-w-0">
                <span class="block truncate font-medium">{{ a.movimiento.nombre }}</span>
                <span class="text-[10px] opacity-70">{{ a.movimiento.ingreso ? 'Ingreso' : 'Gasto' }} verificado · {{ signo(a.movimiento) }}</span>
              </span>
            </div>
          }
        </div>
      </div>

      <!-- Cursor del recorrido automático -->
      @if (cursor(); as c) {
        <div class="pointer-events-none absolute top-0 left-0 z-10 transition-transform duration-700 ease-in-out" aria-hidden="true"
          [style.transform]="'translate(' + c.x + 'px,' + c.y + 'px)'">
          @if (c.clic) {
            <span class="absolute -top-3 -left-3 size-7 rounded-full bg-brand/40 animate-onda"></span>
          }
          <svg lucideMousePointer2 class="size-5 fill-ink text-white drop-shadow-md"></svg>
        </div>
      }
    </div>
  `,
})
export class DemoPanel {
  private readonly marco = viewChild.required<ElementRef<HTMLElement>>('marco');

  protected readonly secciones: { vista: VistaDemo; nombre: string }[] = [
    { vista: 'resumen', nombre: 'Resumen' },
    { vista: 'gastos', nombre: 'Gastos' },
    { vista: 'ingresos', nombre: 'Ingresos' },
    { vista: 'buzon', nombre: 'Buzón' },
  ];
  protected readonly pasos = PASOS;
  protected readonly filtrosGasto: ('Todas' | CategoriaDemo)[] = ['Todas', ...CATEGORIAS];

  protected readonly vista = signal<VistaDemo>('resumen');
  protected readonly filtro = signal<'Todas' | CategoriaDemo>('Todas');
  private readonly movimientos = signal<MovimientoDemo[]>(INICIALES);
  protected readonly llegadas = signal<LlegadaDemo[]>([]);
  protected readonly aviso = signal<LlegadaDemo[]>([]);
  protected readonly nuevas = signal(0);
  protected readonly cursor = signal<{ x: number; y: number; clic: boolean } | null>(null);

  protected readonly totalIngresos = computed(() => BASE.ingresos + this.suma((m) => m.ingreso));
  protected readonly totalGastos = computed(() => BASE.gastos + this.suma((m) => !m.ingreso));
  protected readonly kpis = computed(() => [
    { nombre: 'Ingresos', valor: this.totalIngresos(), clase: 'text-success' },
    { nombre: 'Gastos', valor: this.totalGastos(), clase: '' },
    { nombre: 'Balance', valor: this.totalIngresos() - this.totalGastos(), clase: '' },
  ]);
  protected readonly ivaCobrado = computed(() => this.totalIngresos() * 0.15);
  protected readonly ivaPagado = computed(() => this.totalGastos() * 0.15 * 0.62);

  private readonly deducibles = computed(() => {
    const totales = { ...BASE.deducibles };
    for (const m of this.movimientos()) if (m.categoria) totales[m.categoria] += m.monto;
    return totales;
  });
  protected readonly avanceTope = computed(() => {
    const usado = Object.values(this.deducibles()).reduce((a, b) => a + b, 0);
    return Math.min(100, Math.round((usado / TOPE) * 100));
  });
  protected readonly faltaTope = computed(() => Math.max(0, TOPE - Object.values(this.deducibles()).reduce((a, b) => a + b, 0)));
  protected readonly porCategoria = computed(() => {
    const d = this.deducibles();
    const max = Math.max(...Object.values(d));
    return CATEGORIAS.map((nombre) => ({ nombre, texto: MONEDA.format(d[nombre]), porcentaje: (d[nombre] / max) * 100 }));
  });
  protected readonly clientes = computed(() => {
    const totales = { ...BASE.clientes };
    for (const m of this.movimientos()) if (m.ingreso) totales[m.nombre] = (totales[m.nombre] ?? 0) + m.monto;
    const max = Math.max(...Object.values(totales));
    return Object.entries(totales)
      .sort((a, b) => b[1] - a[1])
      .map(([nombre, total]) => ({ nombre, texto: MONEDA.format(total), porcentaje: (total / max) * 100 }));
  });
  protected readonly recientes = computed(() => this.movimientos().slice(0, 5));
  protected readonly gastosFiltrados = computed(() => {
    const f = this.filtro();
    return this.movimientos()
      .filter((m) => !m.ingreso && (f === 'Todas' || m.categoria === f))
      .slice(0, 6);
  });

  private visible = false;
  private ultimaInteraccion = 0;
  private siguienteLlegada = 0;
  private pasoRecorrido = 0;
  private readonly temporizadores = new Set<ReturnType<typeof setTimeout>>();

  constructor() {
    const destroyRef = inject(DestroyRef);
    afterNextRender(() => {
      const observador = new IntersectionObserver(([e]) => (this.visible = e.isIntersecting), { threshold: 0.3 });
      observador.observe(this.marco().nativeElement);
      const llegadas = setInterval(() => this.activa() && this.llegar(), 4200);
      const recorrido = setInterval(() => this.activa() && Date.now() - this.ultimaInteraccion > 12000 && this.recorrer(), 5600);
      this.esperar(1200, () => this.activa() && this.llegar());
      destroyRef.onDestroy(() => {
        observador.disconnect();
        clearInterval(llegadas);
        clearInterval(recorrido);
        this.temporizadores.forEach(clearTimeout);
      });
    });
  }

  protected ir(vista: VistaDemo): void {
    this.vista.set(vista);
    if (vista === 'buzon') this.nuevas.set(0);
  }

  /** Cualquier clic o tecla del visitante pausa el recorrido automático y esconde el cursor. */
  protected interaccion(): void {
    this.ultimaInteraccion = Date.now();
    this.cursor.set(null);
  }

  protected signo(m: MovimientoDemo): string {
    return (m.ingreso ? '+' : '−') + MONEDA.format(m.monto);
  }

  private suma(filtro: (m: MovimientoDemo) => boolean): number {
    return this.movimientos().reduce((total, m) => (filtro(m) ? total + m.monto : total), 0);
  }

  private activa(): boolean {
    return this.visible && document.visibilityState === 'visible';
  }

  /** Llega una factura nueva: avanza por los pasos del buzón y al final suma al mes. */
  private llegar(): void {
    if (this.siguienteLlegada >= POR_LLEGAR.length) {
      // Fin de la secuencia: vuelve al estado inicial y empieza de nuevo.
      this.siguienteLlegada = 0;
      this.movimientos.set(INICIALES);
      this.llegadas.set([]);
      return;
    }
    const id = 101 + this.siguienteLlegada;
    const movimiento: MovimientoDemo = { ...POR_LLEGAR[this.siguienteLlegada++], id };
    this.llegadas.update((l) => [{ id, movimiento, paso: 0 }, ...l].slice(0, 3));
    for (const paso of [1, 2, 3]) {
      this.esperar(paso * 650, () => this.llegadas.update((l) => l.map((x) => (x.id === id ? { ...x, paso } : x))));
    }
    this.esperar(3 * 650, () => {
      this.movimientos.update((m) => [movimiento, ...m]);
      if (this.vista() !== 'buzon') this.nuevas.update((n) => n + 1);
      this.aviso.set([{ id, movimiento, paso: 3 }]);
      this.esperar(2600, () => this.aviso.update((a) => a.filter((x) => x.id !== id)));
    });
  }

  /** Mueve el cursor hasta la siguiente sección, "hace clic" y la abre. */
  private recorrer(): void {
    const vista = RECORRIDO[this.pasoRecorrido++ % RECORRIDO.length];
    const marco = this.marco().nativeElement;
    const boton = [...marco.querySelectorAll<HTMLElement>(`[data-vista="${vista}"]`)].find((b) => b.offsetParent !== null);
    if (!boton) return;
    const m = marco.getBoundingClientRect();
    const b = boton.getBoundingClientRect();
    if (!this.cursor()) this.cursor.set({ x: m.width * 0.6, y: m.height * 0.55, clic: false });
    this.esperar(50, () => this.cursor.set({ x: b.left - m.left + b.width * 0.35, y: b.top - m.top + b.height * 0.5, clic: false }));
    this.esperar(800, () => {
      this.cursor.update((c) => c && { ...c, clic: true });
      this.ir(vista);
    });
    this.esperar(1500, () => this.cursor.update((c) => c && { ...c, clic: false }));
  }

  private esperar(ms: number, accion: () => void): void {
    const t = setTimeout(() => {
      this.temporizadores.delete(t);
      accion();
    }, ms);
    this.temporizadores.add(t);
  }
}
