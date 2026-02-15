import { useParams } from "react-router-dom";

export default function PagoResultado() {
  const { resultado } = useParams();

  return (
    <main style={{ maxWidth: 960, margin: "40px auto", padding: "0 16px" }}>
      <h2>Resultado del pago</h2>
      <p>Estado: {resultado}</p>
    </main>
  );
}
