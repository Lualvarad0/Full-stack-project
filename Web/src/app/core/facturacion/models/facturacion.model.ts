// Facturación electrónica: firma, datos de emisión, facturas y servicios (contratos de /api/facturacion).
export type AmbienteSri = 'Pruebas' | 'Produccion';

export type EstadoFactura =
  'Firmada' | 'Recibida' | 'EnProceso' | 'Devuelta' | 'Autorizada' | 'NoAutorizada' | 'Anulada';

export type TarifaIva = 'Iva15' | 'Iva5' | 'Iva0' | 'NoObjeto' | 'Exento';

export interface FirmaElectronica {
  titular: string;
  identificacion: string | null;
  emisor: string;
  numeroSerie: string;
  validaDesde: string;
  validaHasta: string;
  subidaEn: string;
  vigente: boolean;
  diasRestantes: number;
  coincideConPerfil: boolean | null;
}

export interface DatosEmisor {
  agenteRetencion?: boolean;
  ambiente: AmbienteSri;
  establecimiento: string;
  puntoEmision: string;
  siguienteSecuencial: number;
  nombreComercial: string | null;
  direccionMatriz: string;
  direccionEstablecimiento: string | null;
  obligadoContabilidad: boolean;
  contribuyenteEspecial: string | null;
  /** Lo que falta para poder facturar; vacío si está todo listo. */
  pendientes: string[];
}

export type DatosEmisorRequest = Omit<DatosEmisor, 'pendientes'>;

export interface LineaFactura {
  codigo?: string | null;
  descripcion: string;
  cantidad: number;
  precioUnitario: number;
  descuento: number;
  tarifa: TarifaIva;
}

export interface EmitirFacturaRequest {
  tipoIdentificacion: string;
  identificacion: string;
  razonSocial: string;
  correo: string | null;
  direccion: string | null;
  formaPago: string;
  detalles: LineaFactura[];
}

export interface FacturaEmitida {
  tipoDocumento?: '01' | '04' | '07';
  documentoSustentoId?: number | null;
  comprobanteSustentoId?: number | null;
  numeroDocumentoSustento?: string | null;
  motivo?: string | null;
  retenciones?: LineaRetencion[] | null;
  id: number;
  numero: string;
  claveAcceso: string;
  ambiente: AmbienteSri;
  fechaEmision: string;
  tipoIdentificacionComprador: string;
  identificacionComprador: string;
  razonSocialComprador: string;
  correoComprador: string | null;
  direccionComprador: string | null;
  formaPago: string;
  subtotal: number;
  descuento: number;
  iva: number;
  total: number;
  estado: EstadoFactura;
  mensajes: string | null;
  numeroAutorizacion: string | null;
  fechaAutorizacion: string | null;
  detalles: (LineaFactura & { id?: number; subtotal: number; iva: number })[] | null;
}
export interface LineaRetencion { codigoImpuesto: string; codigoRetencion: string; baseImponible: number; porcentaje: number; valor: number; }

export interface ServicioFacturable {
  id: number;
  codigo: string | null;
  descripcion: string;
  precioUnitario: number;
  tarifa: TarifaIva;
}

export type ServicioRequest = Omit<ServicioFacturable, 'id'>;

export interface LineaEditable {
  clave: number;
  codigo: string;
  descripcion: string;
  cantidad: number;
  precioUnitario: number;
  descuento: number;
  tarifa: TarifaIva;
}

export interface ServicioBorrador {
  id?: number;
  codigo: string;
  descripcion: string;
  precioUnitario: number;
  tarifa: TarifaIva;
}
