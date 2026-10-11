/**
 * Cloudflare Email Worker de Nudo: recibe cualquier correo enviado a <codigo>@<tu dominio de facturas>
 * (Email Routing con una regla "catch-all" → este Worker) y lo entrega a la API como MIME crudo.
 *
 * Variables del Worker (Settings → Variables):
 *   NUDO_WEBHOOK_URL     https://api.tudominio.com/api/correo/entrante
 *   NUDO_WEBHOOK_SECRET  el mismo valor que Correo:SecretoWebhook en la API (como "secret")
 */
export default {
  async email(message, env) {
    const crudo = await new Response(message.raw).arrayBuffer();
    const url = `${env.NUDO_WEBHOOK_URL}?para=${encodeURIComponent(message.to)}`;

    const respuesta = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'message/rfc822',
        'X-Nudo-Webhook-Secret': env.NUDO_WEBHOOK_SECRET,
      },
      body: crudo,
    });

    // Si la API no lo acepta, el remitente recibe un rebote en lugar de perder el correo en silencio.
    if (!respuesta.ok) message.setReject(`Nudo no pudo recibir el correo (${respuesta.status}).`);
  },
};
