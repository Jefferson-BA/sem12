# Semana 12 Gestión Veterinaria

Web API REST de ASP.NET Core **.NET 10**, Entity Framework Core y **PostgreSQL en Neon**. Administra propietarios y mascotas con una relación 1:N, sin Repository ni DTOs. Incluye 11 endpoints, Swagger en `/`, validación automática y respuestas 200, 201, 204, 400, 404 y 409.

La implementación está integrada en el proyecto **Webapi**, con la plantilla ASP.NET Core Web API con controladores. Los controladores heredan de `ControllerBase`; Swagger ocupa la raíz. Los modelos y el contexto se consultan directamente, sin Repository ni DTOs.

## Empezar aquí

1. Inicia Docker Desktop. Necesitas el SDK .NET 10; Node.js/npm permite ejecutar las pruebas automáticas.
2. Desde la raíz del repositorio ejecuta `./Start-Local.cmd`. Si no existe `.env`, lo crea con la configuración **local**. Inicia PostgreSQL en el puerto 55433 y ejecuta `scrip.sql` automáticamente la primera vez que se crea su volumen.
3. Abre <http://localhost:5227/>. Para finalizar la API usa `Ctrl+C`; la base conserva sus datos. No necesitas Neon para esta primera prueba.
4. En otra terminal ejecuta `./Test-Api.cmd`: comprueba Swagger y los 11 endpoints y ejecuta las 42 solicitudes de la colección.
5. Puedes obtener nuevamente el modelo de la base con `./Scaffold.cmd`; el resultado se guarda en una carpeta nueva sin sobrescribir las validaciones.
6. Para pasar a Neon y Render, sigue [el inicio local y posterior publicación](docs/INICIO.md). El ejemplo de Neon está separado en `config/neon.env.example`.

Los lanzadores `.cmd` funcionan con la política actual de PowerShell sin cambiarla permanentemente. Visual Studio y `dotnet run` por sí solos no leen `.env`; el script carga sus variables. No inicies una segunda API en el mismo puerto mientras la primera está ejecutándose.

## Archivos de entrega

- [Guía completa](docs/GUIA.md): comandos CLI, Scaffold, variables de entorno, Docker y despliegue en Render + Neon.
- [Primero local y después nube](docs/INICIO.md): comandos de inicio, parada, pruebas y transición a Neon.
- [Informe listo para completar y pegar](docs/INFORME.md): tema, estructura, enlaces y conclusiones.
- [Plan de pruebas](docs/PRUEBAS.md) y [matriz de 42 solicitudes](docs/MATRIZ-PRUEBAS.md).
- [Colección Postman importable](postman/Veterinaria.postman_collection.json), con 59 aserciones y limpieza de los registros creados.
- [SQL de PostgreSQL](scrip.sql), con los dos propietarios y las tres mascotas de ejemplo.
- [Configuración de ejemplo](.env.example) y [Dockerfile](Dockerfile).
- [Configuración Neon](config/neon.env.example), [base local](compose.yaml) y [Blueprint de Render](render.yaml).

## Código fuente

```text
Webapi/
  Controllers/
    PropietariosController.cs    # CRUD, búsqueda y ruta anidada
    MascotasController.cs        # CRUD y filtro de especie
  Data/VeterinariaContext.cs     # Mapeo EF Core para PostgreSQL
  Models/
    Propietario.cs
    Mascota.cs
  Program.cs                    # DI, CORS, Swagger y rutas
  Webapi.csproj
scripts/                        # Carga de entorno y generación de colección
postman/                        # Pruebas importables
docs/                           # Documentación de entrega
scrip.sql
.env.example
Dockerfile
dotnet-tools.json               # dotnet-ef local reproducible
```

## Endpoints

- `GET /api/propietarios?buscar=texto`: busca parcialmente por nombre, DNI o email; sin filtro lista todos.
- `GET /api/propietarios/{id}`: consulta un propietario.
- `GET /api/propietarios/{id}/mascotas`: lista sus mascotas, o `[]` si existe y no tiene ninguna.
- `POST /api/propietarios`: crea un propietario.
- `PUT /api/propietarios/{id}`: actualiza sus campos escalares.
- `DELETE /api/propietarios/{id}`: elimina si no tiene mascotas.
- `GET /api/mascotas?especie=texto`: filtro parcial sin distinción de mayúsculas; sin filtro lista todas e incluye propietario.
- `GET /api/mascotas/{id}`: consulta una mascota con su propietario.
- `POST /api/mascotas`: crea enviando solamente la FK del propietario.
- `PUT /api/mascotas/{id}`: actualiza una mascota y permite cambiar su propietario.
- `DELETE /api/mascotas/{id}`: elimina una mascota.

Los filtros son variantes de GET: hay **11 endpoints distintos**, sin contar los filtros como rutas adicionales.

## Ejemplos de JSON

Crear propietario (el ID lo genera PostgreSQL):

```json
{
  "nombre": "Lucia Torres",
  "dni": "45678912",
  "telefono": "987654320",
  "email": "lucia.torres@example.com"
}
```

Crear mascota (usar un `idPropietario` existente):

```json
{
  "nombre": "Luna",
  "especie": "Perro",
  "raza": "Mestizo",
  "edad": 2,
  "peso": 8.50,
  "idPropietario": 1
}
```

En PUT se envían todos los campos anteriores **y el ID de la entidad** (`idPropietario` para propietario o `idMascota` para mascota), coincidente con la ruta. No se necesita enviar el objeto de navegación `propietario`. El controlador no inserta un propietario anidado.

## Validaciones y reglas de negocio

`[ApiController]` devuelve 400 automáticamente ante Data Annotations inválidas. DNI: ocho dígitos; nombre, teléfono y email obligatorios con longitudes máximas; email válido; edad 0–100 años; peso 0.01–999.99; FK positiva. Los textos obligatorios vacíos o de solo espacios son inválidos. No se permiten IDs elegidos por el cliente en POST.

**Regla 1 (409):** no repetir DNI ni email al crear o actualizar propietarios. El email se guarda en minúsculas; se compara sin distinguir mayúsculas y se protege además con índices únicos, incluido `lower("Email")` en SQL para solicitudes concurrentes.

**Regla 2 (409):** impedir la eliminación de propietarios con mascotas. Se consulta con `AnyAsync` y se respalda con `ON DELETE RESTRICT`; se captura también una violación de FK durante el guardado.

Todos los accesos del CRUD utilizan el contexto inyectado y métodos asíncronos. `AsNoTracking` se aplica a consultas, `Include` carga el propietario y `FindAsync` recupera entidades a modificar. `[JsonIgnore]` en `Propietario.Mascotas` evita ciclos de serialización. Los errores inesperados producen Problem Details sin exponer credenciales.

## Estado de verificación

Se probó con PostgreSQL 17 temporal en Docker y la API en entorno **Production**: 42 solicitudes y 59 aserciones aprobadas; Swagger respondió en la raíz. También se ejecutó Scaffold sobre ese esquema y se verificó su correspondencia con las entidades ajustadas para las validaciones del laboratorio. Consulta [las limitaciones y evidencia pendiente](docs/PRUEBAS.md).

La publicación, los enlaces finales y las capturas sobre la API pública requieren completar los pasos de la guía en tus cuentas de Neon, GitHub y Render. Los archivos no incluyen credenciales reales.
