import "./StockBadge.css";

export default function StockBadge({ stock, min = 10 }) {
  const n = Number(stock ?? 0);
  const limite = Number(min ?? 10);

  let variant = "ok";
  let label = "Stock estable";

  if (n <= 0) {
    variant = "out";
    label = "Sin stock";
  } else if (n <= limite) {
    variant = "low";
    label = "Reponer pronto";
  } else if (n > limite * 2) {
    variant = "high";
    label = "Stock optimo";
  }

  return (
    <span className={`sbadge sbadge--${variant}`}>
      <span className="sbadge__dot" aria-hidden="true" />
      {label}
    </span>
  );
}
