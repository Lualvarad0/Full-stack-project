import { ChangeDetectionStrategy, Component } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import {
  LucideCalculator,
  LucideChartPie,
  LucideFileText,
  LucideHandCoins,
  LucideMail,
  LucideScale,
  LucideShieldCheck,
} from '@lucide/angular';
import {
  CableFlujo,
  DiagramaFlujo,
  NodoFlujo,
  TonoFlujo,
} from '@features/landing/models/landing.model';
/** Duración del ciclo completo: los puntos entran, el nudo se ilumina y salen hacia los resultados. */
const CICLO = 4.5;

const FUENTES = [
  {
    icono: 'correo',
    titulo: 'Correo reenviado',
    detalle: 'Supermaxi · factura.xml',
    tono: 'gasto',
  },
  {
    icono: 'factura',
    titulo: 'Factura que emitiste',
    detalle: 'Constructora del Pacífico',
    tono: 'ingreso',
  },
  { icono: 'sri', titulo: 'Reporte del SRI', detalle: 'Comprobantes del mes .txt', tono: 'sri' },
] as const;

const SALIDAS = [
  { icono: 'gastos', titulo: 'Gastos por categoría', detalle: '$2.476,16', tono: 'gasto' },
  { icono: 'ingresos', titulo: 'Ingresos y clientes', detalle: '$12.480,50', tono: 'ingreso' },
  { icono: 'tope', titulo: 'Tope deducible', detalle: '60 % usado', tono: 'sri' },
  { icono: 'iva', titulo: 'IVA del mes', detalle: 'Cobrado vs. pagado', tono: 'sri' },
] as const;

/** Curva suave entre dos puntos, horizontal o vertical según la orientación. */
function curva(x1: number, y1: number, x2: number, y2: number, horizontal: boolean): string {
  if (horizontal) {
    const m = (x1 + x2) / 2;
    return `M${x1},${y1} C${m},${y1} ${m},${y2} ${x2},${y2}`;
  }
  const m = (y1 + y2) / 2;
  return `M${x1},${y1} C${x1},${m} ${x2},${m} ${x2},${y2}`;
}

/** Arma el diagrama con las mismas piezas en horizontal (escritorio) o vertical (celular). */
function diagrama(horizontal: boolean): DiagramaFlujo {
  const ancho = horizontal ? 960 : 360;
  const alto = horizontal ? 400 : 720;
  const centro = horizontal ? { x: 480, y: 200 } : { x: 180, y: 330 };
  const fuentes: NodoFlujo[] = FUENTES.map((f, i) =>
    horizontal ? { ...f, x: 125, y: 80 + i * 120 } : { ...f, x: 150, y: 40 + i * 70 },
  );
  const salidas: NodoFlujo[] = SALIDAS.map((s, i) =>
    horizontal
      ? { ...s, x: 835, y: 55 + i * 97 }
      : { ...s, x: i % 2 ? 268 : 92, y: 520 + Math.floor(i / 2) * 150 },
  );

  const cables: CableFlujo[] = [
    ...fuentes.map((f, i) => ({
      d: horizontal
        ? curva(f.x + 105, f.y, centro.x - 70, centro.y, true)
        : `M272,${f.y} C335,${f.y} 335,${centro.y - 90} ${centro.x + 20 + i * 12},${centro.y - 68}`,
      tono: f.tono,
      inicio: i * 0.35,
    })),
    ...salidas.map((s, i) => ({
      d: horizontal
        ? curva(centro.x + 70, centro.y, s.x - 105, s.y, true)
        : curva(centro.x + (i % 2 ? 24 : -24), centro.y + 70, s.x, s.y - 40, false),
      tono: s.tono,
      inicio: 2.1 + i * 0.25,
    })),
  ];
  return { ancho, alto, centro, fuentes, salidas, cables };
}

/**
 * Animación de la landing: las facturas viajan como puntos de luz por los hilos hasta el khipu
 * de Nudo, sus nudos se encienden al verificarlas con el SRI y salen ordenadas a los resultados.
 * Los puntos usan animaciones SVG (animateMotion), que escalan con el diagrama.
 */
@Component({
  selector: 'app-flujo-facturas',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NgTemplateOutlet,
    LucideMail,
    LucideFileText,
    LucideShieldCheck,
    LucideChartPie,
    LucideHandCoins,
    LucideScale,
    LucideCalculator,
  ],
  templateUrl: './flujo-facturas.html',
})
export class FlujoFacturas {
  protected readonly ciclo = CICLO;
  protected readonly horizontal = diagrama(true);
  protected readonly vertical = diagrama(false);

  /** Nudos del logo: cada uno se enciende cuando llega el punto de su hilo. */
  protected readonly nudos = [
    { x: 13, y: 19, r: 1.9, color: 'var(--logo-fg)', inicio: 1.3 },
    { x: 19, y: 16, r: 1.5, color: 'var(--flow-ingreso)', inicio: 1.65 },
    { x: 25, y: 17.5, r: 1.9, color: 'var(--flow-sri)', inicio: 2.0 },
  ];

  protected color(tono: TonoFlujo): string {
    return tono === 'ingreso'
      ? 'var(--success)'
      : tono === 'sri'
        ? 'var(--brand)'
        : 'var(--logo-fg)';
  }

  protected anillo(tono: TonoFlujo): string {
    return tono === 'ingreso'
      ? 'ring-success bg-success/10'
      : tono === 'sri'
        ? 'ring-brand bg-brand/15'
        : 'ring-white/60 bg-white/10';
  }

  protected fondoIcono(tono: TonoFlujo): string {
    return tono === 'ingreso'
      ? 'bg-success/15 text-success'
      : tono === 'sri'
        ? 'bg-brand/25 text-flow-sri'
        : 'bg-white/10 text-white';
  }
}
