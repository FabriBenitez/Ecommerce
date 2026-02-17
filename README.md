# Prueba MP

Aplicacion full stack con:
- Backend: ASP.NET Core (`net7.0`)
- Frontend: React + Vite
- Base de datos: SQL Server

## Requisitos para correr en una notebook nueva

Instalar estas versiones:

1. Git `2.44.0` o superior
2. Node.js `20.19.0` (LTS)
3. .NET SDK `7.0.410`
4. SQL Server `2022 Express` (motor de base de datos)
5. EF Core CLI `7.0.17`
6. ngrok `3.x` (para webhook publico de Mercado Pago)

Notas:
- Este proyecto usa `net7.0` (archivo `pruebaPagoMp/pruebaPagoMp.csproj`).
- Vite 7 requiere Node `20.19+`.
- La cadena de conexion local esta en `pruebaPagoMp/appsettings.json` y apunta a `Server=localhost`.

## Clonar el repositorio

```powershell
git clone <URL_DEL_REPO>
cd "Prueba MP"
```

## Configuracion backend (.NET)

Desde la carpeta `pruebaPagoMp`:

```powershell
dotnet restore
dotnet tool install --global dotnet-ef --version 7.0.17
dotnet ef database update
dotnet run
```

Backend disponible en:
- `https://localhost:7248`
- `http://localhost:5231`

## Configuracion frontend (React + Vite)

Desde la carpeta `front`:

```powershell
npm ci
npm run dev
```

Frontend disponible en:
- `http://localhost:5173`

## Variable de entorno del frontend

El cliente HTTP usa `VITE_API_BASE_URL` (archivo `front/src/shared/api/http.js`).

Crear/editar `front/.env` asi:

```env
VITE_API_BASE_URL=https://localhost:7248
```

## Configuracion ngrok para Mercado Pago (obligatorio para webhooks)

El webhook del backend escucha en:
- `https://<tu-url-publica>/api/webhooks/mercadopago`

Ese endpoint se usa al crear la preferencia de pago (`notification_url`) en `pruebaPagoMp/Services/Pagos/MercadoPagoService.cs`.

1. Inicia sesion en ngrok y configura el token (una sola vez):

```powershell
ngrok config add-authtoken <TU_NGROK_AUTHTOKEN>
```

2. Con el backend levantado en `https://localhost:7248`, abre tunel HTTPS:

```powershell
ngrok http https://localhost:7248
```

3. Copia la URL publica HTTPS que te da ngrok (por ejemplo `https://abc123.ngrok-free.app`).

4. Configura esa URL en `pruebaPagoMp/appsettings.Development.json`:

```json
{
  "PublicBaseUrl": "https://abc123.ngrok-free.app",
  "MercadoPago": {
    "PublicBaseUrl": "https://abc123.ngrok-free.app"
  }
}
```

5. Reinicia backend (`dotnet run`) para tomar el cambio.

Importante:
- Cada vez que cambia la URL de ngrok, actualiza `PublicBaseUrl` y reinicia backend.
- Si no haces esto, Mercado Pago no podra notificar el estado del pago a tu app.

## Usuario admin inicial (seed)

El backend crea un usuario administrador al iniciar:
- Email: `admin@admin.com`
- Contrasena: `Admin123!`

(Se define en `pruebaPagoMp/Program.cs`.)
