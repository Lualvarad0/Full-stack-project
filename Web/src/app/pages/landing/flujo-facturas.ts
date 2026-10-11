import { ChangeDetectionStrategy, Component } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { LucideChartPie, LucideFileText, LucideHandCoins, LucideMail, LucideScale, LucideShieldCheck, LucideCalculator } from '@lucide/angular';

type Tono = 'gasto' | 'ingreso' | 'sri';
type Icono = 'correo' | 'factura' | 'sri' | 'gastos' | 'ingresos' | 'tope' | 'iva';

interface Nodo {
  icono: Icono;
  titulo: string;
  detalle: string;
  tono: Tono;
  x: number;
  y: number;
}

interface Cable {
  d: string;
  tono: Tono;
  /** Segundo del ciclo en el que el punto de luz sale por este cable. */
  inicio: number;
}

interface Diagrama {
  ancho: number;
  alto: number;
  centro: { x: number; y: number };
  fuentes: Nodo[];
  salidas: Nodo[];
  cables: Cable[];
}

/** Duración del ciclo completo: los puntos entran, el nudo se ilumina y salen hacia los resultados. */
const CICLO = 4.5;

const FUENTES = [
  { icono: 'correo', titulo: 'Correo reenviado', detalle: 'Supermaxi · factura.xml', tono: 'gasto' },
  { icono: 'factura', titulo: 'Factura que emitiste', detalle: 'Constructora del Pacífico', tono: 'ingreso' },
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
function diagrama(horizontal: boolean): Diagrama {
  const ancho = horizontal ? 960 : 360;
  const alto = horizontal ? 400 : 720;
  const centro = horizontal ? { x: 480, y: 200 } : { x: 180, y: 330 };
  const fuentes: Nodo[] = FUENTES.map((f, i) =>
    horizontal ? { ...f, x: 125, y: 80 + i * 120 } : { ...f, x: 150, y: 40 + i * 70 },
  );
  const salidas: Nodo[] = SALIDAS.map((s, i) =>
    horizontal ? { ...s, x: 835, y: 55 + i * 97 } : { ...s, x: i % 2 ? 268 : 92, y: 520 + Math.floor(i / 2) * 150 },
  );

  const cables: Cable[] = [
    ...fuentes.map((f, i) => ({
      d: horizontal ? curva(f.x + 105, f.y, centro.x - 70, centro.y, true) : `M272,${f.y} C335,${f.y} 335,${centro.y - 90} ${centro.x + 20 + i * 12},${centro.y - 68}`,
      tono: f.tono,
      inicio: i * 0.35,
    })),
    ...salidas.map((s, i) => ({
      d: horizontal ? curva(centro.x + 70, centro.y, s.x - 105, s.y, true) : curva(centro.x + (i % 2 ? 24 : -24), centro.y + 70, s.x, s.y - 40, false),
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
  imports: [NgTemplateOutlet, LucideMail, LucideFileText, LucideShieldCheck, LucideChartPie, LucideHandCoins, LucideScale, LucideCalculator],
  template: `
    <div class="hidden lg:block"><ng-container *ngTemplateOutlet="vista; context: { $implicit: horizontal }" /></div>
    <div class="mx-auto max-w-sm lg:hidden"><ng-container *ngTemplateOutlet="vista; context: { $implicit: vertical }" /></div>

    <ng-template #vista let-g>
      <div class="relative w-full" [style.aspect-ratio]="g.ancho + ' / ' + g.alto">
        <svg class="absolute inset-0 size-full overflow-visible" [attr.viewBox]="'0 0 ' + g.ancho + ' ' + g.alto" aria-hidden="true">
          <defs>
            <filter [attr.id]="'brillo-' + g.ancho" x="-50%" y="-50%" width="200%" height="200%">
              <feGaussianBlur stdDeviation="3" />
            </filter>
          </defs>

          @for (c of g.cables; track $index) {
            <!-- Hilo base y su "corriente" de guiones que avanza -->
            <path [attr.d]="c.d" fill="none" stroke="rgb(255 255 255 / 0.1)" stroke-width="1.5" />
            <path [attr.d]="c.d" fill="none" [attr.stroke]="color(c.tono)" stroke-opacity="0.45" stroke-width="1.5"
              stroke-dasharray="3 9" class="animate-corriente" />
            <!-- Punto de luz que recorre el hilo una vez por ciclo -->
            <g opacity="0" class="motion-reduce:hidden">
              <circle r="7" [attr.fill]="color(c.tono)" [attr.filter]="'url(#brillo-' + g.ancho + ')'" />
              <circle r="3.5" [attr.fill]="color(c.tono)" />
              <animateMotion [attr.path]="c.d" [attr.dur]="ciclo + 's'" [attr.begin]="c.inicio + 's'" repeatCount="indefinite"
                keyPoints="0;1;1" keyTimes="0;0.3;1" calcMode="linear" />
              <animate attributeName="opacity" [attr.dur]="ciclo + 's'" [attr.begin]="c.inicio + 's'" repeatCount="indefinite"
                values="0;1;1;0;0" keyTimes="0;0.04;0.27;0.31;1" />
            </g>
          }

          <!-- Khipu central: cordón, tres hilos y nudos que se encienden al verificar -->
          <g [attr.transform]="'translate(' + g.centro.x + ' ' + g.centro.y + ')'">
            <circle r="70" fill="rgb(100 103 241 / 0.12)">
              <animate attributeName="r" values="62;78;62" [attr.dur]="ciclo + 's'" begin="1.2s" repeatCount="indefinite" />
              <animate attributeName="fill-opacity" values="1;0.3;1" [attr.dur]="ciclo + 's'" begin="1.2s" repeatCount="indefinite" />
            </circle>
            <circle r="56" fill="var(--ink)" stroke="rgb(255 255 255 / 0.18)" stroke-width="1.5" />
            <g transform="translate(-30 -30) scale(1.58)">
              <path d="M8.5 12.6 Q19 9.6 29.5 12.6" stroke="#fff" stroke-width="1.8" fill="none" stroke-linecap="round" />
              <path d="M13 12 V25 M19 11.2 V29.5 M25 12 V22.5" stroke="#fff" stroke-width="1.4" fill="none" stroke-linecap="round" />
              @for (n of nudos; track $index) {
                <circle [attr.cx]="n.x" [attr.cy]="n.y" [attr.r]="n.r" fill="#fff">
                  <animate attributeName="fill" [attr.values]="'#fff;' + n.color + ';#fff'" [attr.dur]="ciclo + 's'"
                    [attr.begin]="n.inicio + 's'" keyTimes="0;0.1;0.5" repeatCount="indefinite" calcMode="spline"
                    keySplines="0.4 0 0.2 1;0.4 0 0.2 1" />
                </circle>
              }
              <circle cx="19" cy="23" r="2.6" fill="#6467f1" />
            </g>
          </g>
        </svg>

        <!-- Insignia del centro -->
        <span class="absolute flex -translate-x-1/2 items-center gap-1 rounded-full border border-white/15 bg-ink px-2.5 py-1 text-[11px] font-medium whitespace-nowrap text-white"
          [style.left.%]="(g.centro.x / g.ancho) * 100" [style.top.%]="((g.centro.y + 82) / g.alto) * 100">
          <svg lucideShieldCheck class="size-3.5 text-success" aria-hidden="true"></svg> Verificado con el SRI
        </span>

        @for (n of g.fuentes; track n.titulo; let i = $index) {
          <ng-container *ngTemplateOutlet="tarjeta; context: { $implicit: n, g: g, retraso: i * 0.35, compacta: g === vertical }" />
        }
        @for (n of g.salidas; track n.titulo; let i = $index) {
          <ng-container *ngTemplateOutlet="tarjeta; context: { $implicit: n, g: g, retraso: 2.1 + i * 0.25 + ciclo * 0.3, compacta: false }" />
        }
      </div>
    </ng-template>

    <ng-template #tarjeta let-n let-g="g" let-retraso="retraso" let-compacta="compacta">
      <div class="absolute flex -translate-1/2 items-center gap-2.5 rounded-xl border border-white/10 bg-white/[0.06] p-2.5 lg:w-52 lg:p-3"
        [class]="compacta ? 'w-[230px]' : 'w-[160px]'"
        [style.left.%]="(n.x / g.ancho) * 100" [style.top.%]="(n.y / g.alto) * 100">
        <!-- Destello cuando llega o sale el punto de luz -->
        <span class="pointer-events-none absolute inset-0 rounded-xl opacity-0 ring-1 animate-destello"
          [class]="anillo(n.tono)" [style.animation-delay.s]="retraso"></span>
        <span class="grid size-8 shrink-0 place-items-center rounded-lg" [class]="fondoIcono(n.tono)">
          @switch (n.icono) {
            @case ('correo') { <svg lucideMail class="size-4" aria-hidden="true"></svg> }
            @case ('factura') { <svg lucideFileText class="size-4" aria-hidden="true"></svg> }
            @case ('sri') { <svg lucideShieldCheck class="size-4" aria-hidden="true"></svg> }
            @case ('gastos') { <svg lucideChartPie class="size-4" aria-hidden="true"></svg> }
            @case ('ingresos') { <svg lucideHandCoins class="size-4" aria-hidden="true"></svg> }
            @case ('tope') { <svg lucideScale class="size-4" aria-hidden="true"></svg> }
            @case ('iva') { <svg lucideCalculator class="size-4" aria-hidden="true"></svg> }
          }
        </span>
        <span class="min-w-0 leading-tight">
          <span class="block text-xs font-medium text-white lg:truncate lg:text-sm">{{ n.titulo }}</span>
          <span class="block text-[11px] text-white/55 lg:truncate lg:text-xs" [class.text-success]="n.tono === 'ingreso' && n.icono === 'ingresos'">{{ n.detalle }}</span>
        </span>
      </div>
    </ng-template>
  `,
})
export class FlujoFacturas {
  protected readonly ciclo = CICLO;
  protected readonly horizontal = diagrama(true);
  protected readonly vertical = diagrama(false);

  /** Nudos del logo: cada uno se enciende cuando llega el punto de su hilo. */
  protected readonly nudos = [
    { x: 13, y: 19, r: 1.9, color: '#ffffff', inicio: 1.3 },
    { x: 19, y: 16, r: 1.5, color: '#2bd4a0', inicio: 1.65 },
    { x: 25, y: 17.5, r: 1.9, color: '#a5a8ff', inicio: 2.0 },
  ];

  protected color(tono: Tono): string {
    return tono === 'ingreso' ? 'var(--success)' : tono === 'sri' ? 'var(--brand)' : '#ffffff';
  }

  protected anillo(tono: Tono): string {
    return tono === 'ingreso' ? 'ring-success bg-success/10' : tono === 'sri' ? 'ring-brand bg-brand/15' : 'ring-white/60 bg-white/10';
  }

  protected fondoIcono(tono: Tono): string {
    return tono === 'ingreso' ? 'bg-success/15 text-success' : tono === 'sri' ? 'bg-brand/25 text-[#a5a8ff]' : 'bg-white/10 text-white';
  }
}
