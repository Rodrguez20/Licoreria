$(document).ready(function () {
    let ValorImpuesto = 0;
    let ProductosParaVenta = [];
    // Cargar tipos de documento
    fetch("/Venta/ListaTipoDocumentoVenta")
        .then(r => r.ok ? r.json() : Promise.reject(r))
        .then(data => {
            data.forEach(item => {
                $("#cboTipoDocumentoVenta").append(
                    $("<option>").val(item.idTipoDocumentoVenta).text(item.descripcion)
                );
            });
        });
    // Cargar datos del negocio
    fetch("/Negocio/Obtener")
        .then(r => r.ok ? r.json() : Promise.reject(r))
        .then(resp => {
            if (resp.estado) {
                const d = resp.objeto;
                $("#inputGroupSubTotal").text(`Sub total - ${d.simboloMoneda}`);
                $("#inputGroupIGV").text(`IVA (${d.porcentajeImpuesto}%) - ${d.simboloMoneda}`);
                $("#inputGroupTotal").text(`Total - ${d.simboloMoneda}`);
                ValorImpuesto = parseFloat(d.porcentajeImpuesto);
            }
        });
    // Inicializar Select2 para búsqueda de productos
    $("#cboBuscarProducto").select2({
        ajax: {
            url: "/Venta/ObtenerProducto",
            dataType: 'json',
            delay: 250,
            data: function (params) {
                return { busqueda: params.term };
            },
            processResults: function (data) {
                let listaProductos = data.objeto || data;
                return {
                    results: listaProductos.map(item => ({
                        id: item.idProducto,
                        text: item.descripcion,
                        marca: item.marca,
                        categoria: item.nombreCategoria,
                        urlImagen: item.urlImagen,
                        precio: parseFloat(item.precio)
                    }))
                };
            }
        },
        language: "es",
        placeholder: 'Buscar Producto...',
        minimumInputLength: 1,
        templateResult: formatoResultados
    });
    // Mostrar resultados con imagen y marca
    function formatoResultados(data) {
        if (data.loading) return data.text;
        return $(`
            <table width="100%">
                <tr>
                    <td style="width:60px">
                        <img style="height:60px; width:60px; margin-right:10px" src="${data.urlImagen || ''}" />
                    </td>
                    <td>
                        <p style="font-weight: bolder; margin:2px">${data.marca || ''}</p>
                        <p style="margin:2px">${data.text || ''}</p>
                    </td>
                </tr>
            </table>
        `);
    }
    // Foco automático al abrir Select2
    $(document).on("select2:open", function () {
        document.querySelector(".select2-search__field").focus();
    });
    // Selección de producto
    $("#cboBuscarProducto").on("select2:select", function (e) {
        const data = e.params.data;
        if (ProductosParaVenta.some(p => p.idProducto == data.id)) {
            $("#cboBuscarProducto").val("").trigger("change");
            toastr.warning("", "El producto ya fue agregado");
            return;
        }
        Swal.fire({
            title: data.marca,
            text: data.text,
            imageUrl: data.urlImagen,
            input: 'text',
            inputLabel: 'Ingrese cantidad',
            showCancelButton: true,
            inputValidator: (value) => {
                if (!value) return 'Necesita ingresar la cantidad';
                if (isNaN(parseInt(value))) return 'Debe ingresar un valor numérico';
            }
        }).then(result => {
            if (!result.isConfirmed) return;
            let cantidad = parseInt(result.value);
            let producto = {
                idProducto: data.id,
                marcaProducto: data.marca,
                descripcionProducto: data.text,
                categoriaProducto: data.categoria,
                cantidad: cantidad,
                precio: data.precio.toString(),
                total: (cantidad * data.precio).toString()
            };
            ProductosParaVenta.push(producto);
            mostrarProducto_Precio();
            $("#cboBuscarProducto").val("").trigger("change");
        });
    });
    // Eliminar producto de la tabla
    $(document).on("click", ".btn-eliminar", function () {
        const _idproducto = $(this).data("idproducto");
        ProductosParaVenta = ProductosParaVenta.filter(p => p.idProducto != _idproducto);
        mostrarProducto_Precio();
    });
    // Función para actualizar tabla y totales
    function mostrarProducto_Precio() {
        let total = 0;
        let subtotal = 0;
        let igv = 0;
        let porcentaje = ValorImpuesto / 100;
        $("#tbProducto tbody").html("");
        ProductosParaVenta.forEach((item) => {
            $("#tbProducto tbody").append(
                $("<tr>").append(
                    $("<td>").append(
                        $("<button>")
                            .addClass("btn btn-danger btn-eliminar btn-sm")
                            .data("idproducto", item.idProducto)
                            .append($("<i>").addClass("fas fa-trash-alt"))
                    ),
                    $("<td>").text(item.descripcionProducto),
                    $("<td>").text(item.cantidad),
                    $("<td>").text(item.precio),
                    $("<td>").text(item.total)
                )
            );
            total += parseFloat(item.total);
        });
        subtotal = total / (1 + porcentaje);
        igv = total - subtotal;
        $("#txtSubTotal").val(subtotal.toFixed(2));
        $("#txtIGV").val(igv.toFixed(2));
        $("#txtTotal").val(total.toFixed(2));
    }
    // Botón finalizar venta
    $("#btnTerminarVenta").click(function () {
        if (ProductosParaVenta.length < 1) {
            toastr.warning("", "Debe ingresar productos");
            return;
        }
        const venta = {
            idTipoDocumentoVenta: $("#cboTipoDocumentoVenta").val(),
            documentoCliente: $("#txtDocumentoCliente").val(),
            nombreCliente: $("#txtNombreCliente").val(),
            subTotal: $("#txtSubTotal").val(),
            impuestoTotal: $("#txtIGV").val(),
            total: $("#txtTotal").val(),
            DetalleVenta: ProductosParaVenta
        };
        $("#btnTerminarVenta").LoadingOverlay("show");
        fetch("/Venta/RegistrarVenta", {
            method: "POST",
            headers: { "Content-Type": "application/json; charset=utf-8" },
            body: JSON.stringify(venta)
        })
            .then(response => {
                $("#btnTerminarVenta").LoadingOverlay("hide");
                if (!response.ok) throw new Error("Error en la respuesta del servidor");
                return response.json();
            })
            .then(data => {
                console.log("Respuesta del servidor:", data); // Log para depurar
                if (data && data.estado === true) {
                    ProductosParaVenta = [];
                    mostrarProducto_Precio();
                    $("#txtDocumentoCliente").val("");
                    $("#txtNombreCliente").val("");
                    $("#cboTipoDocumentoVenta").val($("#cboTipoDocumentoVenta option:first").val());
                    $("#cboBuscarProducto").val(null).trigger("change");
                    // ✅ Alerta garantizada
                    Swal.fire({
                        title: "Listo",
                        text: `Venta registrada exitosamente! Número venta: ${data.objeto?.numeroVenta || 'N/A'}`,
                        icon: "success"
                    });

                } else {
                    Swal.fire({
                        title: "Error",
                        text: data?.mensaje || "No se pudo registrar la venta",
                        icon: "error",
                        confirmButtonText: "Aceptar"
                    });

                }
            })
            .catch(err => {
                console.error("Error en fetch:", err);
                swal("Error", "No se pudo conectar o procesar la venta", "error");
            });
    });
});
