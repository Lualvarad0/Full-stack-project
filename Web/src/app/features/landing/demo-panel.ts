import { NgTemplateOutlet } from '@angular/common';
import { Contador } from './contador';
import {
  CUENTAS,
  EVOLUCION,
  INGRESO_MENSUAL,
  INICIALES,
  MES,
  PASOS,
  POR_LLEGAR,
  RECORRIDO,
  TOPE,
} from './demo-datos';
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
  LucideCheck,
  LucideChevronLeft,
  LucideChevronRight,
  LucideCircleCheck,
  LucideCopy,
  LucideFileText,
  LucideFileUp,
  LucideHandCoins,
  LucideInbox,
  LucideLayoutDashboard,
  LucideLoaderCircle,
  LucideMail,
  LucideMoon,
  LucideMousePointer2,
  LucideReceipt,
  LucideScale,
  LucideSearch,
  LucideStore,
  LucideUndo2,
  LucideUsers,
  LucideWallet,
} from '@lucide/angular';
import { categoria, CATEGORIAS, CategoriaGasto } from '@core/comprobantes';
import { CATEGORIAS_CUENTA, CategoriaCuenta } from '@core/cuentas';
import { MONEDA } from '@core/utils';
import { Marca } from '@shared/components';
import { CuentaDemo, LlegadaDemo, MovimientoDemo, VistaDemo } from '@features/landing/models/landing.model';

const IVA = 0.15;

/**
 * Demo interactiva para la landing: reproduce las pantallas reales de la app (Resumen, Mis cuentas,
 * Traer facturas, Ingresos y Gastos) con sus mismos textos, categorías y colores. Las facturas "llegan"
 * solas al buzón y, si nadie la toca, un cursor la recorre por su cuenta. Todos los datos son ilustrativos.
 */
@Component({
  selector: 'app-demo-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NgTemplateOutlet,
    Contador,
    Marca,
    LucideLayoutDashboard,
    LucideWallet,
    LucideFileUp,
    LucideFileText,
    LucideHandCoins,
    LucideUsers,
    LucideReceipt,
    LucideStore,
    LucideChevronLeft,
    LucideChevronRight,
    LucideMoon,
    LucideMail,
    LucideScale,
    LucideInbox,
    LucideCopy,
    LucideSearch,
    LucideCheck,
    LucideUndo2,
    LucideCircleCheck,
    LucideMousePointer2,
    LucideLoaderCircle,
  ],
  host: { '(pointerdown)': 'interaccion()', '(keydown)': 'interaccion()' },
  templateUrl: './demo-panel.html',
})
export class DemoPanel {
  private readonly marco = viewChild.required<ElementRef<HTMLElement>>('marco');
  private readonly contenido = viewChild.required<ElementRef<HTMLElement>>('contenido');

  protected readonly mes = MES;
  protected readonly pasos = PASOS;
  protected readonly tope = TOPE;
  protected readonly ingresoMensual = INGRESO_MENSUAL;
  /** Mismas secciones y nombres que el menú de la app; las que no tienen vista en la demo no son clicables. */
  protected readonly menu: { grupo: string | null; items: { vista: VistaDemo | null; nombre: string; icono: string }[] }[] = [
    {
      grupo: null,
      items: [
        { vista: 'resumen', nombre: 'Resumen', icono: 'resumen' },
        { vista: 'cuentas', nombre: 'Mis cuentas', icono: 'cuentas' },
        { vista: 'importar', nombre: 'Traer facturas', icono: 'importar' },
      ],
    },
    {
      grupo: 'Lo que facturas',
      items: [
        { vista: null, nombre: 'Emitir facturas', icono: 'factura' },
        { vista: 'ingresos', nombre: 'Ingresos', icono: 'ingresos' },
        { vista: null, nombre: 'Clientes', icono: 'clientes' },
      ],
    },
    {
      grupo: 'Gastos con facturas',
      items: [
        { vista: 'gastos', nombre: 'Gastos', icono: 'gastos' },
        { vista: null, nombre: 'Proveedores', icono: 'proveedores' },
      ],
    },
  ];
  protected readonly pestanas: { vista: VistaDemo; nombre: string }[] = [
    { vista: 'resumen', nombre: 'Resumen' },
    { vista: 'cuentas', nombre: 'Mis cuentas' },
    { vista: 'importar', nombre: 'Traer facturas' },
    { vista: 'ingresos', nombre: 'Ingresos' },
    { vista: 'gastos', nombre: 'Gastos' },
  ];
  protected readonly categoriasGasto = CATEGORIAS.filter((c) => c.deducible);

  protected readonly vista = signal<VistaDemo>('resumen');
  protected readonly filtro = signal<CategoriaGasto | ''>('');
  private readonly movimientos = signal<MovimientoDemo[]>(INICIALES);
  protected readonly cuentas = signal<CuentaDemo[]>(CUENTAS);
  protected readonly llegadas = signal<LlegadaDemo[]>([]);
  protected readonly aviso = signal<LlegadaDemo[]>([]);
  protected readonly nuevas = signal(0);
  protected readonly cursor = signal<{ x: number; y: number; clic: boolean } | null>(null);

