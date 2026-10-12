import { Contador } from './contador';
import { BASE, CATEGORIAS, INICIALES, PASOS, POR_LLEGAR, RECORRIDO, TOPE } from './demo-datos';
import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  ElementRef,
  inject,
  signal,
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
import {
  CategoriaDemo,
  LlegadaDemo,
  MovimientoDemo,
  VistaDemo,
} from '@features/landing/models/landing.model';
/**
 * Demo interactiva del panel para la landing: se puede navegar con clics, las facturas
 * "llegan" solas al buzón y, si nadie la toca, un cursor la recorre por su cuenta.
 * Todos los datos son ilustrativos.
 */
@Component({
  selector: 'app-demo-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Contador,
    LucideSearch,
    LucideBell,
    LucideCircleCheck,
    LucideLayoutDashboard,
    LucideReceipt,
    LucideHandCoins,
    LucideInbox,
    LucideMousePointer2,
    LucideLoaderCircle,
  ],
  host: { '(pointerdown)': 'interaccion()', '(keydown)': 'interaccion()' },
  templateUrl: './demo-panel.html',
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
  protected readonly faltaTope = computed(() =>
    Math.max(0, TOPE - Object.values(this.deducibles()).reduce((a, b) => a + b, 0)),
  );
  protected readonly porCategoria = computed(() => {
    const d = this.deducibles();
    const max = Math.max(...Object.values(d));
    return CATEGORIAS.map((nombre) => ({
      nombre,
      texto: MONEDA.format(d[nombre]),
      porcentaje: (d[nombre] / max) * 100,
    }));
  });
  protected readonly clientes = computed(() => {
    const totales = { ...BASE.clientes };
    for (const m of this.movimientos())
      if (m.ingreso) totales[m.nombre] = (totales[m.nombre] ?? 0) + m.monto;
    const max = Math.max(...Object.values(totales));
    return Object.entries(totales)
      .sort((a, b) => b[1] - a[1])
      .map(([nombre, total]) => ({
        nombre,
        texto: MONEDA.format(total),
        porcentaje: (total / max) * 100,
      }));
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
      const observador = new IntersectionObserver(([e]) => (this.visible = e.isIntersecting), {
        threshold: 0.3,
      });
      observador.observe(this.marco().nativeElement);
      const llegadas = setInterval(() => this.activa() && this.llegar(), 4200);
      const recorrido = setInterval(
        () => this.activa() && Date.now() - this.ultimaInteraccion > 12000 && this.recorrer(),
        5600,
      );
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
      this.esperar(paso * 650, () =>
        this.llegadas.update((l) => l.map((x) => (x.id === id ? { ...x, paso } : x))),
      );
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
    const boton = [...marco.querySelectorAll<HTMLElement>(`[data-vista="${vista}"]`)].find(
      (b) => b.offsetParent !== null,
    );
    if (!boton) return;
    const m = marco.getBoundingClientRect();
    const b = boton.getBoundingClientRect();
    if (!this.cursor()) this.cursor.set({ x: m.width * 0.6, y: m.height * 0.55, clic: false });
    this.esperar(50, () =>
      this.cursor.set({
        x: b.left - m.left + b.width * 0.35,
        y: b.top - m.top + b.height * 0.5,
        clic: false,
      }),
    );
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
