// Genera una colección reproducible. Ejecutar desde la raíz: node scripts/Build-Postman.mjs
import fs from 'node:fs';
fs.mkdirSync('postman', { recursive: true });
const items = [];
function add(name, method, path, code, body, extra = []) {
  const tests = [`pm.test('HTTP ${code}', () => pm.response.to.have.status(${code}));`, ...extra];
  if (code === 204) tests.push("pm.test('Sin cuerpo', () => pm.expect(pm.response.text()).to.eql(''));");
  if (code === 201) tests.push("pm.test('Location del recurso', () => pm.expect(pm.response.headers.get('Location')).to.match(/api\\//i));");
  items.push({ name: `${String(items.length + 1).padStart(2, '0')} ${name}`, request: {
    method, header: body ? [{ key: 'Content-Type', value: 'application/json' }] : [],
    url: '{{baseUrl}}' + path,
    ...(body ? { body: { mode: 'raw', raw: JSON.stringify(body, null, 2), options: { raw: { language: 'json' } } } } : {})
  }, event: [{ listen: 'test', script: { type: 'text/javascript', exec: tests } }] });
}
const owner = {nombre:'Propietario Postman', dni:'{{dni}}', telefono:'987654321', email:'lab-{{runId}}@example.com'};
const other = {...owner, dni:'{{otroDni}}', email:'otro-{{runId}}@example.com'};
const pet = {nombre:'Luna Postman', especie:'Perro', raza:'Mestizo', edad:2, peso:8.5, idPropietario:'{{ownerId}}'};
add('Crear propietario', 'POST', '/api/propietarios', 201, owner, ["pm.collectionVariables.set('ownerId', pm.response.json().idPropietario);"]);
add('Crear segundo propietario', 'POST', '/api/propietarios', 201, other, ["pm.collectionVariables.set('otherId', pm.response.json().idPropietario);"]);
add('Listar propietarios', 'GET', '/api/propietarios', 200, null, ["pm.test('Incluye propietario creado', () => pm.expect(pm.response.json().some(x => x.idPropietario === Number(pm.collectionVariables.get('ownerId')))).to.be.true);"]);
add('Filtro propietarios', 'GET', '/api/propietarios?buscar={{dni}}', 200, null, ["pm.test('Filtro DNI', () => pm.expect(pm.response.json().some(x => x.dni === pm.collectionVariables.get('dni'))).to.be.true);"]);
add('Obtener propietario', 'GET', '/api/propietarios/{{ownerId}}', 200);
add('Mascotas vacías de propietario existente', 'GET', '/api/propietarios/{{ownerId}}/mascotas', 200, null, ["pm.test('Colección vacía', () => pm.expect(pm.response.json()).to.eql([]));"]);
add('DNI duplicado POST', 'POST', '/api/propietarios', 409, {...owner, email:'distinto-{{runId}}@example.com'});
add('Email duplicado POST', 'POST', '/api/propietarios', 409, {...owner, dni:'12345678'});
add('Email mayúsculas duplicado', 'POST', '/api/propietarios', 409, {...owner, dni:'12345678',email:'LAB-{{runId}}@EXAMPLE.COM'});
add('DNI duplicado PUT', 'PUT', '/api/propietarios/{{otherId}}', 409, {...other,idPropietario:'{{otherId}}',dni:'{{dni}}'});
add('Email duplicado PUT', 'PUT', '/api/propietarios/{{otherId}}', 409, {...other,idPropietario:'{{otherId}}',email:owner.email});
add('Actualizar propietario', 'PUT', '/api/propietarios/{{ownerId}}', 204, {...owner,idPropietario:'{{ownerId}}',nombre:'Propietario actualizado'});
add('Comprobar actualización propietario', 'GET', '/api/propietarios/{{ownerId}}', 200, null, ["pm.test('Nombre persistido', () => pm.expect(pm.response.json().nombre).to.eql('Propietario actualizado'));"]);
add('Propietario inválido', 'POST', '/api/propietarios', 400, {...owner,nombre:'',dni:'abc',email:'invalido'});
add('ID propietario discordante', 'PUT', '/api/propietarios/{{ownerId}}', 400, {...owner,idPropietario:0});
add('Crear mascota solo FK', 'POST', '/api/mascotas', 201, pet, ["pm.collectionVariables.set('petId', pm.response.json().idMascota);"]);
add('Listar mascotas con propietario', 'GET', '/api/mascotas', 200, null, ["pm.test('Include sin ciclos', () => { const m=pm.response.json().find(x=>x.idMascota===Number(pm.collectionVariables.get('petId'))); pm.expect(m.propietario.idPropietario).to.eql(Number(pm.collectionVariables.get('ownerId'))); pm.expect(m.propietario).not.to.have.property('mascotas'); });"]);
add('Filtro especie', 'GET', '/api/mascotas?especie=pErRo', 200, null, ["pm.test('Filtro sin distinción de mayúsculas', () => { pm.expect(pm.response.json().length).to.be.above(0); pm.response.json().forEach(m => pm.expect(m.especie.toLowerCase()).to.include('perro')); });"]);
add('Obtener mascota', 'GET', '/api/mascotas/{{petId}}', 200);
add('Ruta anidada', 'GET', '/api/propietarios/{{ownerId}}/mascotas', 200, null, ["pm.test('Mascota vinculada', () => pm.expect(pm.response.json().some(x=>x.idMascota===Number(pm.collectionVariables.get('petId')))).to.be.true);"]);
add('Eliminar propietario con mascotas', 'DELETE', '/api/propietarios/{{ownerId}}', 409);
add('Actualizar mascota', 'PUT', '/api/mascotas/{{petId}}', 204, {...pet,idMascota:'{{petId}}',nombre:'Luna actualizada',peso:9.25});
add('Comprobar actualización mascota', 'GET', '/api/mascotas/{{petId}}', 200, null, ["pm.test('Peso persistido', () => pm.expect(pm.response.json().peso).to.eql(9.25));"]);
add('FK inexistente POST', 'POST', '/api/mascotas', 400, {...pet,idPropietario:2147483647});
add('FK inexistente PUT', 'PUT', '/api/mascotas/{{petId}}', 400, {...pet,idMascota:'{{petId}}',idPropietario:2147483647});
add('Edad y peso inválidos', 'POST', '/api/mascotas', 400, {...pet,edad:-1,peso:1000});
add('Nombre mascota obligatorio', 'POST', '/api/mascotas', 400, {...pet,nombre:''});
add('Nombre propietario demasiado largo', 'POST', '/api/propietarios', 400, {...owner,nombre:'x'.repeat(101)});
add('ID mascota discordante', 'PUT', '/api/mascotas/{{petId}}', 400, {...pet,idMascota:0});
for (const resource of ['propietarios','mascotas']) {
  add(`${resource} GET inexistente`, 'GET', `/api/${resource}/2147483647`, 404);
  add(`${resource} PUT inexistente`, 'PUT', `/api/${resource}/2147483647`, 404,
    resource==='propietarios'?{...owner,idPropietario:2147483647}:{...pet,idMascota:2147483647});
  add(`${resource} DELETE inexistente`, 'DELETE', `/api/${resource}/2147483647`, 404);
}
add('Ruta anidada propietario inexistente', 'GET', '/api/propietarios/2147483647/mascotas', 404);
add('Filtro sin resultados', 'GET', '/api/mascotas?especie=SinCoincidencias{{runId}}', 200, null, ["pm.test('Lista vacía', () => pm.expect(pm.response.json()).to.eql([]));"]);
add('Eliminar mascota', 'DELETE', '/api/mascotas/{{petId}}', 204);
add('Mascota eliminada', 'GET', '/api/mascotas/{{petId}}', 404);
add('Eliminar propietario liberado', 'DELETE', '/api/propietarios/{{ownerId}}', 204);
add('Propietario eliminado', 'GET', '/api/propietarios/{{ownerId}}', 404);
add('Eliminar segundo propietario', 'DELETE', '/api/propietarios/{{otherId}}', 204);
const collection = {info:{name:'Semana 12 Gestión Veterinaria',schema:'https://schema.getpostman.com/json/collection/v2.1.0/collection.json',description:'Ejecutar completa en orden sobre datos de laboratorio. Crea y elimina sus propios registros. No modifica los datos iniciales.'},
  variable:[{key:'baseUrl',value:'http://localhost:5227'}],
  event:[{listen:'prerequest',script:{type:'text/javascript',exec:[
    "if (pm.info.requestName.startsWith('01 ')) {",
    "const id=Date.now().toString()+Math.floor(Math.random()*100000);",
    "const dni=String(10000000+Math.floor(Math.random()*80000000));",
    "pm.collectionVariables.set('runId',id); pm.collectionVariables.set('dni',dni);",
    "pm.collectionVariables.set('otroDni',String(Number(dni)+1));",
    "}"
  ]}}],item:items};
fs.writeFileSync('postman/Veterinaria.postman_collection.json',JSON.stringify(collection,null,2)+'\n');
// Matriz legible para el plan y las capturas, derivada de las mismas solicitudes.
fs.mkdirSync('docs',{recursive:true});
fs.writeFileSync('docs/MATRIZ-PRUEBAS.md','# Matriz de solicitudes Postman\n\nCada caso indica método, ruta y código esperado. Ejecutar en orden con Collection Runner.\n\n'+items.map(i=>{
 const code=i.event[0].script.exec[0].match(/HTTP (\d+)/)[1];
 return `- **${i.name}** — \`${i.request.method} ${i.request.url.replace('{{baseUrl}}','')}\` → **${code}**.`;
}).join('\n')+'\n');
console.log(`${items.length} solicitudes generadas.`);
