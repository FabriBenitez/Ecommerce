import "./ProductCard.css";

export default function ProductCard({ producto, onAgregar }) {
  const { id, nombre, descripcion, imagenUrl, precio, stock } = producto;

  const agotado = stock <= 0;
  const apiBase = (import.meta.env.VITE_API_BASE_URL || "https://localhost:7248").replace(/\/$/, "");
  const normalizeImageUrl = (raw) => {
    let value = (raw ?? "").trim();
    if (!value) return "";
    value = value.replace(/^"+|"+$/g, "");
    if (/^data:image\//i.test(value)) {
      const headers = [...value.matchAll(/data:image\/[a-z0-9.+-]+;base64,/ig)];
      if (headers.length) {
        const lastHeader = headers[headers.length - 1];
        const prefix = lastHeader[0];
        let payload = value.slice((lastHeader.index ?? 0) + prefix.length);
        payload = payload.replace(/["'\s]/g, "");
        payload = payload.replace(/[^A-Za-z0-9+/=]/g, "");
        if (!payload) return "";
        const mod = payload.length % 4;
        if (mod !== 0) payload = payload + "=".repeat(4 - mod);
        value = `${prefix}${payload}`;
      }
    }
    return value;
  };
  const normalizedImageUrl = normalizeImageUrl(imagenUrl);
  const imageSrc = normalizedImageUrl
    ? (
      normalizedImageUrl.startsWith("http://")
      || normalizedImageUrl.startsWith("https://")
      || normalizedImageUrl.startsWith("data:")
      || normalizedImageUrl.startsWith("blob:")
    )
      ? normalizedImageUrl
      : `${apiBase}${normalizedImageUrl.startsWith("/") ? "" : "/"}${normalizedImageUrl}`
    : null;

  return (
    <article className="productCard">
      <div className="productCard__imageWrap">
        {imageSrc ? (
          <img className="productCard__img" src={imageSrc} alt={nombre} loading="lazy" />
        ) : (
          <div className="productCard__imgFallback">Sin imagen</div>
        )}

        {agotado ? <span className="productCard__badge">Sin stock</span> : null}
      </div>

      <div className="productCard__body">
        <h3 className="productCard__title" title={nombre}>
          {nombre}
        </h3>

        {descripcion ? (
          <p className="productCard__desc" title={descripcion}>
            {descripcion}
          </p>
        ) : (
          <p className="productCard__desc productCard__desc--muted">—</p>
        )}

        <div className="productCard__footer">
          <div className="productCard__price">
            ${Number(precio).toFixed(2)}
            <span className="productCard__currency"> ARS</span>
          </div>

          <button
            className="productCard__btn"
            disabled={agotado}
            onClick={() => onAgregar?.(id)}
            type="button"
          >
            {agotado ? "Agotado" : "Agregar"}
          </button>
        </div>
      </div>
    </article>
  );
}