  // Resumen, ingresos y gastos
  protected readonly ingresos = computed(() => this.movimientos().filter((m) => m.ingreso));
  protected readonly gastos = computed(() => this.movimientos().filter((m) => !m.ingreso));
  protected readonly totalIngresos = computed(() => this.suma(this.ingresos()));
  protected readonly totalGastos = computed(() => this.suma(this.gastos()));
  protected readonly balance = computed(() => this.totalIngresos() - this.totalGastos());
  protected readonly deducible = computed(
    () => this.suma(this.gastos().filter((m) => m.categoria && categoria(m.categoria).deducible)),
  );
  protected readonly facturasEmitidas = computed(() => this.ingresos().length);
  protected readonly comprobantes = computed(() => this.gastos().length);
  protected readonly ivaCobrado = computed(() => this.ivaDe(this.totalIngresos()));
  protected readonly ivaPagado = computed(() => this.ivaDe(this.totalGastos()) * 0.6);
  protected readonly evolucion = computed(() => {
    const puntos = [...EVOLUCION, { mes: 'Oct', ingresos: this.totalIngresos(), gastos: this.totalGastos() }];
    const max = Math.max(...puntos.map((p) => p.ingresos));
    return puntos.map((p, i) => ({
      ...p,
      altoIngresos: (p.ingresos / max) * 100,
      altoGastos: (p.gastos / max) * 100,
      actual: i === puntos.length - 1,
    }));
  });
  protected readonly avanceTope = computed(() =>
    Math.min(100, ((TOPE.deducidoAntes + this.deducible()) / TOPE.tope) * 100),
  );
  protected readonly faltaTope = computed(() => Math.max(0, TOPE.tope - TOPE.deducidoAntes - this.deducible()));
  protected readonly rebaja = computed(() => Math.min(TOPE.tope, TOPE.deducidoAntes + this.deducible()) * 0.18);
  protected readonly porCategoria = computed(() => {
    const totales = new Map<CategoriaGasto, number>();
    for (const m of this.gastos()) if (m.categoria) totales.set(m.categoria, (totales.get(m.categoria) ?? 0) + m.monto);
    const max = Math.max(1, ...totales.values());
    return [...totales.entries()]
      .sort((a, b) => b[1] - a[1])
      .map(([valor, total]) => ({ ...categoria(valor), total, ancho: (total / max) * 100 }));
  });
  protected readonly recientes = computed(() => this.movimientos().slice(0, 5));
  protected readonly gastosFiltrados = computed(() => {
    const f = this.filtro();
    return this.gastos().filter((m) => !f || m.categoria === f);
  });
  protected readonly totalFiltrado = computed(() => this.suma(this.gastosFiltrados()));

  // Mis cuentas
  protected readonly totalCuentas = computed(() => this.cuentas().reduce((t, c) => t + c.monto, 0));
  protected readonly pagado = computed(() =>
    this.cuentas().reduce((t, c) => (c.pagadaEl ? t + c.monto : t), 0),
  );
  protected readonly pagadas = computed(() => this.cuentas().filter((c) => c.pagadaEl).length);
  protected readonly teQueda = computed(() => INGRESO_MENSUAL - this.totalCuentas());
  protected readonly proporcion = computed(() => Math.round((this.totalCuentas() / INGRESO_MENSUAL) * 100));
  protected readonly cuentasOrdenadas = computed(() =>
    [...this.cuentas()].sort((a, b) => Number(!!a.pagadaEl) - Number(!!b.pagadaEl) || a.diaVencimiento - b.diaVencimiento),
  );

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
    // Cada pantalla empieza arriba, como al navegar en la app.
    if (this.vista() !== vista) this.contenido().nativeElement.scrollTop = 0;
    this.vista.set(vista);
    if (vista === 'importar') this.nuevas.set(0);
  }

  /** Cualquier clic o tecla del visitante pausa el recorrido automático y esconde el cursor. */
  protected interaccion(): void {
    this.ultimaInteraccion = Date.now();
    this.cursor.set(null);
  }

  protected dinero(valor: number): string {
    return MONEDA.format(valor);
  }

  protected signo(m: MovimientoDemo): string {
    return (m.ingreso ? '+' : '−') + MONEDA.format(m.monto);
  }

  protected infoCategoria(m: MovimientoDemo) {
    return categoria(m.categoria ?? 'SinCategoria');
  }

  protected etiquetaCuenta(valor: CategoriaCuenta): string {
    return CATEGORIAS_CUENTA.find((c) => c.valor === valor)?.etiqueta ?? valor;
  }

  protected vencida(c: CuentaDemo): boolean {
    return !c.pagadaEl && c.diaVencimiento < MES.hoy;
  }

  protected marcarPagada(c: CuentaDemo, pagada: boolean): void {
    this.cuentas.update((cs) => cs.map((x) => (x.id === c.id ? { ...x, pagadaEl: pagada ? MES.hoy : null } : x)));
  }

  private ivaDe(total: number): number {
    return (total / (1 + IVA)) * IVA;
  }

  private suma(lista: MovimientoDemo[]): number {
    return lista.reduce((t, m) => t + m.monto, 0);
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
      if (this.vista() !== 'importar') this.nuevas.update((n) => n + 1);
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
