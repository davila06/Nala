export default function ClinicPendingPage() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-sand-50 px-4 animate-fade-in-up">
      <div className="max-w-sm text-center">
        <p className="text-5xl">⏳</p>
        <h1 className="mt-4 text-xl font-extrabold text-sand-900">
          Solicitud recibida
        </h1>
        <p className="mt-2 text-sm text-sand-600">
          Tu clínica ha sido registrada con estado <strong>Pendiente</strong>.
          El equipo de PawTrack revisará tu solicitud y activará tu cuenta en 1-2 días hábiles.
        </p>
        <p className="mt-4 text-xs text-sand-400">
          Recibirás un correo electrónico cuando tu cuenta esté activa.
        </p>
      </div>
    </div>
  )
}

