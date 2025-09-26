const VISTA_BUSQUEDA = {
    busquedaFecha: () => {
        $("#txtFechaInicio").val("");
        $("#txtFechaFin").val("");
        $("#txtNumeroVenta").val("");

        $(".busqueda-fecha").show();
        $(".busqueda-venta").hide();
    },

    busquedaVenta: () => {
        $("#txtFechaInicio").val("");
        $("#txtFechaFin").val("");
        $("#txtNumeroVenta").val("");

        $(".busqueda-fecha").hide();
        $(".busqueda-venta").show();
    }
}

function parseFecha(fechaString) {
    if (!fechaString) return "";
    let soloFecha = fechaString.split(" ")[0]; // si viene con hora, se corta
    let partes = soloFecha.split("/"); // ["21","09","2025"]

    if (partes.length !== 3) return "";

    let dia = partes[0].padStart(2, "0");
    let mes = partes[1].padStart(2, "0");
    let anio = partes[2];

    return `${dia}/${mes}/${anio}`;
}

$(document).ready(function () {
    // Inicializa la vista por fecha
    VISTA_BUSQUEDA.busquedaFecha();

    // Configura datepicker en español
    $.datepicker.setDefaults($.datepicker.regional["es"]);
    $("#txtFechaInicio").datepicker({ dateFormat: "dd/mm/yy" });
    $("#txtFechaFin").datepicker({ dateFormat: "dd/mm/yy" });

    // Cambio entre búsqueda por fecha o número de venta
    $("#cboBuscarPor").change(function () {
        if ($("#cboBuscarPor").val() === "fecha") {
            VISTA_BUSQUEDA.busquedaFecha();
        } else {
            VISTA_BUSQUEDA.busquedaVenta();
        }
    });

    // Botón buscar ventas
    $("#btnBuscar").click(function () {
        // Validación según tipo de búsqueda
        if ($("#cboBuscarPor").val() === "fecha") {
            if ($("#txtFechaInicio").val().trim() === "" || $("#txtFechaFin").val().trim() === "") {
                toastr.warning("", "Debe ingresar fecha inicio y fin");
                return;
            }
        } else {
            if ($("#txtNumeroVenta").val().trim() === "") {
                toastr.warning("", "Debe ingresar el número de venta");
                return;
            }
        }

        // Captura de valores correctamente
        let numeroVenta = $("#txtNumeroVenta").val();
        let fechaInicio = $("#txtFechaInicio").val();
        let fechaFin = $("#txtFechaFin").val();

        // Mostrar Loading
        $(".card-body").find("div.row").LoadingOverlay("show");

        fetch(`/Venta/Historial?numeroVenta=${numeroVenta}&fechaInicio=${fechaInicio}&fechaFin=${fechaFin}`)
            .then(response => {
                $(".card-body").find("div.row").LoadingOverlay("hide");
                return response.ok ? response.json() : Promise.reject(response);
            })
            .then(responseJson => {
                $("#tbventa tbody").html(""); // Limpia tabla

                if (responseJson.length > 0) {
                    responseJson.forEach(venta => {
                        let fila = $("<tr>");
                        fila.append($("<td>").text(parseFecha(venta.fechaRegistro))); // 👉 usamos parseFecha aquí
                        fila.append($("<td>").text(venta.numeroVenta));
                        fila.append($("<td>").text(venta.tipoDocumentoVenta));
                        fila.append($("<td>").text(venta.documentoCliente));
                        fila.append($("<td>").text(venta.nombreCliente));
                        fila.append($("<td>").text(venta.total));
                        fila.append($("<td>").append(
                            $("<button>").addClass("btn btn-info btn-sm").append(
                                $("<i>").addClass("fas fa-eye")
                            ).data("venta", venta)
                        ));

                        $("#tbventa tbody").append(fila);
                    });
                } else {
                    toastr.info("", "No se encontraron ventas para los criterios ingresados");
                }
            })
            .catch(err => console.error(err));
    });
});

$("#tbventa tbody").on("click", ".btn-info", function () {
    let d = $(this).data("venta");

    let fechaFormateada = parseFecha(d.fechaRegistro);

    $("#txtFechaRegistro").val(fechaFormateada);
    $("#txtNumVenta").val(d.numeroVenta);
    $("#txtUsuarioRegistro").val(d.usuario || (d.idUsuarioNavigation ? d.idUsuarioNavigation.nombre : ""));
    $("#txtTipoDocumento").val(d.tipoDocumentoVenta || (d.idTipoDocumentoVentaNavigation ? d.idTipoDocumentoVentaNavigation.descripcion : ""));
    $("#txtDocumentoCliente").val(d.documentoCliente);
    $("#txtNombreCliente").val(d.nombreCliente);
    $("#txtSubTotal").val(d.subTotal);
    $("#txtIGV").val(d.impuestoTotal);
    $("#txtTotal").val(d.total);

    $("#tbProductos tbody").html(""); // Limpia tabla

    d.detalleVenta.forEach((item) => {
        $("#tbProductos tbody").append(
            $("<tr>").append(
                $("<td>").text(item.descripcionProducto),
                $("<td>").text(item.cantidad),
                $("<td>").text(item.precio),
                $("<td>").text(item.total)
            )
        );
    });

    $("#linkImprimir").attr("href", `/Venta/MostrarPDFVenta?numeroVenta=${d.numeroVenta}`)

    $("#modalData").modal("show");
});
