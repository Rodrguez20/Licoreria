$(document).ready(function () {

    $(".card-body").LoadingOverlay("show");

    fetch("/Negocio/Obtener")
        .then(response => {
            $(".card-body").LoadingOverlay("hide");
            return response.ok ? response.json() : Promise.reject(response);
        })
        .then(responseJson => {

            console.log(responseJson)

            if (responseJson.estado) {
                const d = responseJson.objeto

                $("#txtNumeroDocumento").val(d.numeroDocumento)
                $("#txtRazonSocial").val(d.nombre)
                $("#txtCorreo").val(d.correo)
                $("#txtDireccion").val(d.direccion)
                $("#txtTelefono").val(d.telefono)
                $("#txtImpuesto").val(d.porcentajeImpuesto)
                $("#txtSimboloMoneda").val(d.simboloMoneda)
                $("#imgLogo").attr("src", d.urlLogo)
            }
            else {
                swal("Error", responseJson.mensaje || "Ocurrió un error", "error");
            }
        });

}) 

$("#btnGuardarCambios").click(function () {
    // Validar campos
    const inputs = $("input.input-validar").serializeArray();
    const inputs_sin_valor = inputs.filter(item => item.value.trim() === "");

    if (inputs_sin_valor.length > 0) {
        const mensaje = `Debe completar el campo: "${inputs_sin_valor[0].name}"`;
        toastr.warning("", mensaje);
        $(`input[name="${inputs_sin_valor[0].name}"]`).focus();
        return;
    }

    // Crear modelo con los valores del formulario
    const modelo = {
        numeroDocumento: $("#txtNumeroDocumento").val(),
        nombre: $("#txtRazonSocial").val(),
        correo: $("#txtCorreo").val(),
        direccion: $("#txtDireccion").val(),
        telefono: $("#txtTelefono").val(),
        porcentajeImpuesto: $("#txtImpuesto").val(),
        simboloMoneda: $("#txtSimboloMoneda").val()
    };

    // Preparar FormData con el logo si existe
    const inputLog = document.getElementById("txtLogo");
    const formData = new FormData();

    if (inputLog.files.length > 0) {
        formData.append("logo", inputLog.files[0]);
    }

    formData.append("modelo", JSON.stringify(modelo));

    // Mostrar overlay de carga
    $(".card-body").LoadingOverlay("show");

    // Enviar al backend
    fetch("/Negocio/GuardarCambios", {
        method: "POST",
        body: formData
    })
        .then(response => {
            $(".card-body").LoadingOverlay("hide");
            return response.ok ? response.json() : Promise.reject(response);
        })
        .then(responseJson => {
            console.log("Respuesta del servidor:", responseJson);

            if (responseJson.estado) {
                const d = responseJson.objeto;

                // Actualizar todos los campos del formulario
                $("#txtNumeroDocumento").val(d.numeroDocumento);
                $("#txtRazonSocial").val(d.nombre);
                $("#txtCorreo").val(d.correo);
                $("#txtDireccion").val(d.direccion);
                $("#txtTelefono").val(d.telefono);           // ✅ teléfono ahora se mantiene
                $("#txtImpuesto").val(d.porcentajeImpuesto);
                $("#txtSimboloMoneda").val(d.simboloMoneda);

                // Actualizar logo
                $("#imgLogo").attr("src", d.urlLogo);

                // Mensaje de éxito
                swal("Éxito", "Los cambios se guardaron correctamente", "success");
            } else {
                swal("Error", responseJson.mensaje || "Ocurrió un error", "error");
            }
        })
        .catch(error => {
            $(".card-body").LoadingOverlay("hide");
            console.error("Error en la petición:", error);
            swal("Error", "No se pudo completar la solicitud", "error");
        });
});

