# Plan de Backup – pruebaPagoMp

## 1. Objetivo
Garantizar la disponibilidad e integridad de la información crítica del sistema
(usuarios, pedidos, pagos y carritos) ante fallas, errores humanos o incidentes
de seguridad.

---

## 2. Alcance
Se respalda la base de datos principal utilizada por el backend ASP.NET Core.

No se incluyen archivos temporales ni builds del frontend.

---

## 3. Estrategia de Backup

### Tipo
- Backup completo de la base de datos

### Frecuencia
- Diario (una vez cada 24 horas)

### Retención
- Últimos 7 backups diarios
- Último backup mensual

---

## 4. Almacenamiento
- Carpeta segura en servidor
- Copia adicional en almacenamiento externo (cloud o disco externo)

---

## 5. Procedimiento de Restauración
1. Detener el backend
2. Restaurar el backup más reciente válido
3. Verificar integridad de datos
4. Reiniciar servicios

---

## 6. Responsabilidad
- Administrador del sistema / equipo de backend

---

## 7. Pruebas
- Restauración probada manualmente en entorno de desarrollo
