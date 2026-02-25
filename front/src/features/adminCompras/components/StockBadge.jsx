import "./StockBadge.css";

export default function StockBadge({ stock, min = 10 }) {
  const n = Number(stock ?? 0);
  const low = n <= min;

  return (
    <span className={`sbadge ${low ? "sbadge--low" : "sbadge--ok"}`}>
      {low ? `Bajo (${n})` : `OK (${n})`}
    </span>
  );
}