/**
 * PawTrack CR — Embeddable widget for Clínica Partner customers.
 *
 * Usage:
 *   <div id="pawtrack-widget" data-clinic-id="CLINIC_UUID"></div>
 *   <script src="https://pawtrack.cr/widget.js"></script>
 *
 * The widget is a standalone IIFE with zero external dependencies.
 * It renders a pet-search input inside the host element.
 */

const API_BASE = "https://pawtrack.cr/api";

function createWidget(host: HTMLElement, clinicId: string) {
  const shadow = host.attachShadow({ mode: "open" });

  shadow.innerHTML = `
    <style>
      * { box-sizing: border-box; font-family: system-ui, sans-serif; }
      .pw-wrap { border: 1px solid #e2d3c4; border-radius: 16px; padding: 16px; background: #fdfcf9; }
      .pw-brand { font-size: 11px; font-weight: 700; letter-spacing: .12em; text-transform: uppercase; color: #e8521e; margin-bottom: 10px; }
      .pw-row { display: flex; gap: 8px; }
      .pw-input { flex: 1; border: 1px solid #e2d3c4; border-radius: 10px; padding: 8px 12px; font-size: 13px; outline: none; }
      .pw-input:focus { border-color: #e8521e; box-shadow: 0 0 0 2px rgba(232,82,30,.15); }
      .pw-btn { border: none; background: #e8521e; color: #fff; border-radius: 10px; padding: 8px 14px; font-size: 13px; font-weight: 700; cursor: pointer; white-space: nowrap; }
      .pw-btn:hover { background: #c43f10; }
      .pw-result { margin-top: 12px; border-radius: 12px; padding: 12px; background: #f9f5ef; display: none; }
      .pw-result.show { display: block; }
      .pw-result img { width: 56px; height: 56px; border-radius: 50%; object-fit: cover; margin-right: 10px; float: left; }
      .pw-name { font-weight: 700; font-size: 14px; color: #352823; }
      .pw-meta { font-size: 12px; color: #6e5244; margin-top: 2px; }
      .pw-err { color: #c43f10; font-size: 13px; margin-top: 10px; display: none; }
      .pw-err.show { display: block; }
      .pw-footer { font-size: 10px; color: #6e5244; text-align: right; margin-top: 8px; }
      .pw-footer a { color: #e8521e; text-decoration: none; font-weight: 600; }
    </style>
    <div class="pw-wrap">
      <div class="pw-brand">🐾 PawTrack CR</div>
      <div class="pw-row">
        <input class="pw-input" id="pw-input" placeholder="N° microchip o URL del QR…" autocomplete="off" />
        <button class="pw-btn" id="pw-search">Buscar</button>
      </div>
      <div class="pw-result" id="pw-result"></div>
      <div class="pw-err" id="pw-err">No se encontró ninguna mascota con ese identificador.</div>
      <div class="pw-footer">Verificación de mascotas · <a href="https://pawtrack.cr" target="_blank">pawtrack.cr</a></div>
    </div>
  `;

  const input = shadow.getElementById("pw-input") as HTMLInputElement;
  const btn = shadow.getElementById("pw-search") as HTMLButtonElement;
  const result = shadow.getElementById("pw-result") as HTMLDivElement;
  const errEl = shadow.getElementById("pw-err") as HTMLDivElement;

  async function search() {
    const val = input.value.trim();
    if (!val) return;
    result.classList.remove("show");
    errEl.classList.remove("show");
    btn.textContent = "…";
    btn.disabled = true;

    try {
      const isUrl = val.startsWith("http") || val.includes("/p/");
      const param = isUrl
        ? `qr=${encodeURIComponent(val)}`
        : `chip=${encodeURIComponent(val)}`;
      const res = await fetch(`${API_BASE}/v1/pets/lookup?${param}`, {
        headers: { "X-Widget-Clinic": clinicId },
      });

      if (!res.ok) throw new Error("not_found");
      const data = (await res.json()) as {
        petName: string;
        petSpecies: string;
        petPhotoUrl: string | null;
      };

      result.innerHTML = `
        ${data.petPhotoUrl ? `<img src="${data.petPhotoUrl}" alt="${data.petName}" />` : ""}
        <div class="pw-name">${data.petName}</div>
        <div class="pw-meta">${data.petSpecies}</div>
      `;
      result.classList.add("show");
    } catch {
      errEl.classList.add("show");
    } finally {
      btn.textContent = "Buscar";
      btn.disabled = false;
    }
  }

  btn.addEventListener("click", () => void search());
  input.addEventListener("keydown", (e) => {
    if (e.key === "Enter") void search();
  });
}

// Auto-initialize all widgets on DOMContentLoaded
function init() {
  document
    .querySelectorAll<HTMLElement>('[id="pawtrack-widget"]')
    .forEach((el) => {
      const clinicId = el.dataset.clinicId ?? "";
      createWidget(el, clinicId);
    });
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", init);
} else {
  init();
}
