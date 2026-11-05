const { createApp } = Vue;

createApp({
    data() {
        return {
            clientes: [],
            paginaActual: 1,
            totalPaginas: 0,
            totalRegistros: 0,
            tamanioPagina: 10,
            busqueda: '',
            busquedaAplicada: '',
            cargando: false,
            guardando: false,
            formularioVisible: false,
            editando: false,
            idEditado: null,
            mensaje: '',
            error: false,
            errores: {},
            form: { dni: '', nombre: '', apellido: '', telefono: '', email: '' }
        };
    },
    mounted() {
        this.cargarClientes();
    },
    methods: {
        formularioVacio() {
            return { dni: '', nombre: '', apellido: '', telefono: '', email: '' };
        },
        async solicitar(url, opciones = {}) {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const headers = { Accept: 'application/json', ...(opciones.headers || {}) };

            if (opciones.method && opciones.method !== 'GET') {
                headers['X-CSRF-TOKEN'] = token || '';
            }

            const respuesta = await fetch(url, { ...opciones, headers });
            if (respuesta.status === 204) {
                return null;
            }

            const contenido = await respuesta.json().catch(() => ({}));
            if (!respuesta.ok) {
                const errores = contenido.errors
                    ? Object.fromEntries(Object.entries(contenido.errors).map(([campo, mensajes]) => [
                        campo.charAt(0).toLowerCase() + campo.slice(1),
                        Array.isArray(mensajes) ? mensajes.join(' ') : String(mensajes)
                    ]))
                    : {};
                const detalleValidacion = Object.entries(errores)
                    .map(([campo, texto]) => `${campo}: ${texto}`)
                    .join(' ');
                const error = new Error(contenido.message || detalleValidacion || contenido.title || 'No se pudo completar la operación.');
                error.fieldErrors = errores;
                throw error;
            }

            return contenido;
        },
        async cargarClientes() {
            this.cargando = true;
            try {
                const parametros = new URLSearchParams({
                    pagina: String(this.paginaActual),
                    busqueda: this.busquedaAplicada
                });
                const resultado = await this.solicitar(`/api/clientes?${parametros.toString()}`);
                this.clientes = resultado.elementos;
                this.paginaActual = resultado.paginaActual;
                this.totalPaginas = resultado.totalPaginas;
                this.totalRegistros = resultado.totalRegistros;
                this.tamanioPagina = resultado.tamanioPagina;
            } catch (exception) {
                this.mostrarError(exception.message);
            } finally {
                this.cargando = false;
            }
        },
        buscar() {
            this.mensaje = '';
            this.busquedaAplicada = this.busqueda;
            this.paginaActual = 1;
            this.cargarClientes();
        },
        limpiarBusqueda() {
            this.mensaje = '';
            this.busqueda = '';
            this.busquedaAplicada = '';
            this.paginaActual = 1;
            this.cargarClientes();
        },
        cambiarPagina(pagina) {
            if (pagina < 1 || pagina > this.totalPaginas) {
                return;
            }
            this.mensaje = '';
            this.paginaActual = pagina;
            this.cargarClientes();
        },
        nuevoCliente() {
            this.form = this.formularioVacio();
            this.editando = false;
            this.idEditado = null;
            this.errores = {};
            this.mensaje = '';
            this.formularioVisible = true;
        },
        editar(cliente) {
            this.form = {
                dni: cliente.dni,
                nombre: cliente.nombre,
                apellido: cliente.apellido,
                telefono: cliente.telefono || '',
                email: cliente.email || ''
            };
            this.editando = true;
            this.idEditado = cliente.id;
            this.errores = {};
            this.mensaje = '';
            this.formularioVisible = true;
            window.scrollTo({ top: 0, behavior: 'smooth' });
        },
        cancelar() {
            this.formularioVisible = false;
            this.errores = {};
        },
        async guardar() {
            this.guardando = true;
            this.errores = {};
            this.mensaje = '';
            const esEdicion = this.editando;
            try {
                await this.solicitar(
                    esEdicion ? `/api/clientes/${this.idEditado}` : '/api/clientes',
                    {
                        method: esEdicion ? 'PUT' : 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(this.form)
                    }
                );
                this.formularioVisible = false;
                this.form = this.formularioVacio();
                this.mensaje = esEdicion ? 'Cliente actualizado correctamente.' : 'Cliente creado correctamente.';
                this.error = false;
                await this.cargarClientes();
            } catch (exception) {
                this.errores = exception.fieldErrors || {};
                this.mostrarError(exception.message);
            } finally {
                this.guardando = false;
            }
        },
        async eliminar(cliente) {
            if (!window.confirm(`¿Eliminar a ${cliente.apellido}, ${cliente.nombre}?`)) {
                return;
            }

            this.mensaje = '';
            try {
                await this.solicitar(`/api/clientes/${cliente.id}`, { method: 'DELETE' });
                if (this.clientes.length === 1 && this.paginaActual > 1) {
                    this.paginaActual--;
                }
                this.mensaje = 'Cliente eliminado correctamente.';
                this.error = false;
                await this.cargarClientes();
            } catch (exception) {
                this.mostrarError(exception.message);
            }
        },
        mostrarError(texto) {
            this.mensaje = texto;
            this.error = true;
        }
    }
}).mount('#clientes-app');
