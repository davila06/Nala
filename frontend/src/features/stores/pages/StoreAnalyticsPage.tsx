import { useState } from "react";
import { Helmet } from "react-helmet-async";
import { useStoreAnalytics } from "../hooks/useStores";

function fmt(n: number) {
  return `₡${n.toLocaleString("es-CR")}`;
}

export default function StoreAnalyticsPage() {
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);

  const { data, isLoading, error } = useStoreAnalytics(year, month);

  const monthNames = [
    "Enero",
    "Febrero",
    "Marzo",
    "Abril",
    "Mayo",
    "Junio",
    "Julio",
    "Agosto",
    "Setiembre",
    "Octubre",
    "Noviembre",
    "Diciembre",
  ];

  const prevMonth = () => {
    if (month === 1) {
      setMonth(12);
      setYear((y) => y - 1);
    } else setMonth((m) => m - 1);
  };
  const nextMonth = () => {
    if (month === 12) {
      setMonth(1);
      setYear((y) => y + 1);
    } else setMonth((m) => m + 1);
  };

  return (
    <div className="mx-auto max-w-2xl px-4 py-8 space-y-6 animate-fade-in-up">
      <Helmet>
        <title>Analíticas — PawTrack CR</title>
      </Helmet>

      <div className="flex items-center justify-between gap-4">
        <h1 className="font-display text-xl font-bold text-sand-900">
          📊 Analíticas
        </h1>
        <div className="flex items-center gap-2">
          <button
            onClick={prevMonth}
            className="rounded-lg border border-sand-200 px-2 py-1 text-sm text-sand-600 hover:bg-sand-50"
          >
            ←
          </button>
          <span className="text-sm font-semibold text-sand-800 w-32 text-center">
            {monthNames[month - 1]} {year}
          </span>
          <button
            onClick={nextMonth}
            className="rounded-lg border border-sand-200 px-2 py-1 text-sm text-sand-600 hover:bg-sand-50"
          >
            →
          </button>
        </div>
      </div>

      {isLoading && (
        <div className="grid grid-cols-2 gap-3">
          {[...Array(4)].map((_, i) => (
            <div
              key={i}
              className="h-20 animate-pulse rounded-2xl bg-sand-100"
            />
          ))}
        </div>
      )}

      {error && (
        <div className="rounded-2xl border border-warn-200 bg-warn-50 p-6 text-center">
          <p className="font-semibold text-warn-700">Plan insuficiente</p>
          <p className="mt-1 text-sm text-warn-600">
            Las estadísticas de ventas requieren el plan Tienda Plus o superior.
          </p>
        </div>
      )}

      {data && (
        <>
          {/* Totals */}
          <div className="grid grid-cols-2 gap-3">
            {[
              {
                label: "Ingresos",
                value: fmt(data.totalRevenueCrc),
                icon: "💰",
                color: "text-rescue-700",
              },
              {
                label: "Ticket promedio",
                value:
                  data.deliveredOrders > 0
                    ? fmt(data.averageOrderValueCrc)
                    : "—",
                icon: "🎫",
                color: "text-brand-700",
              },
              {
                label: "Pedidos totales",
                value: String(data.totalOrders),
                icon: "📦",
                color: "text-trust-700",
              },
              {
                label: "Entregados",
                value: `${data.deliveredOrders} / ${data.totalOrders}`,
                icon: "✅",
                color: "text-sand-700",
              },
            ].map((s) => (
              <div
                key={s.label}
                className="rounded-2xl border border-sand-100 bg-surface p-4"
              >
                <p className="text-2xl" aria-hidden="true">
                  {s.icon}
                </p>
                <p className={`text-xl font-black ${s.color}`}>{s.value}</p>
                <p className="text-xs text-sand-500 mt-0.5">{s.label}</p>
              </div>
            ))}
          </div>

          {/* Daily breakdown — Partner only */}
          {data.byDay && data.byDay.length > 0 && (
            <div>
              <h2 className="mb-3 font-semibold text-sand-800">
                Ingresos por día
              </h2>
              <div className="space-y-1">
                {data.byDay.map((d) => {
                  const pct =
                    data.totalRevenueCrc > 0
                      ? (d.revenueCrc / data.totalRevenueCrc) * 100
                      : 0;
                  return (
                    <div
                      key={d.day}
                      className="flex items-center gap-3 text-sm"
                    >
                      <span className="w-24 shrink-0 text-sand-500">
                        {d.day.slice(5)}
                      </span>
                      <div className="flex-1 rounded-full bg-sand-100 h-2">
                        <div
                          className="h-2 rounded-full bg-brand-400 transition-all"
                          style={{ width: `${pct}%` }}
                        />
                      </div>
                      <span className="w-24 shrink-0 text-right font-semibold text-sand-800">
                        {fmt(d.revenueCrc)}
                      </span>
                      <span className="w-10 shrink-0 text-right text-sand-400">
                        {d.orderCount}p
                      </span>
                    </div>
                  );
                })}
              </div>
            </div>
          )}

          {/* Top products — Partner only */}
          {data.topProducts && data.topProducts.length > 0 && (
            <div>
              <h2 className="mb-3 font-semibold text-sand-800">
                Top productos
              </h2>
              <ol className="space-y-2">
                {data.topProducts.map((p, i) => (
                  <li
                    key={p.productId}
                    className="flex items-center gap-3 rounded-xl border border-sand-100 bg-surface px-4 py-3"
                  >
                    <span className="text-lg font-black text-sand-300">
                      #{i + 1}
                    </span>
                    <div className="flex-1 min-w-0">
                      <p className="truncate font-semibold text-sand-900">
                        {p.productName}
                      </p>
                      <p className="text-xs text-sand-500">
                        {p.quantitySold} unidades
                      </p>
                    </div>
                    <span className="shrink-0 font-bold text-rescue-700">
                      {fmt(p.revenueCrc)}
                    </span>
                  </li>
                ))}
              </ol>
            </div>
          )}

          {/* Upsell nudge for Plus users without Partner breakdown */}
          {!data.byDay && (
            <div className="rounded-2xl border border-sand-200 bg-sand-50 p-5 text-center">
              <p className="text-sm font-semibold text-sand-700">
                📈 Desglose diario y top productos disponibles con Tienda
                Partner
              </p>
              <p className="mt-1 text-xs text-sand-500">
                Actualiza tu plan para ver el análisis completo.
              </p>
            </div>
          )}
        </>
      )}
    </div>
  );
}
