$(document).ready(function () {

    $(".container-fluid").LoadingOverlay("show");

    fetch("/Home/ObtenerUsuario")
        .then(response => {
            $(".container-fluid").LoadingOverlay("hide");
            return response.ok ? response.json() : Promise.reject(response);
        })
        .then(responseJson => {

            if (responseJson.estado) {
                const d = responseJson.objeto

                $("#imgFoto").attr("src", d.urlFoto)
                $("#txtNombre").val(d.nombre)
                $("#txtCorreo").val(d.correo)
                $("#txtTelefono").val(d.telefono || "No registrado");
                $("#txtRol").val(d.nombreRol)

            }
            else {
                swal("Error", responseJson.mensaje || "Ocurrió un error", "error");
            }
        });

})


$("#btnGuardarCambios").click(function () {

    if ($("#txtCorreo").val().trim() == "") {
        toastr.warning("", "Debe completar el campo correo")
        $("#txtCorreo").focus()
        return;
    }
    if ($("#txtTelefono").val().trim() == "") {
        toastr.warning("", "Debe completar el campo telefono")
        $("#txtTelefono").focus()
        return;
    }

    swal({
        title: "¿Desea guardar los cambios?",
        type: "warning",
        showCancelButton: true,
        confirmButtonClass: "btn-primary",
        confirmButtonText: "Sí",
        cancelButtonText: "No",
        closeOnConfirm: false,
        closeOnCancel: true
    },
        function (respuesta) {

            if (respuesta) {

                $(".showSweetAlert").LoadingOverlay("show");

                let modelo = {
                    correo: $("#txtCorreo").val().trim(),
                    telefono: $("#txtTelefono").val().trim()
                }

                fetch("/Home/GuardarPerfil", {
                    method: "POST",
                    headers: { "Content-Type": "application/json; charset=utf-8" },
                    body: JSON.stringify(modelo)
                })
                    .then(response => {
                        $(".showSweetAlert").LoadingOverlay("hide");
                        return response.ok ? response.json() : Promise.reject(response);
                    })
                    .then(responseJson => {
                        if (responseJson.estado) {
                            swal("Listo", "Los cambios fueron guardado", "success");
                        } else {
                            swal("Error", responseJson.mensaje || "Lo sentimos", "error");
                        }
                    })
                    .catch(error => {
                        console.error(error);
                        $(".showSweetAlert").LoadingOverlay("hide");
                        swal("Error", "No se pudo conectar con el servidor", "error");
                    });
            }
        }
    );
})



$("#btnCambiarClave").click(function () {

    const inputs = $("input.input-validar").serializeArray();
    const inputs_sin_valor = inputs.filter((item) => item.value.trim() === "");

    console.log(inputs);           // ver todos los inputs
    console.log(inputs_sin_valor); // ver solo los vacíos

    if (inputs_sin_valor.length > 0) {
        const mensaje = `Debe completar el campo: "${inputs_sin_valor[0].name}"`;
        toastr.warning("", mensaje)
        $(`input[name="${inputs_sin_valor[0].name}"]`).focus()
        return;
    }

    if ($("#txtClaveNueva").val().trim() != $("#txtConfirmarClave").val().trim()) {
        toastr.warning("", "Las contraseñas no coinciden.")
        return;
    }

    let modelo = {
        claveActual: $("#txtClaveActual").val().trim(),
        claveNueva: $("#txtClaveNueva").val().trim()
    }

    fetch("/Home/CambiarClave", {
        method: "POST",
        headers: { "Content-Type": "application/json; charset=utf-8" },
        body: JSON.stringify(modelo)
    })
        .then(response => {
            $(".showSweetAlert").LoadingOverlay("hide");
            return response.ok ? response.json() : Promise.reject(response);
        })
        .then(responseJson => {
            if (responseJson.estado) {
                swal("Listo", "Su contraseña fue actualizada", "success");
                $("input.input-validar").val("");
            } else {
                swal("Error", responseJson.mensaje || "Lo sentimos", "error");
            }
        })
        .catch(error => {
            console.error(error);
            $(".showSweetAlert").LoadingOverlay("hide");
            swal("Error", "No se pudo conectar con el servidor", "error");
        });

})