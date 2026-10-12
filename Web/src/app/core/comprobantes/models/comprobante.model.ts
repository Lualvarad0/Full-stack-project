// Comprobantes del SRI: importación, gastos, ingresos y resúmenes (contratos de /api/comprobantes).
export interface CategoriaInfo {
  valor: CategoriaGasto;
  nombre: string;
  /** Token CSS del color categórico (orden fijo de la paleta validada). */
  color: string;
  deducible: boolean;
}

/** Recibido = gasto (soy el comprador). Emitido = ingreso (soy el emisor). */
export type Direccion = 'Recibido' | 'Emitido';
export type CategoriaIngreso = 'Ventas' | 'Servicios' | 'OtrosIngresos';
export type FinalidadComprobante = 'SinDefinir' | 'Personal' | 'Negocio' | 'NoAplicable' | 'Mixto';
export type TratamientoIva = 'Pendiente' | 'ConCredito' | 'SinCredito';
export interface SugerenciaClasificacion {
  categoria: CategoriaGasto | null;
  categoriaIngreso: CategoriaIngreso | null;
  estado: 'Pendiente' | 'Confirmada' | 'SinEvidencia';
  motivo: string;
  versionReglas: string;
}

export type CategoriaGasto =
  | 'SinCategoria'
  | 'Vivienda'
  | 'Educacion'
  | 'Salud'
  | 'Alimentacion'
  | 'Vestimenta'
  | 'Turismo'
  | 'NoDeducible';

export type ResultadoImportacion =
  | 'Importado'
  | 'YaExistia'
  | 'NoAutorizado'
  | 'NoEncontrado'
  | 'NoSoportado'
  | 'Error'
  | 'LimiteDelPlan';

export interface ClaveImportada {
  claveAcceso: string;
  resultado: ResultadoImportacion;
  detalle: string | null;
  direccion: Direccion | null;
}

export interface ImportacionResponse {
  clavesEncontradas: number;
  importados: number;
  yaExistian: number;
  conProblemas: number;
  ingresos: number;
  perfilCompleto: boolean;
  claves: ClaveImportada[];
}

export interface ComprobanteLista {
  id: number;
  claveAcceso: string;
  direccion: Direccion;
  tipoComprobante: string;
  numeroComprobante: string;
  fechaEmision: string;
  rucEmisor: string;
  razonSocialEmisor: string;
  nombreComercial: string | null;
  identificacionComprador: string;
  razonSocialComprador: string;
  subtotal: number;
  iva: number;
  total: number;
  categoria: CategoriaGasto;
  categoriaIngreso?: CategoriaIngreso | null;
  /** Solo gastos: si la factura está a tu nombre. Null si tu perfil no tiene cédula/RUC. */
  aNombreDelUsuario: boolean | null;
}

export interface ComprobanteDetalle {
  id?: number;
  categoria?: CategoriaGasto | null;
  categoriaIngreso?: CategoriaIngreso | null;
  finalidad?: FinalidadComprobante;
  tratamientoIva?: TratamientoIva;
  iva?: number | null;
  tarifaIva?: number | null;
  revisadoEn?: string | null;
  sugerencia?: SugerenciaClasificacion | null;
  codigo: string | null;
  descripcion: string;
  cantidad: number;
  precioUnitario: number;
  descuento: number;
  subtotal: number;
}

export interface Comprobante extends ComprobanteLista {
  finalidad?: FinalidadComprobante;
  tratamientoIva?: TratamientoIva;
  revisadoEn?: string | null;
  sugerencia?: SugerenciaClasificacion | null;
  numeroAutorizacion: string;
  fechaAutorizacion: string | null;
  baseIva0: number;
  baseIvaGravada: number;
  otrosImpuestos: number;
  descuento: number;
  propina: number;
  documentoModificado: string | null;
  detalles: ComprobanteDetalle[];
}

export interface Proveedor {
  ruc: string;
  razonSocial: string;
  categoria: CategoriaGasto | null;
  comprobantes: number;
  total: number;
}

export interface ClienteFacturado {
  identificacion: string;
  razonSocial: string;
  comprobantes: number;
  total: number;
  iva: number;
  ultimaFactura: string;
}

export interface TotalCategoria {
  categoria: CategoriaGasto;
  comprobantes: number;
  total: number;
}

export interface TotalTercero {
  identificacion: string;
  razonSocial: string;
  comprobantes: number;
  total: number;
}

export interface TotalMes {
  mes: number;
  comprobantes: number;
  total: number;
  iva: number;
  ingresos: number;
  ivaCobrado: number;
}

/** Gastos (total, iva, …) e ingresos del período; netos de notas de crédito. */
export interface ResumenPeriodo {
  anio: number;
  mes: number | null;
  comprobantes: number;
  total: number;
  iva: number;
  totalDeducible: number;
  sinCategoria: number;
  aNombreDeOtros: number;
  comprobantesEmitidos: number;
  ingresos: number;
  ivaCobrado: number;
  porCategoria: TotalCategoria[];
  principalesProveedores: TotalTercero[];
  principalesClientes: TotalTercero[];
  porMes: TotalMes[] | null;
}

export interface FiltroComprobantes {
  direccion?: Direccion;
  anio?: number;
  mes?: number;
  categoria?: CategoriaGasto | '';
  ruc?: string;
  q?: string;
  desde?: string;
  hasta?: string;
  min?: number;
  max?: number;
  cliente?: string;
}

export type ModoImportacion = 'archivos' | 'claves';

export interface VariacionMensual {
  porcentaje: number;
  sube: boolean;
  mesAnterior: string;
}
