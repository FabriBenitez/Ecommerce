import api from './api';

export const crearPreferenciaPago = async (carrito) => {
  try {
    // Mapeamos al formato PedidoItemDto del C#
    const datosParaBackend = carrito.map(item => ({
      productoId: item.id,
      nombreProducto: item.nombre,
      cantidad: item.cantidad,
      precioUnitario: item.precio
    }));

    const response = await api.post('/pedidos/crear-pago', datosParaBackend);
    return response.data.initPoint;
  } catch (error) {
    console.error("Error al crear la preferencia de pago:", error);
    throw error;
  }
};
